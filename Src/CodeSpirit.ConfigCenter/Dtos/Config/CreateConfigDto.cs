using CodeSpirit.ConfigCenter.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 创建配置 DTO
/// </summary>
public class CreateConfigDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Required = true,
        Placeholder = "请选择应用"
    )]
    public required string AppId { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("配置键")]
    [RegularExpression(@"^[a-zA-Z0-9_:.]+$", ErrorMessage = "配置键只能包含字母、数字、下划线、冒号和点")]
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
    /// 配置值类型
    /// </summary>
    [Required]
    [DisplayName("配置类型")]
    public ConfigValueType ValueType { get; set; } = ConfigValueType.String;

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("状态")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 配置状态
    /// </summary>
    [DisplayName("配置状态")]
    public ConfigStatus Status { get; set; } = ConfigStatus.Init;
} 