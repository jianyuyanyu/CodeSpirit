using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM.Settings;

/// <summary>
/// 基于配置的设置提供者
/// </summary>
/// <remarks>
/// 此实现从IConfiguration中读取LLM设置，简化了配置管理
/// </remarks>
public class ConfigurationBasedSettingsProvider : ISettingsProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationBasedSettingsProvider> _logger;

    /// <summary>
    /// 初始化基于配置的设置提供者
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    public ConfigurationBasedSettingsProvider(
        IConfiguration configuration,
        ILogger<ConfigurationBasedSettingsProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        try
        {
            if (typeof(T) == typeof(LLMSettings) && settingsKey == "LLMSettings")
            {
                _logger.LogDebug("从配置中获取LLM设置");
                
                var settings = new LLMSettings
                {
                    ApiBaseUrl = _configuration["LLM:ApiBaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1",
                    ApiKey = _configuration["LLM:ApiKey"] ?? string.Empty,
                    ModelName = _configuration["LLM:ModelName"] ?? "qwen-plus",
                    TimeoutSeconds = int.TryParse(_configuration["LLM:TimeoutSeconds"], out int timeout) ? timeout : 120,
                    MaxTokens = int.TryParse(_configuration["LLM:MaxTokens"], out int maxTokens) ? maxTokens : 2048,
                    UseProxy = bool.TryParse(_configuration["LLM:UseProxy"], out bool useProxy) && useProxy,
                    ProxyAddress = _configuration["LLM:ProxyAddress"]
                };

                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    _logger.LogWarning("LLM ApiKey未配置或为空");
                    return Task.FromResult<T?>(null);
                }

                _logger.LogInformation("成功从配置中获取LLM设置: 模型={ModelName}, API={ApiBaseUrl}", 
                    settings.ModelName, settings.ApiBaseUrl);

                return Task.FromResult((T?)(object)settings);
            }

            // 对于其他类型的设置，尝试从配置节绑定
            var configSection = _configuration.GetSection(settingsKey);
            if (configSection.Exists())
            {
                var instance = new T();
                configSection.Bind(instance);
                return Task.FromResult<T?>(instance);
            }

            _logger.LogWarning("未找到设置键: {SettingsKey}", settingsKey);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设置时发生错误: {SettingsKey}", settingsKey);
            return Task.FromResult<T?>(null);
        }
    }

    /// <inheritdoc/>
    public Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        _logger.LogWarning("ConfigurationBasedSettingsProvider不支持保存设置操作，设置键: {SettingsKey}", settingsKey);
        return Task.FromResult(false);
    }
}
