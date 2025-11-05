using CodeSpirit.PathfinderApi.Models.Enums;

namespace CodeSpirit.PathfinderApi.Models;

/// <summary>
/// 任务实体
/// </summary>
public class PathfinderTask : AuditableEntityBase<Guid>
{
    
    /// <summary>
    /// 目标ID
    /// </summary>
    [Required]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 父任务ID（子任务支持）
    /// </summary>
    public Guid? ParentTaskId { get; set; }
    
    /// <summary>
    /// 任务标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Display(Name = "任务标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 任务描述
    /// </summary>
    [MaxLength(2000)]
    [Display(Name = "任务描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 任务顺序
    /// </summary>
    [Display(Name = "顺序")]
    public int Order { get; set; }
    
    /// <summary>
    /// 优先级（1-5，数字越大优先级越高）
    /// </summary>
    [Display(Name = "优先级")]
    public int Priority { get; set; } = 3;
    
    /// <summary>
    /// 预计耗时（分钟）
    /// </summary>
    [Display(Name = "预计耗时")]
    public int? EstimatedDuration { get; set; }
    
    /// <summary>
    /// 实际耗时（分钟）
    /// </summary>
    [Display(Name = "实际耗时")]
    public int? ActualDuration { get; set; }
    
    /// <summary>
    /// 建议开始日期
    /// </summary>
    [Display(Name = "建议开始日期")]
    public DateTime? SuggestedStartDate { get; set; }
    
    /// <summary>
    /// 建议结束日期
    /// </summary>
    [Display(Name = "建议结束日期")]
    public DateTime? SuggestedEndDate { get; set; }
    
    /// <summary>
    /// 实际开始日期
    /// </summary>
    [Display(Name = "实际开始日期")]
    public DateTime? ActualStartDate { get; set; }
    
    /// <summary>
    /// 实际结束日期
    /// </summary>
    [Display(Name = "实际结束日期")]
    public DateTime? ActualEndDate { get; set; }
    
    /// <summary>
    /// 预计开始时间
    /// </summary>
    [Display(Name = "预计开始时间")]
    public DateTime? EstimatedStartTime { get; set; }
    
    /// <summary>
    /// 预计完成时间
    /// </summary>
    [Display(Name = "预计完成时间")]
    public DateTime? EstimatedEndTime { get; set; }
    
    /// <summary>
    /// 实际开始时间
    /// </summary>
    [Display(Name = "实际开始时间")]
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// 实际完成时间
    /// </summary>
    [Display(Name = "实际完成时间")]
    public DateTime? ActualEndTime { get; set; }
    
    /// <summary>
    /// 截止日期
    /// </summary>
    [Display(Name = "截止日期")]
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// 依赖任务ID（逗号分隔的Guid）
    /// </summary>
    [MaxLength(1000)]
    [Display(Name = "依赖任务")]
    public string? DependsOn { get; set; }
    
    /// <summary>
    /// 任务结果（JSON格式）
    /// </summary>
    [Display(Name = "任务结果")]
    public string? Result { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [MaxLength(2000)]
    [Display(Name = "错误信息")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    [Required]
    [Display(Name = "状态")]
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    
    /// <summary>
    /// 是否可自动化执行
    /// </summary>
    [Display(Name = "可自动化")]
    public bool IsAutomatable { get; set; }
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// 导航属性：关联的目标
    /// </summary>
    public virtual Goal? Goal { get; set; }
    
    /// <summary>
    /// 导航属性：父任务
    /// </summary>
    public virtual PathfinderTask? ParentTask { get; set; }
    
    /// <summary>
    /// 导航属性：子任务列表
    /// </summary>
    public virtual ICollection<PathfinderTask>? SubTasks { get; set; }
}

