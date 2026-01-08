using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 更新配置 DTO
/// </summary>
public class UpdateConfigDto
{
    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [StringLength(4000)]
    [DisplayName("配置值")]
    [AmisFormField(Type = "json-editor", Placeholder = "请输入配置值（支持JSON格式）")]
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
}