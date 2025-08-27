using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 批量删除题目请求
/// </summary>
[DisplayName("批量删除题目")]
public class BatchDeleteQuestionsRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 要删除的题目ID列表
    /// </summary>
    [Required]
    [DisplayName("题目ID列表")]
    public List<int> QuestionIds { get; set; } = new();
}
