using System.ComponentModel;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 题目类型统计DTO
/// </summary>
public class QuestionTypeStatisticsDto
{
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 题目类型名称
    /// </summary>
    [DisplayName("题目类型名称")]
    public string QuestionTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int Count { get; set; }
    
    /// <summary>
    /// 正确题目数
    /// </summary>
    [DisplayName("正确题目数")]
    public int CorrectCount { get; set; }
    
    /// <summary>
    /// 错误题目数
    /// </summary>
    [DisplayName("错误题目数")]
    public int IncorrectCount { get; set; }
    
    /// <summary>
    /// 正确率
    /// </summary>
    [DisplayName("正确率")]
    public double CorrectRate { get; set; }
} 