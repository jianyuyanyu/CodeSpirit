using CodeSpirit.LLM.Settings;

namespace CodeSpirit.IdentityApi.Services.Settings;

/// <summary>
/// 身份认证系统LLM设置提供者
/// </summary>
public class IdentityLLMSettingsProvider : ISettingsProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityLLMSettingsProvider> _logger;

    /// <summary>
    /// 初始化LLM设置提供者
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    public IdentityLLMSettingsProvider(
        IConfiguration configuration,
        ILogger<IdentityLLMSettingsProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        if (typeof(T) == typeof(LLMSettings) && settingsKey == "LLMSettings")
        {
            _logger.LogDebug("从配置文件获取身份认证系统LLM设置");
            
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
            
            _logger.LogInformation("身份认证系统LLM设置已加载: 模型={ModelName}, API={ApiBaseUrl}", 
                settings.ModelName, settings.ApiBaseUrl);
            
            return settings as T;
        }
        
        _logger.LogWarning("尝试获取不支持的设置类型: {SettingsType}, 键: {SettingsKey}", typeof(T).Name, settingsKey);
        return await Task.FromResult<T?>(null);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        _logger.LogWarning("身份认证系统LLM设置提供者不支持保存设置操作");
        await Task.CompletedTask;
        return false;
    }
}
