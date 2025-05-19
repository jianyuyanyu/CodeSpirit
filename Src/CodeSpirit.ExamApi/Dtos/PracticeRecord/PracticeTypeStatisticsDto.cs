using System.ComponentModel;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习类型统计DTO
/// </summary>
public class PracticeTypeStatisticsDto
{
    /// <summary>
    /// 练习类型
    /// </summary>
    [DisplayName("练习类型")]
    public PracticeType PracticeType { get; set; }
    
    /// <summary>
    /// 练习类型名称
    /// </summary>
    [DisplayName("练习类型名称")]
    public string PracticeTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 练习次数
    /// </summary>
    [DisplayName("练习次数")]
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