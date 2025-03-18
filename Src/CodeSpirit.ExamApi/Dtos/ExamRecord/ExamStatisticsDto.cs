using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 考试统计DTO
/// </summary>
public class ExamStatisticsDto
{
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [DisplayName("考试设置ID")]
    public long ExamSettingId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string ExamName { get; set; }
    
    /// <summary>
    /// 参加人数
    /// </summary>
    [DisplayName("参加人数")]
    public int TotalParticipants { get; set; }
    
    /// <summary>
    /// 完成人数
    /// </summary>
    [DisplayName("完成人数")]
    public int CompletedCount { get; set; }
    
    /// <summary>
    /// 通过人数
    /// </summary>
    [DisplayName("通过人数")]
    public int PassedCount { get; set; }
    
    /// <summary>
    /// 通过率
    /// </summary>
    [DisplayName("通过率")]
    public decimal PassRate { get; set; }
    
    /// <summary>
    /// 平均分
    /// </summary>
    [DisplayName("平均分")]
    public decimal AverageScore { get; set; }
    
    /// <summary>
    /// 最高分
    /// </summary>
    [DisplayName("最高分")]
    public double HighestScore { get; set; }
    
    /// <summary>
    /// 最低分
    /// </summary>
    [DisplayName("最低分")]
    public double LowestScore { get; set; }
    
    /// <summary>
    /// 平均完成时间（分钟）
    /// </summary>
    [DisplayName("平均完成时间")]
    public double AverageCompletionTime { get; set; }
    
    /// <summary>
    /// 作弊嫌疑人数
    /// </summary>
    [DisplayName("作弊嫌疑人数")]
    public int CheatingSuspicionCount { get; set; }
} 