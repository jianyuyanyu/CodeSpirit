using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试记录实体
/// </summary>
public class ExamRecord : AuditableEntityBase<int>
{
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [Required]
    public int ExamSettingId { get; set; }
    
    /// <summary>
    /// 考试设置
    /// </summary>
    public ExamSetting ExamSetting { get; set; } = null!;
    
    /// <summary>
    /// 考生ID
    /// </summary>
    [Required]
    public int StudentId { get; set; }
    
    /// <summary>
    /// 考生
    /// </summary>
    public Student Student { get; set; } = null!;
    
    /// <summary>
    /// 尝试次数
    /// </summary>
    [Required]
    public int AttemptNumber { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTime? SubmitTime { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [Required]
    public ExamRecordStatus Status { get; set; } = ExamRecordStatus.InProgress;
    
    /// <summary>
    /// 得分
    /// </summary>
    [Range(0, 1000)]
    public double? Score { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    public bool IsPassed { get; set; }
    
    /// <summary>
    /// 切屏次数
    /// </summary>
    [Required]
    public int ScreenSwitchCount { get; set; } = 0;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [Required]
    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备信息（JSON格式存储）
    /// </summary>
    [StringLength(1000)]
    public string? DeviceInfo { get; set; }
    
    /// <summary>
    /// 浏览器信息
    /// </summary>
    [StringLength(500)]
    public string? BrowserInfo { get; set; }
    
    /// <summary>
    /// 作弊嫌疑等级（0-100）
    /// </summary>
    [Range(0, 100)]
    public int CheatingSuspicionLevel { get; set; } = 0;
    
    /// <summary>
    /// 作弊记录（JSON格式存储）
    /// </summary>
    [StringLength(2000)]
    public string? CheatingSuspicionRecord { get; set; }
    
    /// <summary>
    /// 考试答题记录
    /// </summary>
    public ICollection<ExamAnswerRecord> AnswerRecords { get; set; } = new List<ExamAnswerRecord>();
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int? Duration { get; set; }
    
    /// <summary>
    /// 评语
    /// </summary>
    [StringLength(1000)]
    public string? Comments { get; set; }
    
    /// <summary>
    /// 批改人ID
    /// </summary>
    public string? GraderId { get; set; }
    
    /// <summary>
    /// 批改时间
    /// </summary>
    public DateTime? GradedTime { get; set; }
}