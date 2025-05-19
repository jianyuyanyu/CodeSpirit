using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 练习设置实体
/// </summary>
public class PracticeSetting : LongKeyAuditableEntityBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    public long ExamPaperId { get; set; }
    
    /// <summary>
    /// 试卷
    /// </summary>
    public ExamPaper ExamPaper { get; set; } = null!;
    
    /// <summary>
    /// 练习模式
    /// </summary>
    public PracticeMode PracticeMode { get; set; }
    
    /// <summary>
    /// 练习次数限制(0表示不限制)
    /// </summary>
    public int MaxAttempts { get; set; }
    
    /// <summary>
    /// 时长限制(分钟, 0表示不限制)
    /// </summary>
    public int TimeLimit { get; set; }
    
    /// <summary>
    /// 是否显示答案解析
    /// </summary>
    public bool ShowAnalysis { get; set; }
    
    /// <summary>
    /// 是否随机排序题目
    /// </summary>
    public bool RandomizeQuestions { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    public PracticeSettingStatus Status { get; set; } = PracticeSettingStatus.Draft;
} 