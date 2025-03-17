using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 错题记录实体
/// </summary>
public class WrongQuestion : AuditableEntityBase<int>
{
    /// <summary>
    /// 考生ID
    /// </summary>
    public int StudentId { get; set; }
    
    /// <summary>
    /// 考生
    /// </summary>
    public Student Student { get; set; } = null!;
    
    /// <summary>
    /// 题目ID
    /// </summary>
    public int QuestionId { get; set; }
    
    /// <summary>
    /// 题目
    /// </summary>
    public Question Question { get; set; } = null!;
    
    /// <summary>
    /// 错误次数
    /// </summary>
    public int WrongCount { get; set; } = 1;
    
    /// <summary>
    /// 最后一次错误答案
    /// </summary>
    [Required]
    public string LastWrongAnswer { get; set; } = string.Empty;
        
    /// <summary>
    /// 最后错误时间
    /// </summary>
    public DateTime LastWrongTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 分类标签（如"重点复习"等）
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// 考生笔记
    /// </summary>
    public string? Notes { get; set; }
}