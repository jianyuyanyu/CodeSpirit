using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 创建配置 DTO
/// </summary>
[AiFormFill(TriggerField = nameof(Key), ApiEndpoint = "ai-fill", IgnoreFields = new[] { nameof(AppId) })]
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
    [Description("根据配置键提供合适的配置值")]
    [AmisFormField(Type = "json-editor", Placeholder = "请输入配置值（支持JSON格式）")]
    [AiFieldFill(Weight = 3, Priority = 1)]
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
    [Description("详细描述配置项的用途和作用")]
    [AiFieldFill(Weight = 2, Priority = 2)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 配置值类型
    /// </summary>
    [Required]
    [DisplayName("配置类型")]
    public ConfigValueType ValueType { get; set; } = ConfigValueType.String;

    /// <summary>
    /// 配置状态
    /// </summary>
    [DisplayName("配置状态")]
    public ConfigStatus Status { get; set; } = ConfigStatus.Init;
} 