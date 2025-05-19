using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 创建练习记录DTO
/// </summary>
public class CreatePracticeRecordDto
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [Required]
    [DisplayName("考生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [DisplayName("题目类型")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [Required]
    [DisplayName("题目内容")]
    public string QuestionContent { get; set; } = string.Empty;
    
    /// <summary>
    /// 练习类型
    /// </summary>
    [Required]
    [DisplayName("练习类型")]
    public PracticeType PracticeType { get; set; }
    
    /// <summary>
    /// 考生回答
    /// </summary>
    [Required]
    [DisplayName("考生回答")]
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [Required]
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 耗时（秒）
    /// </summary>
    [Range(0, int.MaxValue)]
    [DisplayName("耗时（秒）")]
    public int TimeSpent { get; set; }
    
    /// <summary>
    /// 练习时间
    /// </summary>
    [DisplayName("练习时间")]
    public DateTime PracticeTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 模拟考试ID
    /// </summary>
    [DisplayName("模拟考试ID")]
    public long? MockExamId { get; set; }
    
    /// <summary>
    /// 练习设置ID
    /// </summary>
    [DisplayName("练习设置ID")]
    public long? PracticeSettingId { get; set; }
} 