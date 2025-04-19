using CodeSpirit.ExamApi.Settings;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.ExamApi.Services.LLM;

/// <summary>
/// 默认LLM客户端工厂实现
/// </summary>
public class DefaultLLMClientFactory : ILLMClientFactory
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DefaultLLMClientFactory> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    
    private LLMSettings? _cachedSettings;

    /// <summary>
    /// 初始化默认LLM客户端工厂
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="configuration">配置</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="serviceProvider">服务提供者</param>
    public DefaultLLMClientFactory(
        ISettingsService settingsService,
        ILogger<DefaultLLMClientFactory> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<ILLMClient?> CreateClientAsync()
    {
        return await CreateClientAsync(false);
    }

    /// <inheritdoc/>
    public async Task<ILLMClient?> CreateClientAsync(bool forceRefreshSettings)
    {
        // 如果需要强制刷新设置或缓存为空，则获取最新设置
        if (forceRefreshSettings || _cachedSettings == null)
        {
            _cachedSettings = await GetLLMSettingsAsync();
        }

        if (_cachedSettings == null)
        {
            _logger.LogError("未能获取有效的LLM设置");
            return null;
        }

        // 创建日志记录器
        var clientLogger = (ILogger<DefaultLLMClient>)_serviceProvider
            .GetRequiredService(typeof(ILogger<DefaultLLMClient>));

        // 创建客户端
        return new DefaultLLMClient(clientLogger, _cachedSettings, _httpClientFactory);
    }

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    private async Task<LLMSettings?> GetLLMSettingsAsync()
    {
        _logger.LogDebug("开始获取LLM设置");
        try
        {
            // 尝试从设置服务获取
            _logger.LogDebug("从设置服务获取LLM设置");
            var settings = await _settingsService.GetGlobalSettingAsync<LLMSettings>("ExamApi", "LLMSettings");
            
            // 如果设置不为null且API密钥不为空，则返回
            if (settings != null && !string.IsNullOrEmpty(settings.ApiKey))
            {
                _logger.LogInformation("成功从设置服务获取LLM设置: 模型={ModelName}, API={ApiBaseUrl}", 
                    settings.ModelName, settings.ApiBaseUrl);
                return settings;
            }
            
            // 如果从设置服务获取失败，则从配置文件中获取
            _logger.LogInformation("从设置服务获取LLM设置失败或API密钥为空，尝试从配置文件获取");
            
            var llmSettings = new LLMSettings
            {
                ApiBaseUrl = _configuration["LLM:ApiBaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ApiKey = _configuration["LLM:ApiKey"] ?? string.Empty,
                ModelName = _configuration["LLM:ModelName"] ?? "qwen-plus",
                TimeoutSeconds = int.TryParse(_configuration["LLM:TimeoutSeconds"], out int timeout) ? timeout : 120,
                MaxTokens = int.TryParse(_configuration["LLM:MaxTokens"], out int maxTokens) ? maxTokens : 2048,
                UseProxy = bool.TryParse(_configuration["LLM:UseProxy"], out bool useProxy) && useProxy,
                ProxyAddress = _configuration["LLM:ProxyAddress"]
            };
            
            _logger.LogDebug("从配置文件加载的设置: 模型={ModelName}, API={ApiBaseUrl}, 超时={TimeoutSeconds}秒", 
                llmSettings.ModelName, llmSettings.ApiBaseUrl, llmSettings.TimeoutSeconds);
            
            // 验证从配置文件获取的设置是否有效
            if (string.IsNullOrEmpty(llmSettings.ApiKey))
            {
                _logger.LogWarning("从配置文件获取的LLM设置API密钥为空");
                return null;
            }
            
            // 验证和纠正设置
            ValidateAndCorrectSettings(llmSettings);
            
            return llmSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM设置时发生错误: {ErrorMessage}", ex.Message);
            
            // 尝试返回基本设置以保持服务可用
            _logger.LogWarning("使用默认LLM设置");
            return new LLMSettings
            {
                ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1",
                ApiKey = _configuration["LLM:ApiKey"] ?? string.Empty,
                ModelName = "qwen-plus",
                TimeoutSeconds = 120,
                MaxTokens = 2048
            };
        }
    }

    /// <summary>
    /// 验证并纠正LLM设置
    /// </summary>
    private void ValidateAndCorrectSettings(LLMSettings settings)
    {
        // 模型名称验证
        if (string.IsNullOrEmpty(settings.ModelName))
        {
            _logger.LogWarning("模型名称为空，使用默认值qwen-plus");
            settings.ModelName = "qwen-plus";
        }
        
        // ApiBaseUrl验证
        if (string.IsNullOrEmpty(settings.ApiBaseUrl))
        {
            _logger.LogWarning("API基础地址为空，使用默认值https://dashscope.aliyuncs.com/compatible-mode/v1");
            settings.ApiBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
        }
        
        // 超时时间合理性验证
        if (settings.TimeoutSeconds < 10 || settings.TimeoutSeconds > 300)
        {
            _logger.LogWarning("超时时间 {TimeoutSeconds} 秒不合理，使用默认值120秒", settings.TimeoutSeconds);
            settings.TimeoutSeconds = 120;
        }
        
        // 令牌数合理性验证
        if (settings.MaxTokens < 100 || settings.MaxTokens > 8000)
        {
            _logger.LogWarning("最大令牌数 {MaxTokens} 不合理，使用默认值2048", settings.MaxTokens);
            settings.MaxTokens = 2048;
        }
    }
} 