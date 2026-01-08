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
/// SSE 事件监听器 - 接收配置变更通知（支持自动降级到轮询模式）
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
    /// 当前已知的配置版本（用于轮询模式检测变更）
    /// </summary>
    private long _currentVersion = 0;
    
    /// <summary>
    /// SSE 连续失败次数
    /// </summary>
    private int _sseFailureCount = 0;
    
    /// <summary>
    /// 是否已切换到轮询模式
    /// </summary>
    private bool _usePollingMode = false;

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
        _usePollingMode = _options.UsePollingMode;
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
            _logger.LogWarning("无法获取AppId，配置变更监听器将不会启动");
            return;
        }

        // 初始化当前版本
        await InitializeCurrentVersionAsync(appId);

        if (_usePollingMode)
        {
            _logger.LogInformation("配置变更监听器启动（轮询模式）: AppId={AppId}, 轮询间隔={Interval}秒", 
                appId, _options.PollingIntervalSeconds);
            await RunPollingModeAsync(appId, stoppingToken);
        }
        else
        {
            _logger.LogInformation("配置变更监听器启动（SSE模式）: AppId={AppId}", appId);
            await RunSseModeWithFallbackAsync(appId, stoppingToken);
        }
    }

    /// <summary>
    /// 初始化当前配置版本
    /// </summary>
    private async Task InitializeCurrentVersionAsync(string appId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
            var cached = memoryCache.GetFromCache(appId);
            if (cached != null)
            {
                _currentVersion = cached.Version;
                _logger.LogDebug("初始化配置版本: AppId={AppId}, Version={Version}", appId, _currentVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "初始化配置版本失败: AppId={AppId}", appId);
        }
    }

    /// <summary>
    /// 运行 SSE 模式（带自动降级到轮询的后备机制）
    /// </summary>
    private async Task RunSseModeWithFallbackAsync(string appId, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenToSseAsync(appId, stoppingToken);
                // 如果 SSE 正常退出（非异常），重置失败计数
                _sseFailureCount = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SSE监听器已取消: AppId={AppId}", appId);
                break;
            }
            catch (TimeoutException)
            {
                _sseFailureCount++;
                _logger.LogWarning("SSE连接超时，失败次数: {FailureCount}/{Threshold}", 
                    _sseFailureCount, _options.SseFailureThresholdBeforePolling);
                
                // 检查是否应该切换到轮询模式
                if (_sseFailureCount >= _options.SseFailureThresholdBeforePolling)
                {
                    _logger.LogWarning("SSE连续失败 {Count} 次，自动切换到轮询模式。AppId={AppId}", 
                        _sseFailureCount, appId);
                    _usePollingMode = true;
                    await RunPollingModeAsync(appId, stoppingToken);
                    return;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _sseFailureCount++;
                _logger.LogError(ex, "SSE连接断开，失败次数: {FailureCount}，5秒后重连: AppId={AppId}", 
                    _sseFailureCount, appId);
                
                // 检查是否应该切换到轮询模式
                if (_sseFailureCount >= _options.SseFailureThresholdBeforePolling)
                {
                    _logger.LogWarning("SSE连续失败 {Count} 次，自动切换到轮询模式。AppId={AppId}", 
                        _sseFailureCount, appId);
                    _usePollingMode = true;
                    await RunPollingModeAsync(appId, stoppingToken);
                    return;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 运行轮询模式
    /// </summary>
    private async Task RunPollingModeAsync(string appId, CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForConfigChangesAsync(appId);
                await Task.Delay(pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("轮询监听器已取消: AppId={AppId}", appId);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮询检查配置变更失败，{Interval}秒后重试: AppId={AppId}", 
                    _options.PollingIntervalSeconds, appId);
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }
    }

    /// <summary>
    /// 检查配置是否有变更（轮询模式）- 使用轻量级版本检查API
    /// </summary>
    private async Task CheckForConfigChangesAsync(string appId)
    {
        if (_options.EnableDetailedLogging)
        {
            _logger.LogDebug("轮询检查配置版本: AppId={AppId}, CurrentVersion={Version}", appId, _currentVersion);
        }

        // 使用轻量级API获取版本号
        var serverVersion = await _configClient.GetConfigVersionAsync(appId);
        
        // 如果获取版本失败（返回-1），则跳过本次检查
        if (serverVersion < 0)
        {
            _logger.LogWarning("获取配置版本失败，跳过本次检查: AppId={AppId}", appId);
            return;
        }
        
        // 版本未变化，无需拉取完整配置
        if (serverVersion <= _currentVersion)
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.LogDebug("配置无变更: AppId={AppId}, Version={Version}", appId, _currentVersion);
            }
            return;
        }
        
        // 版本有变化，拉取完整配置
        _logger.LogInformation("检测到配置版本变更（轮询）: AppId={AppId}, OldVersion={OldVersion}, NewVersion={NewVersion}", 
            appId, _currentVersion, serverVersion);

        // 获取旧配置用于对比
        ConfigItemsExportDto? oldConfig = null;
        if (_options.EnableDetailedLogging)
        {
            using var scope = _scopeFactory.CreateScope();
            var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
            oldConfig = memoryCache.GetFromCache(appId);
        }

        // 拉取完整配置
        var newConfig = await _configClient.GetConfigsAsync(appId);

        // 更新缓存
        using (var scope = _scopeFactory.CreateScope())
        {
            var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
            memoryCache.ClearCache(appId);
            memoryCache.SaveToCache(appId, newConfig);
        }

        await _cacheService.ClearCacheAsync(appId);
        await _cacheService.SaveToCacheAsync(appId, newConfig);

        // 打印详细变更日志
        if (_options.EnableDetailedLogging)
        {
            LogConfigChanges(appId, oldConfig, newConfig, newConfig.Version);
        }

        // 更新版本号
        _currentVersion = newConfig.Version;

        // 触发配置重载
        _configurationRoot?.Reload();

        _logger.LogInformation("配置已刷新（轮询）: AppId={AppId}, Version={Version}", appId, newConfig.Version);
    }

    /// <summary>
    /// 监听SSE事件流
    /// </summary>
    private async Task ListenToSseAsync(string appId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ConfigCenter");
        var url = $"/api/config/client/events/{appId}";

        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation("正在建立SSE连接: URL={Url}, BaseAddress={BaseAddress}", url, client.BaseAddress);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // 添加请求头以确保服务端和代理不缓冲响应
        request.Headers.Add("Accept", "text/event-stream");
        request.Headers.Add("Cache-Control", "no-cache");
        request.Headers.Add("Connection", "keep-alive");
        
        _logger.LogInformation("SSE正在发送请求: AppId={AppId}, FullUrl={FullUrl}", 
            appId, new Uri(client.BaseAddress!, url).ToString());
        
        // 使用超时机制检测 SendAsync 是否阻塞
        using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        sendCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30秒超时
        
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, sendCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("SSE发送请求超时(30秒): AppId={AppId}，可能是Aspire代理缓冲问题", appId);
            throw new TimeoutException($"SSE发送请求超时: AppId={appId}");
        }

        // 使用 using 块确保 response 被正确释放
        using (response)
        {
            // 无论是否启用详细日志，都打印响应状态（用于诊断）
            _logger.LogInformation("SSE收到响应: AppId={AppId}, StatusCode={StatusCode}, ContentType={ContentType}", 
                appId, response.StatusCode, response.Content.Headers.ContentType);
            
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("SSE开始读取流: AppId={AppId}", appId);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            _logger.LogInformation("SSE连接已建立，开始读取数据: AppId={AppId}", appId);

            string? line;
            int lineCount = 0;
            _logger.LogInformation("SSE进入读取循环: AppId={AppId}", appId);
            
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null && !cancellationToken.IsCancellationRequested)
            {
                lineCount++;
                _logger.LogDebug("SSE读取第{LineCount}行: AppId={AppId}, IsEmpty={IsEmpty}, Length={Length}", 
                    lineCount, appId, string.IsNullOrWhiteSpace(line), line?.Length ?? 0);
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 处理心跳注释
                if (line.StartsWith(":"))
                {
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogDebug("收到SSE心跳: AppId={AppId}", appId);
                    }
                    continue;
                }

                // 开发环境下打印收到的原始数据
                if (_options.EnableDetailedLogging)
                {
                    _logger.LogInformation("收到SSE原始数据: AppId={AppId}, Line={Line}", appId, line);
                }

                // 处理数据行
                if (line.StartsWith("data: "))
                {
                    var json = line.Substring(6);
                    await HandleEventAsync(json, appId);
                }
                else
                {
                    _logger.LogWarning("收到未知格式的SSE数据: AppId={AppId}, Line={Line}", appId, line);
                }
            }

            _logger.LogWarning("SSE读取循环结束: AppId={AppId}, IsCancelled={IsCancelled}", 
                appId, cancellationToken.IsCancellationRequested);
        }
    }

    /// <summary>
    /// 打印配置变更详情
    /// </summary>
    private void LogConfigChanges(string appId, ConfigItemsExportDto? oldConfig, ConfigItemsExportDto newConfig, long newVersion)
    {
        _logger.LogInformation("==== 配置变更详情 [AppId: {AppId}] ====", appId);
        _logger.LogInformation("变更时间: {Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        _logger.LogInformation("新版本号: {NewVersion}", newVersion);
        
        if (oldConfig == null || oldConfig.Configs == null)
        {
            _logger.LogInformation("无法对比变更（旧配置不存在），新配置包含 {Count} 个配置项", 
                newConfig.Configs?.Count ?? 0);
            
            if (newConfig.Configs != null)
            {
                _logger.LogInformation("全部配置项:");
                foreach (var config in newConfig.Configs.OrderBy(c => c.Key))
                {
                    var valueStr = config.Value?.ToString() ?? "(null)";
                    if (valueStr.Length > 200)
                    {
                        valueStr = valueStr.Substring(0, 200) + "... (已截断)";
                    }
                    _logger.LogInformation("  [新增] {Key} = {Value}", config.Key, valueStr);
                }
            }
        }
        else
        {
            var oldConfigs = oldConfig.Configs ?? new Dictionary<string, object>();
            var newConfigs = newConfig.Configs ?? new Dictionary<string, object>();
            
            _logger.LogInformation("旧版本号: {OldVersion}", oldConfig.Version);
            
            // 查找新增的配置项
            var addedKeys = newConfigs.Keys.Except(oldConfigs.Keys).OrderBy(k => k).ToList();
            if (addedKeys.Any())
            {
                _logger.LogInformation("新增配置项 ({Count}):", addedKeys.Count);
                foreach (var key in addedKeys)
                {
                    var valueStr = newConfigs[key]?.ToString() ?? "(null)";
                    if (valueStr.Length > 200)
                    {
                        valueStr = valueStr.Substring(0, 200) + "... (已截断)";
                    }
                    _logger.LogInformation("  [+] {Key} = {Value}", key, valueStr);
                }
            }
            
            // 查找删除的配置项
            var removedKeys = oldConfigs.Keys.Except(newConfigs.Keys).OrderBy(k => k).ToList();
            if (removedKeys.Any())
            {
                _logger.LogInformation("删除配置项 ({Count}):", removedKeys.Count);
                foreach (var key in removedKeys)
                {
                    var valueStr = oldConfigs[key]?.ToString() ?? "(null)";
                    if (valueStr.Length > 200)
                    {
                        valueStr = valueStr.Substring(0, 200) + "... (已截断)";
                    }
                    _logger.LogInformation("  [-] {Key} = {Value}", key, valueStr);
                }
            }
            
            // 查找修改的配置项
            var changedKeys = oldConfigs.Keys.Intersect(newConfigs.Keys)
                .Where(k => !Equals(oldConfigs[k]?.ToString(), newConfigs[k]?.ToString()))
                .OrderBy(k => k)
                .ToList();
            
            if (changedKeys.Any())
            {
                _logger.LogInformation("修改配置项 ({Count}):", changedKeys.Count);
                foreach (var key in changedKeys)
                {
                    var oldValueStr = oldConfigs[key]?.ToString() ?? "(null)";
                    var newValueStr = newConfigs[key]?.ToString() ?? "(null)";
                    
                    if (oldValueStr.Length > 100)
                    {
                        oldValueStr = oldValueStr.Substring(0, 100) + "... (已截断)";
                    }
                    if (newValueStr.Length > 100)
                    {
                        newValueStr = newValueStr.Substring(0, 100) + "... (已截断)";
                    }
                    
                    _logger.LogInformation("  [*] {Key}", key);
                    _logger.LogInformation("      旧值: {OldValue}", oldValueStr);
                    _logger.LogInformation("      新值: {NewValue}", newValueStr);
                }
            }
            
            // 查找未变更的配置项
            var unchangedKeys = oldConfigs.Keys.Intersect(newConfigs.Keys)
                .Where(k => Equals(oldConfigs[k]?.ToString(), newConfigs[k]?.ToString()))
                .ToList();
            
            if (unchangedKeys.Any())
            {
                _logger.LogInformation("未变更配置项: {Count} 个", unchangedKeys.Count);
            }
        }
        
        _logger.LogInformation("==== 配置变更完成 ====");
    }

    /// <summary>
    /// 处理SSE事件
    /// </summary>
    private async Task HandleEventAsync(string json, string appId)
    {
        try
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation("开始处理SSE事件: AppId={AppId}, JSON={Json}", appId, json);
            }

            var eventData = JsonConvert.DeserializeObject<SseEvent>(json);
            if (eventData == null)
            {
                _logger.LogWarning("无法解析SSE事件: {Json}", json);
                return;
            }

            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation("SSE事件解析成功: Type={Type}, EventAppId={EventAppId}, Version={Version}", 
                    eventData.Type, eventData.AppId, eventData.Version);
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

                // 获取旧配置（用于对比）
                ConfigItemsExportDto? oldConfig = null;
                if (_options.EnableDetailedLogging)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
                    oldConfig = memoryCache.GetFromCache(appId);
                }

                // 重新加载配置
                var newConfig = await _configClient.GetConfigsAsync(appId);
                
                // 清除并更新内存缓存
                using (var scope = _scopeFactory.CreateScope())
                {
                    var memoryCache = scope.ServiceProvider.GetRequiredService<InMemoryConfigCache>();
                    memoryCache.ClearCache(appId);
                    memoryCache.SaveToCache(appId, newConfig);
                }
                
                // 清除并更新Redis缓存（可选）
                await _cacheService.ClearCacheAsync(appId);
                await _cacheService.SaveToCacheAsync(appId, newConfig);

                // 开发环境下打印详细的配置变更对比
                if (_options.EnableDetailedLogging)
                {
                    LogConfigChanges(appId, oldConfig, newConfig, eventData.Version);
                }

                // 更新当前版本号
                _currentVersion = eventData.Version;

                // 触发配置重载
                _configurationRoot?.Reload();

                _logger.LogInformation("配置已刷新（SSE）: AppId={AppId}, Version={Version}", 
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

