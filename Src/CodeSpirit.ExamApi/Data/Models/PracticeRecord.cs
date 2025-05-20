using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Data;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 练习记录实体
/// </summary>
public class PracticeRecord : LongKeyAuditableEntityBase
{    
    /// <summary>
    /// 考生ID
    /// </summary>
    public long StudentId { get; set; }
    
    /// <summary>
    /// 考生
    /// </summary>
    public Student Student { get; set; } = null!;
    
    /// <summary>
    /// 题目ID
    /// </summary>
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目
    /// </summary>
    public Question Question { get; set; } = null!;
    
    /// <summary>
    /// 练习会话ID
    /// </summary>
    public long PracticeSessionId { get; set; }
    
    /// <summary>
    /// 练习会话
    /// </summary>
    public PracticeSession PracticeSession { get; set; }
    
    /// <summary>
    /// 练习类型
    /// </summary>
    public PracticeType PracticeType { get; set; }
    
    /// <summary>
    /// 考生回答
    /// </summary>
    [Required]
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正确
    /// </summary>
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 是否标记
    /// </summary>
    public bool IsMarked { get; set; }
    
    /// <summary>
    /// 耗时（秒）
    /// </summary>
    public int TimeSpent { get; set; }
    
    /// <summary>
    /// 练习时间
    /// </summary>
    public DateTime PracticeTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 模拟考试ID（如果是模拟考试的练习）
    /// </summary>
    public long? MockExamId { get; set; }
}