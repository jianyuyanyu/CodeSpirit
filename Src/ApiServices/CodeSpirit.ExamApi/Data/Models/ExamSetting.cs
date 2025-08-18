using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试设置实体
/// </summary>
public class ExamSetting : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 考试名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    [Required]
    public long ExamPaperId { get; set; }
    
    /// <summary>
    /// 试卷
    /// </summary>
    public ExamPaper ExamPaper { get; set; } = null!;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Required]
    [Range(1, 1440)]
    public int Duration { get; set; }
    
    /// <summary>
    /// 允许考试次数
    /// </summary>
    [Required]
    [Range(1, 10)]
    public int AllowedAttempts { get; set; } = 1;
    
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
    [Range(0, 10)]
    public int AllowedScreenSwitchCount { get; set; } = 0;
    
    /// <summary>
    /// 提交后是否可以查看考试结果
    /// </summary>
    public bool EnableViewResult { get; set; } = false;
    
    /// <summary>
    /// 最小考试时间（分钟），低于此时间不允许提交
    /// </summary>
    [Range(1, 1440)]
    public int MinExamTime { get; set; } = 30;
    
    /// <summary>
    /// 是否在结果页显示题目分析
    /// </summary>
    public bool EnableQuestionAnalysis { get; set; } = true;
    
    /// <summary>
    /// 考试状态
    /// </summary>
    [Required]
    public ExamSettingStatus Status { get; set; } = ExamSettingStatus.Draft;
    
    /// <summary>
    /// 参加考试的学生分组
    /// </summary>
    public ICollection<ExamSettingStudentGroup> StudentGroups { get; set; } = [];
    
    /// <summary>
    /// 考试记录
    /// </summary>
    public ICollection<ExamRecord> ExamRecords { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}