using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 回答详情实体
/// </summary>
public class ResponseAnswer : AuditableEntityBase<int>
{
    /// <summary>
    /// 回答ID
    /// </summary>
    [Required]
    public int ResponseId { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 回答文本
    /// </summary>
    [StringLength(4000)]
    public string? AnswerText { get; set; }

    /// <summary>
    /// 回答值（用于选择题等）
    /// </summary>
    [StringLength(2000)]
    public string? AnswerValue { get; set; }

    /// <summary>
    /// 回答时间
    /// </summary>
    [Required]
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的回答
    /// </summary>
    public virtual SurveyResponse Response { get; set; } = null!;

    /// <summary>
    /// 关联的题目
    /// </summary>
    public virtual Question Question { get; set; } = null!;
}
