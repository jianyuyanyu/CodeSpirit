using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CodeSpirit.ConfigCenter.Client.Models;
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
        try
        {
            _logger.LogInformation("正在获取应用 {AppId} 在 {Environment} 环境的配置", 
                _options.AppId, _options.Environment);
            
            // 添加身份验证头
            if (!string.IsNullOrEmpty(_options.AppSecret))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _options.AppSecret);
            }
            
            var response = await _httpClient.GetAsync(
                $"/api/config/client/ConfigItems/{_options.AppId}/{_options.Environment}/collection", 
                cancellationToken);
            
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
            _logger.LogError(ex, "获取应用 {AppId} 在 {Environment} 环境的配置失败：{Message}", 
                _options.AppId, _options.Environment, ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// API响应基类
    /// </summary>
    private class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
} 