using CodeSpirit.Web.Resources;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Dtos.Settings;

/// <summary>
/// 设置值Dto
/// </summary>
public class SettingValueDto
{
    /// <summary>
    /// 模块
    /// </summary>
    [Display(Name = nameof(Module), ResourceType = typeof(SettingsDisplayResources))]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 设置键
    /// </summary>
    [Display(Name = nameof(Key), ResourceType = typeof(SettingsDisplayResources))]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置值
    /// </summary>
    [Display(Name = nameof(Value), ResourceType = typeof(SettingsDisplayResources))]
    public string? Value { get; set; }

    /// <summary>
    /// 设置名称
    /// </summary>
    [Display(Name = nameof(Name), ResourceType = typeof(SettingsDisplayResources))]
    public string Name { get; set; } = string.Empty;
}

