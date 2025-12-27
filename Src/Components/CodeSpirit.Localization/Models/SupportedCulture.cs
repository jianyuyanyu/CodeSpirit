namespace CodeSpirit.Localization.Models;

/// <summary>
/// 支持的语言配置
/// </summary>
public class SupportedCulture
{
    /// <summary>
    /// 语言代码（如 zh-CN, en）
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// 显示名称（如 简体中文, English）
    /// </summary>
    public required string DisplayName { get; set; }
}
