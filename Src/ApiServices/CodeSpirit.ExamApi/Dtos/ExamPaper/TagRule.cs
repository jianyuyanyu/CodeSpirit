using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 标签分布规则
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
    public int Percentage { get; set; }
}

