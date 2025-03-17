using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 练习记录实体
/// </summary>
public class PracticeRecord
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public int Id { get; set; }
    
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
    public int? MockExamId { get; set; }
}