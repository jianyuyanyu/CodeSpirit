using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 题目版本实体
/// </summary>
public class QuestionVersion : AuditableEntityBase<int>
{
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
    /// 版本号
    /// </summary>
    [Required]
    public int Version { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目选项
    /// </summary>
    [Required]
    [StringLength(2000)]
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
    /// 题目分值
    /// </summary>
    [Range(0, 100)]
    public int DefaultScore { get; set; }

    /// <summary>
    /// 标签（JSON格式存储）
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 修改原因
    /// </summary>
    [StringLength(500)]
    public string? ChangeReason { get; set; }
} 