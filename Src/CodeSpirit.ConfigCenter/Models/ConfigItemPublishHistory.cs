using CodeSpirit.Shared.Entities;

namespace CodeSpirit.ConfigCenter.Models;

/// <summary>
/// 配置项发布历史
/// </summary>
public class ConfigItemPublishHistory : AuditableEntityBase<int>
{
    /// <summary>
    /// 所属发布历史ID
    /// </summary>
    [Required]
    [DisplayName("发布历史ID")]
    public required int ConfigPublishHistoryId { get; set; }

    /// <summary>
    /// 配置项ID
    /// </summary>
    [Required]
    [DisplayName("配置项ID")]
    public required int ConfigItemId { get; set; }

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
    /// 发布版本
    /// </summary>
    [Required]
    [DisplayName("发布版本")]
    public required long Version { get; set; }

    /// <summary>
    /// 所属发布历史
    /// </summary>
    public ConfigPublishHistory ConfigPublishHistory { get; set; }

    /// <summary>
    /// 配置项
    /// </summary>
    public ConfigItem ConfigItem { get; set; }
} 