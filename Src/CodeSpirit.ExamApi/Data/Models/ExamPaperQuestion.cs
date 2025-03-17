using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 试卷题目关联实体
/// </summary>
public class ExamPaperQuestion : AuditableEntityBase<int>
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [Required]
    public long ExamPaperId { get; set; }

    /// <summary>
    /// 试卷
    /// </summary>
    public ExamPaper ExamPaper { get; set; } = null!;

    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 题目
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// 题目版本ID
    /// </summary>
    [Required]
    public int QuestionVersionId { get; set; }

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
    /// 分值
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int Score { get; set; }

    /// <summary>
    /// 是否必答
    /// </summary>
    public bool IsRequired { get; set; } = true;
}