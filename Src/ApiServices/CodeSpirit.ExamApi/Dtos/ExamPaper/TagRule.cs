using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 标签分布规则
/// 说明：标签规则按照列表顺序执行，如果一道题目拥有多个标签，将被第一个匹配的标签规则选中。
/// 可通过拖拽调整规则顺序来控制标签的优先级。
/// </summary>
[DisplayName("标签分布规则")]
public class TagRule
{
    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    [Required(ErrorMessage = "标签不能为空")]
    [StringLength(50, ErrorMessage = "标签长度不能超过50个字符")]
    [Description("选择题目标签。如果题目拥有多个标签，将按照规则顺序优先匹配")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Questions/tags",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择或输入标签"
    )]
    public string Tag { get; set; } = string.Empty;
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    [Description("该标签题目占总题数的比例，所有标签规则的比例总和必须为100%")]
    public int Percentage { get; set; }
}

