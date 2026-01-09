using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 定时任务列表数据传输对象
/// </summary>
public class ScheduledTaskDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    [TplColumn(template: "<strong>${name}</strong>")]
    [Badge(Animation = true, Level = "success", Mode = "text", Text = "${executionCount}", VisibleOn = "executionCount > 0")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务分组
    /// </summary>
    [DisplayName("任务分组")]
    [TagsColumn]
    public string? Group { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [DisplayName("任务类型")]
    public TaskType Type { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Cron表达式
    /// </summary>
    [DisplayName("Cron表达式")]
    [TplColumn("<code>${cronExpression}</code>")]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Cron描述
    /// </summary>
    [DisplayName("执行描述")]
    public string? CronDescription { get; set; }

        /// <summary>
    /// 任务描述
    /// </summary>
    [DisplayName("执行描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [DisplayName("下次执行")]
    [DateColumn(FromNow = true)]
    public DateTime? NextExecuteTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [DisplayName("上次执行")]
    [DateColumn(FromNow = true)]
    public DateTime? LastExecuteTime { get; set; }

    /// <summary>
    /// 执行次数
    /// </summary>
    [DisplayName("执行次数")]
    [Badge(VisibleOn = "executionCount > 0", Level = "info", Mode = "text", Text = "${executionCount}")]
    public int ExecutionCount { get; set; }

    /// <summary>
    /// 处理器类型
    /// </summary>
    [DisplayName("处理器")]
    [LongTextColumn(30)]
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [DisplayName("重试次数")]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [DisplayName("优先级")]
    [TplColumn("<span class='text-primary'>${priority}</span>")]
    public int Priority { get; set; }

    /// <summary>
    /// 目标服务名称
    /// </summary>
    [DisplayName("目标服务")]
    public string? TargetService { get; set; }

    /// <summary>
    /// 是否来自配置文件
    /// </summary>
    [DisplayName("来自配置文件")]
    [AmisColumn(Type = "switch")]
    public bool IsFromConfiguration { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm")]
    public DateTime CreatedAt { get; set; }
}
