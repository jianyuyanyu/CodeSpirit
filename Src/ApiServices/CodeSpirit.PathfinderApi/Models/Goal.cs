using CodeSpirit.PathfinderApi.Models.Enums;

namespace CodeSpirit.PathfinderApi.Models;

/// <summary>
/// 目标实体
/// </summary>
public class Goal : AuditableEntityBase<Guid>
{
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }
    
    /// <summary>
    /// 目标标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Display(Name = "目标标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标描述
    /// </summary>
    [MaxLength(2000)]
    [Display(Name = "目标描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 目标类型
    /// </summary>
    [MaxLength(50)]
    [Display(Name = "目标类型")]
    public string? Category { get; set; }
    
    /// <summary>
    /// 目标完成日期
    /// </summary>
    [Display(Name = "目标日期")]
    public DateTime? TargetDate { get; set; }
    
    /// <summary>
    /// 目标状态
    /// </summary>
    [Required]
    [Display(Name = "状态")]
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    
    /// <summary>
    /// 目标进度（0-100）
    /// </summary>
    [Display(Name = "进度")]
    public int Progress { get; set; }
    
    /// <summary>
    /// 可行性评分（0-10）- 综合评分
    /// </summary>
    [Display(Name = "可行性评分")]
    public int? FeasibilityScore { get; set; }
    
    /// <summary>
    /// 明确性评分（0-10）
    /// </summary>
    [Display(Name = "明确性评分")]
    public int? ClarityScore { get; set; }
    
    /// <summary>
    /// 可执行性评分（0-10）
    /// </summary>
    [Display(Name = "可执行性评分")]
    public int? ExecutabilityScore { get; set; }
    
    /// <summary>
    /// 完整性评分（0-10）
    /// </summary>
    [Display(Name = "完整性评分")]
    public int? CompletenessScore { get; set; }
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// 导航属性：关联的任务列表
    /// </summary>
    public virtual ICollection<PathfinderTask>? Tasks { get; set; }
}

