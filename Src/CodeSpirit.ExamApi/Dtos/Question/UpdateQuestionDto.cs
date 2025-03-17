using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

/// <summary>
/// 更新题目DTO
/// </summary>
public class UpdateQuestionDto : CreateQuestionDto
{
    /// <summary>
    /// 修改原因
    /// </summary>
    [Required(ErrorMessage = "请填写修改原因")]
    [StringLength(500, ErrorMessage = "修改原因最多500字符")]
    [DisplayName("修改原因")]
    [AmisTextareaField(MaxLength = 500, ShowCounter = true)]
    public string ChangeReason { get; set; } = string.Empty;
} 