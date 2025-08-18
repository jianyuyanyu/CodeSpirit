using CodeSpirit.Core;
using CodeSpirit.Shared.Entities.Interfaces;

namespace CodeSpirit.Settings.Models;

/// <summary>
/// 设置项
/// </summary>
public class SettingItem : AuditableEntityBase<long>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = "default";
    
    /// <summary>
    /// 模块
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Module { get; set; }
    
    /// <summary>
    /// 设置键
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Key { get; set; }
    
    /// <summary>
    /// 设置值
    /// </summary>
    [Required]
    [StringLength(4000)]
    public required string Value { get; set; }
    
    /// <summary>
    /// 设置名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// 设置描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 设置类型
    /// </summary>
    [Required]
    public SettingValueType ValueType { get; set; } = SettingValueType.String;
    
    /// <summary>
    /// 设置范围
    /// </summary>
    [Required]
    public SettingScope Scope { get; set; } = SettingScope.Global;
    
    /// <summary>
    /// 作用对象ID，如用户ID
    /// </summary>
    [StringLength(50)]
    public string? ScopeId { get; set; }
    
    /// <summary>
    /// 是否系统预设
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;
    
    /// <summary>
    /// 设置分组
    /// </summary>
    [StringLength(50)]
    public string? Group { get; set; }
    
    /// <summary>
    /// 设置选项（JSON格式，用于单选、多选等）
    /// </summary>
    [StringLength(2000)]
    public string? Options { get; set; }
    
    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; } = 1;
} 