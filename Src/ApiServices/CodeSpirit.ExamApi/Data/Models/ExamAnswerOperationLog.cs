using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试答题操作日志实体
/// </summary>
public class ExamAnswerOperationLog : LongKeyAuditableEntityBase, IMultiTenant
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
    /// 操作类型
    /// </summary>
    [Required]
    public AnswerOperationType OperationType { get; set; }

    /// <summary>
    /// 本次操作的答案
    /// </summary>
    [StringLength(2000)]
    public string? Answer { get; set; }

    /// <summary>
    /// 操作时间（UTC）
    /// </summary>
    [Required]
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}
