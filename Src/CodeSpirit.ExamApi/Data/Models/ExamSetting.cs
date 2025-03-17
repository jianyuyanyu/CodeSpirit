using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试设置实体
/// </summary>
public class ExamSetting : AuditableEntityBase<int>
{    
    /// <summary>
    /// 考试名称
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    public int ExamPaperId { get; set; }
    
    /// <summary>
    /// 试卷
    /// </summary>
    public ExamPaper ExamPaper { get; set; } = null!;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int Duration { get; set; }
    
    /// <summary>
    /// 允许考试次数
    /// </summary>
    public int AllowedAttempts { get; set; } = 1;
    
    /// <summary>
    /// 考试状态
    /// </summary>
    public ExamStatus Status { get; set; } = ExamStatus.NotStarted;
    
    /// <summary>
    /// 是否启用题目乱序
    /// </summary>
    public bool EnableRandomQuestionOrder { get; set; }
    
    /// <summary>
    /// 是否启用选项乱序
    /// </summary>
    public bool EnableRandomOptionOrder { get; set; }
    
    /// <summary>
    /// 允许切屏次数
    /// </summary>
    public int AllowedScreenSwitchCount { get; set; } = 0;
    
    /// <summary>
    /// 参加考试的学生分组
    /// </summary>
    public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
    
    /// <summary>
    /// 考试记录
    /// </summary>
    public ICollection<ExamRecord> ExamRecords { get; set; } = new List<ExamRecord>();
}