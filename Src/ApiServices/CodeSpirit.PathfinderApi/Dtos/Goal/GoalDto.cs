using CodeSpirit.Core.Attributes;
using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos.Goal;

/// <summary>
/// 目标数据传输对象
/// </summary>
public class GoalDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("目标ID")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public long UserId { get; set; }
    
    /// <summary>
    /// 目标标题
    /// </summary>
    [DisplayName("目标标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标描述
    /// </summary>
    [DisplayName("目标描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 目标类型
    /// </summary>
    [DisplayName("目标类型")]
    public string? Category { get; set; }
    
    /// <summary>
    /// 目标日期
    /// </summary>
    [DisplayName("目标日期")]
    public DateTime? TargetDate { get; set; }
    
    /// <summary>
    /// 目标状态
    /// </summary>
    [DisplayName("目标状态")]
    public GoalStatus Status { get; set; }
    
    /// <summary>
    /// 目标进度
    /// </summary>
    [DisplayName("目标进度")]
    public int Progress { get; set; }
    
    /// <summary>
    /// 可行性评分（综合评分）
    /// </summary>
    [DisplayName("可行性评分")]
    public int? FeasibilityScore { get; set; }
    
    /// <summary>
    /// 明确性评分
    /// </summary>
    [DisplayName("明确性评分")]
    public int? ClarityScore { get; set; }
    
    /// <summary>
    /// 可执行性评分
    /// </summary>
    [DisplayName("可执行性评分")]
    public int? ExecutabilityScore { get; set; }
    
    /// <summary>
    /// 完整性评分
    /// </summary>
    [DisplayName("完整性评分")]
    public int? CompletenessScore { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}

