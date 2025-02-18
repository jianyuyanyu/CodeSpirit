using CodeSpirit.ConfigCenter.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项 DTO
/// </summary>
public class ConfigItemDto
{
    /// <summary>
    /// 配置ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用")]
    public required string AppId { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("配置键")]
    [TplColumn(template: "${key}")]
    [Badge(VisibleOn = "!onlineStatus", Level = "warning", Text = "未发布")]
    public required string Key { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("配置值")]
    public required string Value { get; set; }

    /// <summary>
    /// 应用环境
    /// </summary>
    [Required]
    [DisplayName("环境")]
    [Badge(Level = "info")]
    public required EnvironmentType Environment { get; set; }

    /// <summary>
    /// 配置分组
    /// </summary>
    [StringLength(50)]
    [DisplayName("配置组")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置说明
    /// </summary>
    [StringLength(200)]
    [DisplayName("配置描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否已发布上线
    /// </summary>
    [DisplayName("发布状态")]
    public bool OnlineStatus { get; set; }

    /// <summary>
    /// 配置值类型
    /// </summary>
    [Required]
    [DisplayName("配置类型")]
    public ConfigValueType ValueType { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本")]
    public long Version { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("状态")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    [DisplayName("配置状态")]
    public ConfigStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
} 