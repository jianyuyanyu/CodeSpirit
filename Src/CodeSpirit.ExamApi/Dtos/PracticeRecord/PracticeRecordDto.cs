using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习记录DTO
/// </summary>
public class PracticeRecordDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 考生姓名
    /// </summary>
    [DisplayName("考生姓名")]
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string QuestionContent { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 练习类型
    /// </summary>
    [DisplayName("练习类型")]
    public PracticeType PracticeType { get; set; }
    
    /// <summary>
    /// 考生回答
    /// </summary>
    [DisplayName("考生回答")]
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
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
    [DisplayName("耗时(秒)")]
    public int TimeSpent { get; set; }
    
    /// <summary>
    /// 练习时间
    /// </summary>
    [DisplayName("练习时间")]
    public DateTime PracticeTime { get; set; }
    
    /// <summary>
    /// 模拟考试ID
    /// </summary>
    [DisplayName("模拟考试ID")]
    public long? MockExamId { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }
} 