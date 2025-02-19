using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ConfigCenter.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 更新配置 DTO
/// </summary>
public class UpdateConfigDto
{
    /// <summary>
    /// 应用环境
    /// </summary>
    [Required]
    [DisplayName("环境")]
    public required EnvironmentType Environment { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("配置值")]
    [AmisTextareaField(MinRows = 3, MaxRows = 6, ShowCounter = true, MaxLength = 500)]
    public required string Value { get; set; }

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
    public ConfigValueType ValueType { get; set; }

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
    /// 是否已发布上线
    /// </summary>
    [DisplayName("发布状态")]
    public bool OnlineStatus { get; set; }
}