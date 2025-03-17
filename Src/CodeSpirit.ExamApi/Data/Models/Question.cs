using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 题目
/// </summary>
public class Question : AuditableEntityBase<int>
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
    [StringLength(1000)]
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
    public int CategoryId { get; set; }
    
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
}