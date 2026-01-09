using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 任务执行记录DTO
/// </summary>
public class TaskExecutionDto
{
    /// <summary>
    /// 执行ID
    /// </summary>
    [DisplayName("执行ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    [TplColumn("<strong>${taskName}</strong>")]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 执行状态
    /// </summary>
    [DisplayName("执行状态")]
    public TaskStatus Status { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    [DisplayName("触发类型")]
    [TagsColumn]
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm:ss")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm:ss")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 执行时长
    /// </summary>
    [DisplayName("执行时长")]
    [TplColumn("${duration != null ? duration : '-'}")]
    public string? DurationDisplay { get; set; }

    /// <summary>
    /// 执行结果
    /// </summary>
    [DisplayName("执行结果")]
    [LongTextColumn(50)]
    public string? Result { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    [LongTextColumn(50)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行节点
    /// </summary>
    [DisplayName("执行节点")]
    public string? ExecutionNode { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [DisplayName("重试次数")]
    public int RetryCount { get; set; }
}
