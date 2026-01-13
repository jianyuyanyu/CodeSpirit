using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试答题记录实体
/// </summary>
public class ExamAnswerRecord : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Required]
    public long ExamRecordId { get; set; }

    /// <summary>
    /// 考试记录
    /// </summary>
    public ExamRecord ExamRecord { get; set; } = null!;

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public long QuestionId { get; set; }

    /// <summary>
    /// 题目
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// 题目版本ID
    /// </summary>
    [Required]
    public long QuestionVersionId { get; set; }

    /// <summary>
    /// 题目版本
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;

    /// <summary>
    /// 题目序号
    /// </summary>
    [Required]
    public int OrderNumber { get; set; }

    /// <summary>
    /// 考生答案
    /// </summary>
    [StringLength(4000)]
    public string? Answer { get; set; }

    /// <summary>
    /// 是否标记（考生标记的疑难题目）
    /// </summary>
    public bool IsMarked { get; set; }

    /// <summary>
    /// 是否正确
    /// </summary>
    public bool? IsCorrect { get; set; }

    /// <summary>
    /// 该题在试卷中的分值（来自 ExamPaperQuestion.Score）
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int QuestionScore { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    [Range(0, 100)]
    public double? Score { get; set; }

    /// <summary>
    /// 批改意见
    /// </summary>
    [StringLength(1000)]
    public string? Comments { get; set; }

    /// <summary>
    /// 批改时间
    /// </summary>
    public DateTime? GradedTime { get; set; }

    /// <summary>
    /// 批改人ID
    /// </summary>
    public string? GraderId { get; set; }

    /// <summary>
    /// 答题开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 答题提交时间
    /// </summary>
    public DateTime? SubmitTime { get; set; }

    /// <summary>
    /// 答题用时（秒）
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}