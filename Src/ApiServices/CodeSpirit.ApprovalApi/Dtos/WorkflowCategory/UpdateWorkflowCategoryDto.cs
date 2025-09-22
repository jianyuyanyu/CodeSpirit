using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;

/// <summary>
/// 更新流程分类DTO
/// </summary>
public class UpdateWorkflowCategoryDto
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
    public string? Description { get; set; }

    /// <summary>
    /// 分类颜色（十六进制颜色值）
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "请输入有效的十六进制颜色值")]
    [DisplayName("分类颜色")]
    [AmisFormField(Type = "input-color")]
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
    public string? Icon { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }

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
