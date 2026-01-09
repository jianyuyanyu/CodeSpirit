using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 执行历史查询DTO
/// </summary>
public class ExecutionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 执行状态
    /// </summary>
    [DisplayName("执行状态")]
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// 开始时间范围
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTimeFrom { get; set; }

    /// <summary>
    /// 结束时间范围
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? StartTimeTo { get; set; }

    /// <summary>
    /// 执行节点
    /// </summary>
    [DisplayName("执行节点")]
    public string? ExecutionNode { get; set; }

    /// <summary>
    /// 触发类型筛选
    /// </summary>
    [DisplayName("触发类型")]
    public string? TriggerType { get; set; }
}
