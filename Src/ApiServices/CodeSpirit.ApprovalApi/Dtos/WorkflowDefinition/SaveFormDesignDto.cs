using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

/// <summary>
/// 保存表单设计DTO
/// </summary>
[DisplayName("保存表单设计")]
public class SaveFormDesignDto
{
    /// <summary>
    /// 表单Schema（JSON格式）
    /// </summary>
    [Required]
    [DisplayName("表单Schema")]
    [Description("符合AMIS规范的表单JSON结构")]
    [AmisFormField(Type = "json", Placeholder = "请输入表单Schema")]
    public string FormSchema { get; set; } = string.Empty;

    /// <summary>
    /// 表单标题
    /// </summary>
    [StringLength(100)]
    [DisplayName("表单标题")]
    [Description("表单的显示标题")]
    [AmisInputTextField(Placeholder = "请输入表单标题")]
    public string? FormTitle { get; set; }

    /// <summary>
    /// 备注说明
    /// </summary>
    [StringLength(500)]
    [DisplayName("备注说明")]
    [Description("保存表单设计的备注说明")]
    [AmisTextareaField(Placeholder = "请输入备注说明（可选）")]
    public string? Remarks { get; set; }
}
