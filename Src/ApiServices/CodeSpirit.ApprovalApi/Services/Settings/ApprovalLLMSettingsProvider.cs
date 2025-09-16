using CodeSpirit.LLM.Settings;
using CodeSpirit.Settings.Services.Interfaces;

namespace CodeSpirit.ApprovalApi.Services.Settings;

/// <summary>
/// 审批系统LLM设置提供者
/// </summary>
public class ApprovalLLMSettingsProvider : ISettingsProvider
{
    private readonly ISettingsService _settingsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApprovalLLMSettingsProvider> _logger;
    
    private const string ApprovalModule = "ApprovalApi";

    /// <summary>
    /// 初始化LLM设置提供者
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    public ApprovalLLMSettingsProvider(
        ISettingsService settingsService,
        IConfiguration configuration,
        ILogger<ApprovalLLMSettingsProvider> logger)
    {
        _settingsService = settingsService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new()
    {
        if (typeof(T) == typeof(LLMSettings) && settingsKey == "LLMSettings")
        {
            // 从设置服务获取LLM设置
            var settings = await _settingsService.GetGlobalSettingAsync<LLMSettings>(ApprovalModule, "LLMSettings");
            
            // 如果设置为空，则从配置文件中获取默认设置
            if (settings == null)
            {
                _logger.LogWarning("从设置服务获取LLM设置失败，使用配置文件中的默认设置");
                
                settings = new LLMSettings
                {
                    ApiBaseUrl = _configuration["LLM:ApiBaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1",
                    ApiKey = _configuration["LLM:ApiKey"] ?? string.Empty,
                    ModelName = _configuration["LLM:ModelName"] ?? "qwen-plus",
                    TimeoutSeconds = int.TryParse(_configuration["LLM:TimeoutSeconds"], out int timeout) ? timeout : 120,
                    MaxTokens = int.TryParse(_configuration["LLM:MaxTokens"], out int maxTokens) ? maxTokens : 2048,
                    UseProxy = bool.TryParse(_configuration["LLM:UseProxy"], out bool useProxy) && useProxy,
                    ProxyAddress = _configuration["LLM:ProxyAddress"]
                };
            }
            
            return settings as T;
        }
        
        _logger.LogWarning("尝试获取不支持的设置类型: {SettingsType}, 键: {SettingsKey}", typeof(T).Name, settingsKey);
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new()
    {
        if (typeof(T) == typeof(LLMSettings) && settingsKey == "LLMSettings")
        {
            try
            {
                // 保存LLM设置到设置服务
                return await _settingsService.SetGlobalSettingAsync(ApprovalModule, "LLMSettings", settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存LLM设置失败: {ErrorMessage}", ex.Message);
                return false;
            }
        }
        
        _logger.LogWarning("尝试保存不支持的设置类型: {SettingsType}, 键: {SettingsKey}", typeof(T).Name, settingsKey);
        return false;
    }
}
