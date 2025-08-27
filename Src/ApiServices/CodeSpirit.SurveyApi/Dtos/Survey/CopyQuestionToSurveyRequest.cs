using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 复制题目到问卷请求
/// </summary>
[DisplayName("复制题目到问卷")]
public class CopyQuestionToSurveyRequest
{
    /// <summary>
    /// 目标问卷ID
    /// </summary>
    [Required]
    [DisplayName("目标问卷ID")]
    public int TargetSurveyId { get; set; }

    /// <summary>
    /// 新题目标题（如果为空则使用原标题并添加"副本"后缀）
    /// </summary>
    [StringLength(500)]
    [DisplayName("新题目标题")]
    public string? NewTitle { get; set; }
}
