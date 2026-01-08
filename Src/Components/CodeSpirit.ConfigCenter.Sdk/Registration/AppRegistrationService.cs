using CodeSpirit.ConfigCenter.Sdk.Models;
using CodeSpirit.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace CodeSpirit.ConfigCenter.Sdk.Registration;

/// <summary>
/// 应用自动注册服务
/// </summary>
public class AppRegistrationService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigCenterOptions _options;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AppRegistrationService> _logger;
    private string? _currentAppId;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AppRegistrationService(
        HttpClient httpClient,
        IOptions<ConfigCenterOptions> options,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AppRegistrationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前应用ID
    /// </summary>
    public string GetCurrentAppId()
    {
        if (!string.IsNullOrEmpty(_currentAppId))
        {
            return _currentAppId;
        }

        // 优先使用配置中的 AppId
        if (!string.IsNullOrEmpty(_options.AppId))
        {
            _currentAppId = _options.AppId;
            return _currentAppId;
        }

        // 从配置中获取应用名称（Aspire 服务名称）
        var appName = _configuration["ServiceName"] 
            ?? _configuration["ApplicationName"]
            ?? _environment.ApplicationName;

        _currentAppId = appName ?? "unknown";
        return _currentAppId;
    }

    /// <summary>
    /// 自动注册应用
    /// </summary>
    public async Task<bool> RegisterAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoRegister)
        {
            _logger.LogInformation("自动注册已禁用，跳过应用注册");
            return false;
        }

        try
        {
            var appId = GetCurrentAppId();
            var appName = _configuration["ApplicationName"] ?? appId;
            var description = _configuration["ApplicationDescription"] ?? $"自动注册的应用: {appName}";

            var registrationRequest = new
            {
                Id = appId,
                Name = appName,
                Description = description,
                Secret = Guid.NewGuid().ToString("N")
            };

            var json = JsonConvert.SerializeObject(registrationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = "api/config/client/apps/register";
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("应用注册成功: {AppId}", appId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("应用注册失败: {AppId}, 状态码: {StatusCode}, 错误: {Error}", 
                    appId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用注册过程中发生错误");
            return false;
        }
    }
}

