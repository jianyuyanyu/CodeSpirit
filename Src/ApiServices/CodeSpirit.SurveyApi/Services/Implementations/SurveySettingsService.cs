using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.Settings;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Settings.Services.Interfaces;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷设置服务实现
/// </summary>
public class SurveySettingsService : ISurveySettingsService, IScopedDependency
{
    private readonly ISettingsService _settingsService;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveySettingsService> _logger;
    private const string MODULE_NAME = "Survey";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settingsService">设置服务</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    public SurveySettingsService(
        ISettingsService settingsService,
        IMapper mapper,
        ILogger<SurveySettingsService> logger)
    {
        _settingsService = settingsService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取问卷系统设置
    /// </summary>
    /// <returns>设置信息</returns>
    public async Task<SurveySettingsDto> GetSurveySettingsAsync()
    {
        var settings = new SurveySettingsDto();

        try
        {
            // LLM设置
            settings.MaxPromptLength = await GetSettingAsync<int>("MaxPromptLength");
            settings.MaxTokens = await GetSettingAsync<int>("MaxTokens");
            settings.EnableLLMInsights = await GetSettingAsync<bool>("EnableLLMInsights");

            // 自动保存设置
            settings.AutoSaveEnabled = await GetSettingAsync<bool>("AutoSaveEnabled");
            settings.AutoSaveIntervalSeconds = await GetSettingAsync<int>("AutoSaveIntervalSeconds");
            settings.AutoSaveMaxDataSizeKB = await GetSettingAsync<int>("AutoSaveMaxDataSizeKB");
            settings.AutoSaveRetentionDays = await GetSettingAsync<int>("AutoSaveRetentionDays");

            // 默认限制设置
            settings.MaxSubmissionsPerIp = await GetSettingAsync<int>("MaxSubmissionsPerIp");
            settings.AllowMultipleSubmissions = await GetSettingAsync<bool>("AllowMultipleSubmissions");
            settings.ResponseCountLimit = await GetSettingAsync<int>("ResponseCountLimit");

            // 分析设置
            settings.AnalysisCacheExpirationMinutes = await GetSettingAsync<int>("AnalysisCacheExpirationMinutes");
            settings.EnableRealTimeAnalysis = await GetSettingAsync<bool>("EnableRealTimeAnalysis");

            // 通知设置
            settings.EnableEmailNotification = await GetSettingAsync<bool>("EnableEmailNotification");
            settings.NotificationEmails = await GetSettingAsync<string>("NotificationEmails");
            settings.NotificationThreshold = await GetSettingAsync<int>("NotificationThreshold");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问卷设置失败");
            // 返回默认设置
            settings = new SurveySettingsDto();
        }

        return settings;
    }

    /// <summary>
    /// 更新问卷系统设置
    /// </summary>
    /// <param name="settings">设置信息</param>
    /// <returns>异步任务</returns>
    public async Task UpdateSurveySettingsAsync(SurveySettingsDto settings)
    {
        try
        {
            // LLM设置
            await UpdateSettingAsync("MaxPromptLength", settings.MaxPromptLength);
            await UpdateSettingAsync("MaxTokens", settings.MaxTokens);
            await UpdateSettingAsync("EnableLLMInsights", settings.EnableLLMInsights);

            // 自动保存设置
            await UpdateSettingAsync("AutoSaveEnabled", settings.AutoSaveEnabled);
            await UpdateSettingAsync("AutoSaveIntervalSeconds", settings.AutoSaveIntervalSeconds);
            await UpdateSettingAsync("AutoSaveMaxDataSizeKB", settings.AutoSaveMaxDataSizeKB);
            await UpdateSettingAsync("AutoSaveRetentionDays", settings.AutoSaveRetentionDays);

            // 默认限制设置
            await UpdateSettingAsync("MaxSubmissionsPerIp", settings.MaxSubmissionsPerIp);
            await UpdateSettingAsync("AllowMultipleSubmissions", settings.AllowMultipleSubmissions);
            await UpdateSettingAsync("ResponseCountLimit", settings.ResponseCountLimit);

            // 分析设置
            await UpdateSettingAsync("AnalysisCacheExpirationMinutes", settings.AnalysisCacheExpirationMinutes);
            await UpdateSettingAsync("EnableRealTimeAnalysis", settings.EnableRealTimeAnalysis);

            // 通知设置
            await UpdateSettingAsync("EnableEmailNotification", settings.EnableEmailNotification);
            await UpdateSettingAsync("NotificationEmails", settings.NotificationEmails);
            await UpdateSettingAsync("NotificationThreshold", settings.NotificationThreshold);

            _logger.LogInformation("问卷系统设置已更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新问卷设置失败");
            throw new BusinessException("更新设置失败：" + ex.Message);
        }
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task ResetToDefaultSettingsAsync()
    {
        var defaultSettings = new SurveySettingsDto();
        await UpdateSurveySettingsAsync(defaultSettings);
        _logger.LogInformation("问卷系统设置已重置为默认值");
    }

    /// <summary>
    /// 获取指定设置项的值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <returns>设置值</returns>
    public async Task<T> GetSettingAsync<T>(string key)
    {
        try
        {
            var value = await _settingsService.GetGlobalSettingAsync(MODULE_NAME, key);
            if (value == null)
            {
                return GetDefaultValue<T>(key);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取设置项 {Key} 失败，使用默认值", key);
            return GetDefaultValue<T>(key);
        }
    }

    /// <summary>
    /// 更新指定设置项的值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    /// <returns>异步任务</returns>
    public async Task UpdateSettingAsync<T>(string key, T value)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        await _settingsService.SetGlobalSettingAsync(MODULE_NAME, key, stringValue);
    }

    /// <summary>
    /// 获取自动保存设置
    /// </summary>
    /// <returns>自动保存设置</returns>
    public async Task<AutoSaveSettings> GetAutoSaveSettingsAsync()
    {
        return new AutoSaveSettings
        {
            Enabled = await GetSettingAsync<bool>("AutoSaveEnabled"),
            IntervalSeconds = await GetSettingAsync<int>("AutoSaveIntervalSeconds"),
            MaxDataSizeKB = await GetSettingAsync<int>("AutoSaveMaxDataSizeKB"),
            RetentionDays = await GetSettingAsync<int>("AutoSaveRetentionDays")
        };
    }

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    /// <returns>LLM设置</returns>
    public async Task<LLMSettings> GetLLMSettingsAsync()
    {
        return new LLMSettings
        {
            MaxPromptLength = await GetSettingAsync<int>("MaxPromptLength"),
            MaxTokens = await GetSettingAsync<int>("MaxTokens"),
            EnableInsights = await GetSettingAsync<bool>("EnableLLMInsights")
        };
    }

    /// <summary>
    /// 获取默认限制设置
    /// </summary>
    /// <returns>默认限制设置</returns>
    public async Task<DefaultRestrictionsSettings> GetDefaultRestrictionsSettingsAsync()
    {
        return new DefaultRestrictionsSettings
        {
            MaxSubmissionsPerIp = await GetSettingAsync<int>("MaxSubmissionsPerIp"),
            AllowMultipleSubmissions = await GetSettingAsync<bool>("AllowMultipleSubmissions"),
            ResponseCountLimit = await GetSettingAsync<int>("ResponseCountLimit")
        };
    }

    /// <summary>
    /// 获取默认值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <returns>默认值</returns>
    private T GetDefaultValue<T>(string key)
    {
        var defaultSettings = new SurveySettingsDto();
        var property = typeof(SurveySettingsDto).GetProperty(key);
        if (property != null)
        {
            var value = property.GetValue(defaultSettings);
            if (value is T typedValue)
            {
                return typedValue;
            }
        }

        return default(T)!;
    }
}
