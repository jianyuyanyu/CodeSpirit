using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CodeSpirit.ConfigCenter.Client.Models;
using CodeSpirit.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置中心客户端
/// </summary>
public class ConfigCenterClient
{
    private readonly HttpClient _httpClient;
    private readonly ConfigCenterClientOptions _options;
    private readonly ILogger<ConfigCenterClient> _logger;

    public ConfigCenterClient(
        HttpClient httpClient,
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCenterClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // 设置基础地址和超时时间
        _httpClient.BaseAddress = new Uri(_options.ServiceUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        
        // 设置请求头
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            
        // 设置认证头
        if (!string.IsNullOrEmpty(_options.AppSecret))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _options.AppSecret);
        }
        
        // 处理 SSL 证书验证问题
        ConfigureSslValidation();
    }
    
    /// <summary>
    /// 配置SSL验证
    /// </summary>
    private void ConfigureSslValidation()
    {
        if (_options.IgnoreSslCertificateErrors)
        {
            var handler = _httpClient.GetType()
                .GetField("_handler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                .GetValue(_httpClient) as HttpClientHandler;
            
            if (handler != null)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                _logger.LogWarning("SSL 证书验证已禁用，请勿在生产环境中使用此配置");
            }
        }
    }

    /// <summary>
    /// 获取应用配置
    /// </summary>
    public async Task<ConfigItemsExportDto> GetConfigsAsync(CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        int maxRetries = _options.MaxRetryAttempts ?? 3;
        TimeSpan delay = TimeSpan.FromSeconds(_options.RetryDelaySeconds ?? 2);
        
        while (true)
        {
            try
            {
                _logger.LogInformation("正在获取应用 {AppId} 在 {Environment} 环境的配置", 
                    _options.AppId, _options.Environment);
                
                var requestUrl = $"api/config/client/config/{_options.AppId}/{_options.Environment}";
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
    /// 更新应用密钥
    /// </summary>
    public void UpdateAppSecret(string secret)
    {
        if (!string.IsNullOrEmpty(secret))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", secret);
        }
    }
} 