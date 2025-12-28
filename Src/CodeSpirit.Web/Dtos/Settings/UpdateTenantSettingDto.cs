using CodeSpirit.Web.Resources;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Dtos.Settings;

/// <summary>
/// 更新租户设置Dto
/// </summary>
public class UpdateTenantSettingDto
{
    /// <summary>
    /// 模块
    /// </summary>
    [Required(ErrorMessage = "模块不能为空")]
    [StringLength(50, ErrorMessage = "模块名称长度不能超过50")]
    [Display(Name = nameof(Module), ResourceType = typeof(SettingsDisplayResources))]
    public required string Module { get; set; }

    /// <summary>
    /// 设置键
    /// </summary>
    [Required(ErrorMessage = "设置键不能为空")]
    [StringLength(100, ErrorMessage = "设置键长度不能超过100")]
    [Display(Name = nameof(Key), ResourceType = typeof(SettingsDisplayResources))]
    public required string Key { get; set; }

    /// <summary>
    /// 设置值
    /// </summary>
    [Required(ErrorMessage = "设置值不能为空")]
    [StringLength(4000, ErrorMessage = "设置值长度不能超过4000")]
    [Display(Name = nameof(Value), ResourceType = typeof(SettingsDisplayResources))]
    public required string Value { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    [StringLength(500, ErrorMessage = "变更原因长度不能超过500")]
    [Display(Name = nameof(Reason), ResourceType = typeof(SettingsDisplayResources))]
    public string? Reason { get; set; }
}

