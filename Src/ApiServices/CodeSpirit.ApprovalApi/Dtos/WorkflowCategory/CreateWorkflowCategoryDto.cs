using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;

/// <summary>
/// 创建流程分类DTO
/// </summary>
[AiFormFill(
    TriggerField = "Name",
    IgnoreFields = new[] { "ParentId", "OrderIndex", "IsEnabled" },
    MaxTokens = 1000,
    EnableCache = true,
    UseIndependentLLM = true,
    CacheExpirationMinutes = 60
)]
public class CreateWorkflowCategoryDto
{
    /// <summary>
    /// 分类名称
    /// </summary>
    [Required(ErrorMessage = "分类名称不能为空")]
    [StringLength(100, ErrorMessage = "分类名称长度不能超过100个字符")]
    [DisplayName("分类名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    [StringLength(500, ErrorMessage = "分类描述长度不能超过500个字符")]
    [DisplayName("分类描述")]
    [AiFieldFill(Enabled = true, Weight = 3, Priority = 1, CustomDescription = "根据分类名称生成详细且有意义的分类描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 分类颜色（十六进制颜色值）
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "请输入有效的十六进制颜色值")]
    [DisplayName("分类颜色")]
    [AmisFormField(Type = "input-color", DefaultValue = "#1890ff")]
    [AiFieldFill(Enabled = true, Weight = 2, Priority = 3, CustomDescription = "根据分类名称选择合适的主题颜色，返回十六进制颜色值格式如：#1890ff")]
    public string? Color { get; set; }

    /// <summary>
    /// 分类图标
    /// </summary>
    [StringLength(50, ErrorMessage = "分类图标长度不能超过50个字符")]
    [DisplayName("分类图标")]
    [AmisIconField(
        IconType = "fontawesome",
        Searchable = true,
        Clearable = true,
        ShowPreview = true,
        PreviewSize = "md",
        Placeholder = "请选择图标"
    )]
    [AiFieldFill(Enabled = true, Weight = 2, Priority = 2, CustomDescription = "根据分类名称选择合适的FontAwesome图标类名，格式如：fa-solid fa-folder")]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/approval/WorkflowCategories",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false,
        Clearable = true
    )]
    public int? ParentId { get; set; }
}
