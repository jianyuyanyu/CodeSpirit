using CodeSpirit.ConfigCenter.Sdk.Cache;
using CodeSpirit.ConfigCenter.Sdk.Models;
using CodeSpirit.ConfigCenter.Sdk.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Sdk;

/// <summary>
/// SSE 事件监听器 - 接收配置变更通知
/// </summary>
public class SseEventListener : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConfigCenterClient _configClient;
    private readonly ConfigCacheService _cacheService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppRegistrationService _registrationService;
    private readonly ConfigCenterOptions _options;
    private readonly ILogger<SseEventListener> _logger;
    private readonly IConfigurationRoot? _configurationRoot;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SseEventListener(
        IHttpClientFactory httpClientFactory,
        ConfigCenterClient configClient,
        ConfigCacheService cacheService,
        IServiceScopeFactory scopeFactory,
        AppRegistrationService registrationService,
        IOptions<ConfigCenterOptions> options,
        IConfiguration configuration,
        ILogger<SseEventListener> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configClient = configClient;
        _cacheService = cacheService;
        _scopeFactory = scopeFactory;
        _registrationService = registrationService;
        _options = options.Value;
        _logger = logger;
        _configurationRoot = configuration as IConfigurationRoot;
    }

    /// <summary>
    /// 执行后台服务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待应用注册完成
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var appId = _registrationService.GetCurrentAppId();
        if (string.IsNullOrEmpty(appId))
        {
            _logger.LogWarning("无法获取AppId，SSE监听器将不会启动");
            return;
        }

        _logger.LogInformation("SSE监听器启动: AppId={AppId}", appId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenToSseAsync(appId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SSE监听器已取消: AppId={AppId}", appId);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE连接断开，5秒后重连: AppId={AppId}", appId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 监听SSE事件流
    /// </summary>
    private async Task ListenToSseAsync(string appId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ConfigCenter");
        var url = $"/api/config/client/events/{appId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        _logger.LogInformation("SSE连接已建立: AppId={AppId}", appId);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 处理心跳注释
            if (line.StartsWith(":"))
            {
                _logger.LogTrace("收到SSE心跳: AppId={AppId}", appId);
                continue;
            }

            // 处理数据行
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                await HandleEventAsync(json, appId);
            }
        }
    }

    /// <summary>
    /// 处理SSE事件
    /// </summary>
    private async Task HandleEventAsync(string json, string appId)
    {
        try
        {
            var eventData = JsonConvert.DeserializeObject<SseEvent>(json);
            if (eventData == null)
            {
                _logger.LogWarning("无法解析SSE事件: {Json}", json);
                return;
            }

            if (eventData.Type == "Connected")
            {
                _logger.LogInformation("SSE连接已确认: AppId={AppId}", eventData.AppId);
                return;
            }

            if (eventData.Type == "ConfigChanged")
            {
                _logger.LogInformation("收到配置变更通知: AppId={AppId}, Version={Version}", 
                    eventData.AppId, eventData.Version);

                // 验证应用ID匹配
                if (eventData.AppId != appId)
                {
                    _logger.LogWarning("配置变更事件的应用ID {EventAppId} 与当前应用 {CurrentAppId} 不匹配", 
                        eventData.AppId, appId);
                    return;
                }

                // 重新加载配置
                var newConfig = await _configClient.GetConfigsAsync(appId);
                
                // 清除并更新内存缓存
                using var scope = _scopeFactory.CreateScope();
                var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
                memoryCache.ClearCache(appId);
                memoryCache.SaveToCache(appId, newConfig);
                
                // 清除并更新Redis缓存（可选）
                await _cacheService.ClearCacheAsync(appId);
                await _cacheService.SaveToCacheAsync(appId, newConfig);

                // 触发配置重载
                _configurationRoot?.Reload();

                _logger.LogInformation("配置已刷新: AppId={AppId}, Version={Version}", 
                    appId, eventData.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理SSE事件失败: {Json}", json);
        }
    }
}

/// <summary>
/// SSE事件数据模型
/// </summary>
public class SseEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; }
}

