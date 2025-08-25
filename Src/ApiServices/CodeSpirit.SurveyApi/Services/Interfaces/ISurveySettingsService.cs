using CodeSpirit.SurveyApi.Dtos.Settings;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷设置服务接口
/// </summary>
public interface ISurveySettingsService
{
    /// <summary>
    /// 获取问卷系统设置
    /// </summary>
    /// <returns>设置信息</returns>
    Task<SurveySettingsDto> GetSurveySettingsAsync();

    /// <summary>
    /// 更新问卷系统设置
    /// </summary>
    /// <param name="settings">设置信息</param>
    /// <returns>异步任务</returns>
    Task UpdateSurveySettingsAsync(SurveySettingsDto settings);

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    /// <returns>异步任务</returns>
    Task ResetToDefaultSettingsAsync();

    /// <summary>
    /// 获取指定设置项的值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <returns>设置值</returns>
    Task<T> GetSettingAsync<T>(string key);

    /// <summary>
    /// 更新指定设置项的值
    /// </summary>
    /// <typeparam name="T">设置值类型</typeparam>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    /// <returns>异步任务</returns>
    Task UpdateSettingAsync<T>(string key, T value);

    /// <summary>
    /// 获取自动保存设置
    /// </summary>
    /// <returns>自动保存设置</returns>
    Task<AutoSaveSettings> GetAutoSaveSettingsAsync();

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    /// <returns>LLM设置</returns>
    Task<LLMSettings> GetLLMSettingsAsync();

    /// <summary>
    /// 获取默认限制设置
    /// </summary>
    /// <returns>默认限制设置</returns>
    Task<DefaultRestrictionsSettings> GetDefaultRestrictionsSettingsAsync();
}

/// <summary>
/// 自动保存设置
/// </summary>
public class AutoSaveSettings
{
    /// <summary>
    /// 启用自动保存
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 草稿数据最大大小（KB）
    /// </summary>
    public int MaxDataSizeKB { get; set; } = 1024;

    /// <summary>
    /// 草稿保留天数
    /// </summary>
    public int RetentionDays { get; set; } = 7;
}

/// <summary>
/// LLM设置
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// 提示词最大长度
    /// </summary>
    public int MaxPromptLength { get; set; } = 2000;

    /// <summary>
    /// 最大Token数
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// 启用智能洞察
    /// </summary>
    public bool EnableInsights { get; set; } = true;
}

/// <summary>
/// 默认限制设置
/// </summary>
public class DefaultRestrictionsSettings
{
    /// <summary>
    /// 同一IP最大提交次数
    /// </summary>
    public int MaxSubmissionsPerIp { get; set; } = 1;

    /// <summary>
    /// 允许重复提交
    /// </summary>
    public bool AllowMultipleSubmissions { get; set; } = false;

    /// <summary>
    /// 默认回收量限制
    /// </summary>
    public int ResponseCountLimit { get; set; } = 0;
}
