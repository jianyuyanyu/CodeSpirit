using CodeSpirit.Core.Dtos;
using CodeSpirit.Web.Resources;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Dtos.Settings;

/// <summary>
/// 租户设置查询Dto
/// </summary>
public class TenantSettingQueryDto : QueryDtoBase
{
    /// <summary>
    /// 模块名称
    /// </summary>
    [Display(Name = nameof(Module), ResourceType = typeof(SettingsDisplayResources))]
    public string? Module { get; set; }

    /// <summary>
    /// 设置键
    /// </summary>
    [Display(Name = nameof(Key), ResourceType = typeof(SettingsDisplayResources))]
    public string? Key { get; set; }

    /// <summary>
    /// 设置名称
    /// </summary>
    [Display(Name = nameof(Name), ResourceType = typeof(SettingsDisplayResources))]
    public string? Name { get; set; }

    /// <summary>
    /// 设置分组
    /// </summary>
    [Display(Name = nameof(Group), ResourceType = typeof(SettingsDisplayResources))]
    public string? Group { get; set; }
}

