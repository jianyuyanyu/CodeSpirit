using CodeSpirit.Core.Dtos;
using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 任务查询DTO
/// </summary>
public class TaskQueryDto : QueryDtoBase
{
    /// <summary>
    /// 目标ID
    /// </summary>
    [DisplayName("目标ID")]
    public Guid? GoalId { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus? Status { get; set; }
    
    /// <summary>
    /// 最小优先级
    /// </summary>
    [DisplayName("最小优先级")]
    public int? MinPriority { get; set; }
    
    /// <summary>
    /// 最大优先级
    /// </summary>
    [DisplayName("最大优先级")]
    public int? MaxPriority { get; set; }
    
    /// <summary>
    /// 开始日期范围（起）
    /// </summary>
    [DisplayName("开始日期起")]
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// 开始日期范围（止）
    /// </summary>
    [DisplayName("开始日期止")]
    public DateTime? EndDate { get; set; }
}

