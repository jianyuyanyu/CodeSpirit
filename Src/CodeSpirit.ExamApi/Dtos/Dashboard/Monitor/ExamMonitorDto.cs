using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.Monitor;

/// <summary>
/// 监考大屏显示考试信息DTO
/// </summary>
public class ExamMonitorDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; }
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    public string Description { get; set; }
    
    /// <summary>
    /// 考试时长(分钟)
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public double TotalScore { get; set; }
    
    /// <summary>
    /// 总题目数
    /// </summary>
    [DisplayName("总题目数")]
    public int TotalQuestions { get; set; }
    
    /// <summary>
    /// 参考人数
    /// </summary>
    [DisplayName("参考人数")]
    public int TotalParticipants { get; set; }
    
    /// <summary>
    /// 在线人数
    /// </summary>
    [DisplayName("在线人数")]
    public int OnlineCount { get; set; }
    
    /// <summary>
    /// 已提交考生数量
    /// </summary>
    [DisplayName("已提交人数")]
    public int SubmittedCount { get; set; }
    
    /// <summary>
    /// 作弊嫌疑考生数量
    /// </summary>
    [DisplayName("作弊嫌疑人数")]
    public int SuspiciousCount { get; set; }
    
    /// <summary>
    /// 考试状态文本
    /// </summary>
    [DisplayName("考试状态")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 考生列表
    /// </summary>
    [DisplayName("考生列表")]
    public List<ExamStudentMonitorDto> Students { get; set; } = new List<ExamStudentMonitorDto>();
    
    /// <summary>
    /// 服务器当前时间（本地时间）
    /// </summary>
    [DisplayName("服务器时间")]
    public DateTime ServerTime { get; set; }
    
    /// <summary>
    /// 最近更新时间（用于前端显示）
    /// </summary>
    [DisplayName("最近更新")]
    public string LastUpdate { get; set; } = string.Empty;
} 