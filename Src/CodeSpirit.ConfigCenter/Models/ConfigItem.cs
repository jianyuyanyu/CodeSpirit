using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.ConfigCenter.Models;
/// <summary>
/// 配置项实体
/// </summary>
public class ConfigItem : AuditableEntityBase<int>
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用ID")]
    public required string AppId { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("配置键")]
    public required string Key { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("配置值")]
    public required string Value { get; set; }

    /// <summary>
    /// 应用环境，如Development/Staging/Production
    /// </summary>
    [Required]
    [DisplayName("应用环境")]
    public required EnvironmentType Environment { get; set; } = EnvironmentType.Development;

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
    /// 配置值类型（如：text/json/yaml等）
    /// </summary>
    [Required]
    [DisplayName("配置类型")]
    public ConfigValueType ValueType { get; set; } = ConfigValueType.String;

    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 配置状态
    /// </summary>
    [DisplayName("配置状态")]
    public ConfigStatus Status { get; set; } = ConfigStatus.Init;

    /// <summary>
    /// 所属应用
    /// </summary>
    public App App { get; set; }
} 