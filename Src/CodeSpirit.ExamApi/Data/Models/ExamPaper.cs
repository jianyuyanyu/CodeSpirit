using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 试卷实体
/// </summary>
public class ExamPaper: LongKeyAuditableEntityBase, IMultiTenant
{    
    /// <summary>
    /// 试卷名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷类型
    /// </summary>
    [Required]
    public ExamPaperType Type { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [Required]
    [Range(0, 1000)]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [Range(0, 1000)]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [Range(1, 1440)]
    public int Duration { get; set; }
    
    /// <summary>
    /// 随机试卷规则，JSON格式（如题型分布、难度分布、知识点覆盖等）
    /// </summary>
    [StringLength(2000)]
    public string? RandomRules { get; set; }
    
    /// <summary>
    /// 试卷包含的题目
    /// </summary>
    public ICollection<ExamPaperQuestion> ExamPaperQuestions { get; set; } = [];
    
    /// <summary>
    /// 试卷难度系数（0-100）
    /// </summary>
    [Range(0, 100)]
    public int DifficultyLevel { get; set; }
    
    /// <summary>
    /// 试卷版本
    /// </summary>
    [Required]
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;
    
    /// <summary>
    /// 平均分
    /// </summary>
    [Range(0, 1000)]
    public decimal AverageScore { get; set; } = 0;
    
    /// <summary>
    /// 通过率（百分比）
    /// </summary>
    [Range(0, 100)]
    public decimal PassRate { get; set; } = 0;
    
    /// <summary>
    /// 试卷状态
    /// </summary>
    public ExamPaperStatus Status { get; set; }
    
    /// <summary>
    /// 是否已完成预览检查
    /// </summary>
    public bool IsPreviewChecked { get; set; } = false;
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}