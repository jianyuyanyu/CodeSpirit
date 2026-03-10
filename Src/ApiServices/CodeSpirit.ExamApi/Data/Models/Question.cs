using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 题目
/// </summary>
public class Question : LongKeyAuditableEntityBase, IMultiTenant
{    
    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public QuestionType Type { get; set; }
    
    /// <summary>
    /// 题目难度
    /// </summary>
    [Required]
    public QuestionDifficulty Difficulty { get; set; }
    
    /// <summary>
    /// 题目选项
    /// </summary>
    [Required]
    public List<string> Options { get; set; } = [];
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 解析
    /// </summary>
    [StringLength(2000)]
    public string? Analysis { get; set; }
    
    /// <summary>
    /// 知识点（JSON格式存储）
    /// </summary>
    [StringLength(500)]
    public string? KnowledgePoints { get; set; }
    
    /// <summary>
    /// 分类ID
    /// </summary>
    [Required]
    public long CategoryId { get; set; }
    
    /// <summary>
    /// 分类
    /// </summary>
    public QuestionCategory Category { get; set; } = null!;

    /// <summary>
    /// 题目分值
    /// </summary>
    [Range(0, 100)]
    public int DefaultScore { get; set; } = 0;

    /// <summary>
    /// 题目版本
    /// </summary>
    [Required]
    public int Version { get; set; } = 1;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 正确率（百分比）
    /// </summary>
    [Range(0, 100)]
    public decimal CorrectRate { get; set; } = 0;

    /// <summary>
    /// 标签（JSON格式存储）
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 题目状态
    /// </summary>
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 发布人ID
    /// </summary>
    public long? PublishedBy { get; set; }

    /// <summary>
    /// 试卷题目关联
    /// </summary>
    public ICollection<ExamPaperQuestion> ExamPaperQuestions { get; set; } = [];

    /// <summary>
    /// 是否被试卷引用（计算属性）
    /// </summary>
    [NotMapped]
    public bool IsReferenced => ExamPaperQuestions.Any();

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}