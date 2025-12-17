using CodeSpirit.Localization.Constants;

namespace CodeSpirit.Localization.Models;

/// <summary>
/// 本地化配置选项
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// 默认语言
    /// </summary>
    public string DefaultCulture { get; set; } = CultureConstants.DefaultCulture;

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public List<SupportedCulture> SupportedCultures { get; set; } = new()
    {
        new() { Code = CultureConstants.Chinese, DisplayName = "简体中文" },
        new() { Code = CultureConstants.English, DisplayName = "English" }
    };

    /// <summary>
    /// 是否启用租户级语言配置
    /// </summary>
    public bool EnableTenantLevelLanguage { get; set; } = true;

    /// <summary>
    /// 是否启用用户级语言配置
    /// </summary>
    public bool EnableUserLevelLanguage { get; set; } = true;

    /// <summary>
    /// 是否回退到父级文化
    /// </summary>
    public bool FallbackToParentCultures { get; set; } = true;

    /// <summary>
    /// Settings 模块名
    /// </summary>
    public string SettingsModule { get; set; } = CultureConstants.SettingsModule;

    /// <summary>
    /// Settings 键名配置
    /// </summary>
    public SettingsKeysOptions SettingsKeys { get; set; } = new();
}

/// <summary>
/// Settings 键名配置
/// </summary>
public class SettingsKeysOptions
{
    /// <summary>
    /// 全局默认语言键名
    /// </summary>
    public string GlobalDefault { get; set; } = CultureConstants.GlobalDefaultLanguageKey;

    /// <summary>
    /// 租户默认语言键名
    /// </summary>
    public string TenantDefault { get; set; } = CultureConstants.TenantDefaultLanguageKey;

    /// <summary>
    /// 用户偏好语言键名
    /// </summary>
    public string UserPreference { get; set; } = CultureConstants.UserPreferredLanguageKey;
}
