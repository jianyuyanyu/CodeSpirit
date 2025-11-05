using CodeSpirit.AiFormFill.Models;
using CodeSpirit.Audit.Services.LLM;
using CodeSpirit.LLM.Settings;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.AiFormFill.Services;

/// <summary>
/// AI表单填充LLM客户端工厂
/// </summary>
public class AiFormFillLLMClientFactory : IScopedDependency
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly ILogger<AiFormFillLLMClientFactory> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILLMAuditService? _auditService;
    private readonly ITenantContext? _tenantContext;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    private AiFormFillLLMSettings? _cachedSettings;

    /// <summary>
    /// 初始化AI表单填充LLM客户端工厂
    /// </summary>
    /// <param name="settingsProvider">设置提供程序</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="configuration">配置</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <param name="auditService">LLM审计服务（可选）</param>
    /// <param name="tenantContext">租户上下文（可选）</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器（可选）</param>
    public AiFormFillLLMClientFactory(
        ISettingsProvider settingsProvider,
        ILogger<AiFormFillLLMClientFactory> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILLMAuditService? auditService = null,
        ITenantContext? tenantContext = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _settingsProvider = settingsProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 创建AI表单填充LLM客户端
    /// </summary>
    /// <param name="settingsKey">设置键名</param>
    /// <param name="forceRefreshSettings">是否强制刷新设置</param>
    /// <returns>LLM客户端</returns>
    public async Task<AiFormFillLLMClient?> CreateClientAsync(string settingsKey = "AiFormFillLLM", bool forceRefreshSettings = false)
    {
        // 如果需要强制刷新设置或缓存为空，则获取最新设置
        if (forceRefreshSettings || _cachedSettings == null)
        {
            _cachedSettings = await GetLLMSettingsAsync(settingsKey);
        }

        if (_cachedSettings == null)
        {
            _logger.LogError("未能获取有效的AI表单填充LLM设置");
            return null;
        }

        // 创建日志记录器（使用注入的日志工厂）
        var clientLogger = _loggerFactory.CreateLogger<AiFormFillLLMClient>();

        // 创建客户端，传递审计相关依赖
        return new AiFormFillLLMClient(
            clientLogger, 
            _cachedSettings, 
            _httpClientFactory,
            _auditService,
            _tenantContext,
            _httpContextAccessor);
    }

    /// <summary>
    /// 获取AI表单填充LLM设置
    /// </summary>
    /// <param name="settingsKey">设置键名</param>
    /// <returns>LLM设置</returns>
    private async Task<AiFormFillLLMSettings?> GetLLMSettingsAsync(string settingsKey)
    {
        _logger.LogDebug("开始获取AI表单填充LLM设置，键名: {SettingsKey}", settingsKey);
        
        try
        {
            // 尝试从设置服务获取
            _logger.LogDebug("从设置提供程序获取AI表单填充LLM设置");
            var settings = await _settingsProvider.GetSettingsAsync<AiFormFillLLMSettings>(settingsKey);

            // 如果设置不为null且API密钥不为空，则返回
            if (settings != null && !string.IsNullOrEmpty(settings.ApiKey))
            {
                settings.SettingsKey = settingsKey; // 设置键名
                _logger.LogInformation("成功从设置提供程序获取AI表单填充LLM设置: 模型={ModelName}, API={ApiBaseUrl}",
                    settings.ModelName, settings.ApiBaseUrl);
                return settings;
            }

            // 如果从设置服务获取失败，则从配置文件中获取
            _logger.LogInformation("从设置提供程序获取AI表单填充LLM设置失败或API密钥为空，尝试从配置文件获取");

            var configSectionKey = settingsKey == "AiFormFillLLM" ? "AiFormFillLLM" : "LLM";
            var llmSettings = new AiFormFillLLMSettings
            {
                SettingsKey = settingsKey,
                ApiBaseUrl = _configuration[$"{configSectionKey}:ApiBaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ApiKey = _configuration[$"{configSectionKey}:ApiKey"] ?? string.Empty,
                ModelName = _configuration[$"{configSectionKey}:ModelName"] ?? "qwq-plus",
                TimeoutSeconds = int.TryParse(_configuration[$"{configSectionKey}:TimeoutSeconds"], out int timeout) ? timeout : 120,
                MaxTokens = int.TryParse(_configuration[$"{configSectionKey}:MaxTokens"], out int maxTokens) ? maxTokens : 2048,
                UseProxy = bool.TryParse(_configuration[$"{configSectionKey}:UseProxy"], out bool useProxy) && useProxy,
                ProxyAddress = _configuration[$"{configSectionKey}:ProxyAddress"],
                DisableThinking = bool.TryParse(_configuration[$"{configSectionKey}:DisableThinking"], out bool disableThinking) ? disableThinking : true,
                ResponseFormatType = _configuration[$"{configSectionKey}:ResponseFormatType"] ?? "json_object",
                Temperature = double.TryParse(_configuration[$"{configSectionKey}:Temperature"], out double temperature) ? temperature : 0.1,
                TopP = double.TryParse(_configuration[$"{configSectionKey}:TopP"], out double topP) ? topP : 0.9,
                EnableStreaming = bool.TryParse(_configuration[$"{configSectionKey}:EnableStreaming"], out bool enableStreaming) && enableStreaming
            };

            _logger.LogDebug("从配置文件加载的AI表单填充设置: 模型={ModelName}, API={ApiBaseUrl}, 禁用思考={DisableThinking}, 响应格式={ResponseFormatType}",
                llmSettings.ModelName, llmSettings.ApiBaseUrl, llmSettings.DisableThinking, llmSettings.ResponseFormatType);

            // 验证从配置文件获取的设置是否有效
            if (string.IsNullOrEmpty(llmSettings.ApiKey))
            {
                _logger.LogWarning("从配置文件获取的AI表单填充LLM设置API密钥为空");
                return null;
            }

            // 验证和纠正设置
            ValidateAndCorrectSettings(llmSettings);

            return llmSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取AI表单填充LLM设置时发生错误: {ErrorMessage}", ex.Message);

            // 尝试返回基本设置以保持服务可用
            _logger.LogWarning("使用默认AI表单填充LLM设置");
            return new AiFormFillLLMSettings
            {
                SettingsKey = settingsKey,
                ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ApiKey = _configuration["LLM:ApiKey"] ?? string.Empty,
                ModelName = "qwq-plus",
                TimeoutSeconds = 120,
                MaxTokens = 2048,
                DisableThinking = true,
                ResponseFormatType = "json_object",
                Temperature = 0.1,
                TopP = 0.9,
                EnableStreaming = false
            };
        }
    }

    /// <summary>
    /// 验证并纠正AI表单填充LLM设置
    /// </summary>
    /// <param name="settings">LLM设置</param>
    private void ValidateAndCorrectSettings(AiFormFillLLMSettings settings)
    {
        // 模型名称验证
        if (string.IsNullOrEmpty(settings.ModelName))
        {
            _logger.LogWarning("AI表单填充模型名称为空，使用默认值qwq-plus");
            settings.ModelName = "qwq-plus";
        }

        // ApiBaseUrl验证
        if (string.IsNullOrEmpty(settings.ApiBaseUrl))
        {
            _logger.LogWarning("AI表单填充API基础地址为空，使用默认值https://dashscope.aliyuncs.com/compatible-mode/v1");
            settings.ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
        }

        // 超时时间合理性验证
        if (settings.TimeoutSeconds < 10 || settings.TimeoutSeconds > 300)
        {
            _logger.LogWarning("AI表单填充超时时间 {TimeoutSeconds} 秒不合理，使用默认值120秒", settings.TimeoutSeconds);
            settings.TimeoutSeconds = 120;
        }

        // 令牌数合理性验证
        if (settings.MaxTokens < 100 || settings.MaxTokens > 8000)
        {
            _logger.LogWarning("AI表单填充最大令牌数 {MaxTokens} 不合理，使用默认值2048", settings.MaxTokens);
            settings.MaxTokens = 2048;
        }

        // 温度参数验证
        if (settings.Temperature < 0.0 || settings.Temperature > 2.0)
        {
            _logger.LogWarning("AI表单填充温度参数 {Temperature} 不合理，使用默认值0.1", settings.Temperature);
            settings.Temperature = 0.1;
        }

        // Top-p参数验证
        if (settings.TopP < 0.0 || settings.TopP > 1.0)
        {
            _logger.LogWarning("AI表单填充Top-p参数 {TopP} 不合理，使用默认值0.9", settings.TopP);
            settings.TopP = 0.9;
        }

        // 响应格式类型验证
        if (string.IsNullOrEmpty(settings.ResponseFormatType))
        {
            _logger.LogWarning("AI表单填充响应格式类型为空，使用默认值json_object");
            settings.ResponseFormatType = "json_object";
        }
    }
}
