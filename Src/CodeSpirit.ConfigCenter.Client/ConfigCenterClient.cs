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
        
        // 处理 SSL 证书验证问题（可通过配置控制）
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
            else
            {
                _logger.LogWarning("无法配置 SSL 证书验证回调，SSL 错误可能会持续");
            }
        }
    }

    /// <summary>
    /// 注册应用
    /// </summary>
    public async Task<AppRegistrationResponse> RegisterAppAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("正在注册应用 {AppId}", _options.AppId);
            
            var request = new AppRegistrationRequest
            {
                Id = _options.AppId,
                Name = _options.AppName ?? $"Auto registered: {_options.AppId}",
                Description = $"自动注册应用，注册时间：{DateTime.Now}",
                Secret = _options.AppSecret ?? Guid.NewGuid().ToString("N")
            };
            
            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync("/api/config/client/apps/register", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<ApiResponse<AppRegistrationResponse>>(responseBody);
            
            if (result?.Data == null)
            {
                _logger.LogError("应用注册失败：响应数据为空");
                return new AppRegistrationResponse { Success = false, Message = "应用注册失败：响应数据为空" };
            }
            
            result.Data.Success = true;
            _logger.LogInformation("应用 {AppId} 注册成功", _options.AppId);
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用 {AppId} 注册失败：{Message}", _options.AppId, ex.Message);
            return new AppRegistrationResponse
            {
                Success = false,
                Message = $"应用注册失败：{ex.Message}"
            };
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
                _logger.LogInformation("正在获取应用 {AppId} 在 {Environment} 环境的配置{RetryInfo}", 
                    _options.AppId, _options.Environment, retryCount > 0 ? $"（第 {retryCount} 次重试）" : "");
                
                // 添加身份验证头
                if (!string.IsNullOrEmpty(_options.AppSecret))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", _options.AppSecret);
                }
                
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, 
                    $"api/config/client/config/{_options.AppId}/{_options.Environment}");
                    
                // 使用 SendAsync 并启用 HttpCompletionOption.ResponseHeadersRead 以更好地处理连接问题
                var response = await _httpClient.SendAsync(requestMessage, 
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                _logger.LogDebug("收到配置中心响应，状态码：{StatusCode}", response.StatusCode);

                if ((int)response.StatusCode == 503)
                {
                    _logger.LogWarning("收到服务不可用(503)响应。");
                    _logger.LogInformation("当前请求URL: {BaseUrl}{Path}", _httpClient.BaseAddress,
                        $"api/config/client/config/{_options.AppId}/{_options.Environment}");
                }

                response.EnsureSuccessStatusCode();
                
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<ApiResponse<ConfigItemsExportDto>>(responseBody);
                
                if (result?.Data == null)
                {
                    _logger.LogError("获取应用配置失败：响应数据为空");
                    throw new Exception("获取应用配置失败：响应数据为空");
                }
                
                _logger.LogInformation("成功获取应用 {AppId} 在 {Environment} 环境的配置，包含 {Count} 个配置项", 
                    _options.AppId, _options.Environment, result.Data.Configs?.Count ?? 0);
                
                return result.Data;
            }
            catch (Exception ex)
            {
                var shouldRetry = false;
                var errorMessage = ex.Message;
                
                // 识别可以重试的错误类型
                if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException ||
                    (ex.InnerException is IOException && ex.Message.Contains("unexpected EOF")))
                {
                    shouldRetry = retryCount < maxRetries;
                }
                
                if (ex.InnerException != null)
                {
                    errorMessage += $" 内部错误: {ex.InnerException.Message}";
                    
                    // 提供SSL连接问题的详细说明
                    if (ex.InnerException is System.Security.Authentication.AuthenticationException)
                    {
                        errorMessage += " - SSL连接验证失败。请检查服务器证书是否有效，或在开发环境中考虑设置IgnoreSslCertificateErrors=true";
                    }
                }
                
                if (shouldRetry)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "获取配置失败，将在 {Delay} 秒后进行第 {RetryCount}/{MaxRetries} 次重试: {Message}", 
                        delay.TotalSeconds, retryCount, maxRetries, errorMessage);
                    
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30)); // 指数退避策略
                    continue;
                }
                
                _logger.LogError(ex, "获取应用 {AppId} 在 {Environment} 环境的配置失败：{Message}", 
                    _options.AppId, _options.Environment, errorMessage);
                throw;
            }
        }
    }

    /// <summary>
    /// 更新应用密钥
    /// </summary>
    public void UpdateAppSecret(string secret)
    {
        // 更新 HttpClient 的认证头
        if (!string.IsNullOrEmpty(secret))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", secret);
        }
    }
} 