using System.Net.Http;
using System.Net.Http.Headers;
using CodeSpirit.ConfigCenter.Sdk.Models;
using CodeSpirit.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Sdk;

/// <summary>
/// 配置中心 HTTP 客户端
/// </summary>
public class ConfigCenterClient
{
    private readonly HttpClient _httpClient;
    private readonly ConfigCenterOptions _options;
    private readonly ILogger<ConfigCenterClient> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCenterClient(
        HttpClient httpClient,
        IOptions<ConfigCenterOptions> options,
        ILogger<ConfigCenterClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // 设置基础地址
        if (!string.IsNullOrEmpty(_options.ServiceUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.ServiceUrl);
        }
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // 设置请求头
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// 获取应用配置
    /// </summary>
    public async Task<ConfigItemsExportDto> GetConfigsAsync(string appId, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        int maxRetries = 3;
        TimeSpan delay = TimeSpan.FromSeconds(2);
        
        while (true)
        {
            try
            {
                _logger.LogInformation("正在获取应用 {AppId} 的配置", appId);
                
                var requestUrl = $"api/config/client/config/{appId}";
                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                
                response.EnsureSuccessStatusCode();
                
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<ApiResponse<ConfigItemsExportDto>>(responseBody);
                
                if (result?.Data == null)
                {
                    throw new Exception("获取应用配置失败：响应数据为空");
                }
                
                _logger.LogInformation("成功获取配置，包含 {Count} 个配置项", 
                    result.Data.Configs?.Count ?? 0);

                // 开发环境下打印详细的配置项
                if (_options.EnableDetailedLogging && result.Data.Configs != null)
                {
                    _logger.LogInformation("==== 配置中心拉取详情 [AppId: {AppId}] ====", appId);
                    _logger.LogInformation("配置版本: {Version}", result.Data.Version);
                    _logger.LogInformation("配置项数量: {Count}", result.Data.Configs.Count);
                    _logger.LogInformation("配置项列表:");
                    
                    foreach (var config in result.Data.Configs.OrderBy(c => c.Key))
                    {
                        var valueStr = config.Value?.ToString() ?? "(null)";
                        // 截断过长的值
                        if (valueStr.Length > 200)
                        {
                            valueStr = valueStr.Substring(0, 200) + "... (已截断)";
                        }
                        _logger.LogInformation("  [{Key}] = {Value}", config.Key, valueStr);
                    }
                    _logger.LogInformation("==== 配置拉取完成 ====");
                }
                
                return result.Data;
            }
            catch (Exception ex)
            {
                retryCount++;
                
                if (retryCount <= maxRetries)
                {
                    _logger.LogWarning(ex, "获取配置失败，将在 {Delay} 秒后重试 ({RetryCount}/{MaxRetries})", 
                        delay.TotalSeconds, retryCount, maxRetries);
                    
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30)); // 指数退避策略
                    continue;
                }
                
                _logger.LogError(ex, "获取应用配置失败，已达到最大重试次数");
                throw;
            }
        }
    }

    /// <summary>
    /// 获取应用配置版本（轻量级API，用于轮询检测变更）
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前配置版本号</returns>
    public async Task<long> GetConfigVersionAsync(string appId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"api/config/client/config/{appId}/version";
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<ApiResponse<ConfigVersionDto>>(responseBody);
            
            if (result?.Data == null)
            {
                _logger.LogWarning("获取配置版本失败：响应数据为空，返回版本0");
                return 0;
            }
            
            return result.Data.Version;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取配置版本失败: AppId={AppId}", appId);
            return -1; // 返回-1表示获取失败，调用方可选择拉取完整配置
        }
    }
}

