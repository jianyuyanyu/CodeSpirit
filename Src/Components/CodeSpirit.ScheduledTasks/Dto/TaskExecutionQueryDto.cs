using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 任务执行记录查询参数
/// </summary>
public class TaskExecutionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务ID筛选
    /// </summary>
    [DisplayName("任务ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 执行状态筛选
    /// </summary>
    [DisplayName("执行状态")]
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// 触发类型筛选
    /// </summary>
    [DisplayName("触发类型")]
    public string? TriggerType { get; set; }

    /// <summary>
    /// 开始时间范围
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime[]? StartTime { get; set; }

    /// <summary>
    /// 执行节点筛选
    /// </summary>
    [DisplayName("执行节点")]
    public string? ExecutionNode { get; set; }
}
