using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Settings.Models;
using CodeSpirit.Web.Resources;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Dtos.Settings;

/// <summary>
/// 设置项Dto
/// </summary>
public class SettingItemDto
{
    /// <summary>
    /// 设置ID
    /// </summary>
    [Display(Name = nameof(Id), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Hidden = true)]
    public long Id { get; set; }

    /// <summary>
    /// 模块
    /// </summary>
    [Display(Name = nameof(Module), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Sortable = true)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 设置键
    /// </summary>
    [Display(Name = nameof(Key), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Sortable = true)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置值
    /// </summary>
    [Display(Name = nameof(Value), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Type = "text")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 设置名称
    /// </summary>
    [Display(Name = nameof(Name), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Sortable = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设置描述
    /// </summary>
    [Display(Name = nameof(Description), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Type = "text")]
    public string? Description { get; set; }

    /// <summary>
    /// 设置类型
    /// </summary>
    [Display(Name = nameof(ValueType), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Type = "mapping")]
    public SettingValueType ValueType { get; set; }

    /// <summary>
    /// 设置范围
    /// </summary>
    [Display(Name = nameof(Scope), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Type = "mapping")]
    public SettingScope Scope { get; set; }

    /// <summary>
    /// 对象ID（如用户ID、租户ID等）
    /// </summary>
    [Display(Name = nameof(ScopeId), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Sortable = true)]
    public string? ScopeId { get; set; }

    /// <summary>
    /// 设置分组
    /// </summary>
    [Display(Name = nameof(Group), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Sortable = true)]
    public string? Group { get; set; }

    /// <summary>
    /// 是否系统预设
    /// </summary>
    [Display(Name = nameof(IsSystemDefault), ResourceType = typeof(SettingsDisplayResources))]
    [AmisColumn(Type = "status")]
    public bool IsSystemDefault { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Display(Name = nameof(CreatedAt), ResourceType = typeof(SettingsDisplayResources))]
    [DateColumn]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [Display(Name = nameof(UpdatedAt), ResourceType = typeof(SettingsDisplayResources))]
    [DateColumn]
    public DateTime? UpdatedAt { get; set; }
}

