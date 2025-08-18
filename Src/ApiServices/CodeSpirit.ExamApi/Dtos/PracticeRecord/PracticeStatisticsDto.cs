using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习统计DTO
/// </summary>
public class PracticeStatisticsDto
{
    /// <summary>
    /// 学生ID
    /// </summary>
    [DisplayName("学生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 学生姓名
    /// </summary>
    [DisplayName("学生姓名")]
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// 总练习次数
    /// </summary>
    [DisplayName("总练习次数")]
    public int TotalPracticeCount { get; set; }
    
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
    
    /// <summary>
    /// 平均用时（秒）
    /// </summary>
    [DisplayName("平均用时（秒）")]
    public double AverageTimeSpent { get; set; }
    
    /// <summary>
    /// 练习类型统计
    /// </summary>
    [DisplayName("练习类型统计")]
    public List<PracticeTypeStatisticsDto> PracticeTypeStatistics { get; set; } = new();
    
    /// <summary>
    /// 题目类型统计
    /// </summary>
    [DisplayName("题目类型统计")]
    public List<QuestionTypeStatisticsDto> QuestionTypeStatistics { get; set; } = new();
    
    /// <summary>
    /// 最近练习时间
    /// </summary>
    [DisplayName("最近练习时间")]
    public DateTime? LastPracticeTime { get; set; }
} 