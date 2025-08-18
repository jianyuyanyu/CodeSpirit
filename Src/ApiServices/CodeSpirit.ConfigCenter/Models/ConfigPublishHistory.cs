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
    /// 环境
    /// </summary>
    [Required]
    [DisplayName("环境")]
    public required string Environment { get; set; }

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
    /// 配置项发布历史记录集合
    /// </summary>
    public ICollection<ConfigItemPublishHistory> ConfigItemPublishHistories { get; set; } = new List<ConfigItemPublishHistory>();
} 