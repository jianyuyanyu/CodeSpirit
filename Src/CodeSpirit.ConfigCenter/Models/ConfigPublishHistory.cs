using CodeSpirit.Shared.Entities;

namespace CodeSpirit.ConfigCenter.Models;

/// <summary>
/// 配置发布历史
/// </summary>
public class ConfigPublishHistory : AuditableEntityBase<int>
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用ID")]
    public required string AppId { get; set; }

    /// <summary>
    /// 配置项ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("配置项ID")]
    public required string ConfigItemId { get; set; }

    /// <summary>
    /// 发布前的值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("原始值")]
    public required string OldValue { get; set; }

    /// <summary>
    /// 发布后的值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("新值")]
    public required string NewValue { get; set; }

    /// <summary>
    /// 发布说明
    /// </summary>
    [StringLength(200)]
    [DisplayName("发布说明")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 发布版本
    /// </summary>
    [Required]
    [DisplayName("发布版本")]
    public required long Version { get; set; }

    /// <summary>
    /// 所属应用
    /// </summary>
    public App App { get; set; }

    /// <summary>
    /// 配置项
    /// </summary>
    public ConfigItem ConfigItem { get; set; }
} 