using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 任务查询DTO
/// </summary>
public class TaskQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [DisplayName("任务类型")]
    public TaskType? Type { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [DisplayName("任务分组")]
    public string? Group { get; set; }

    /// <summary>
    /// 是否来自配置文件
    /// </summary>
    [DisplayName("来自配置文件")]
    public bool? IsFromConfiguration { get; set; }

    /// <summary>
    /// 处理器类型筛选
    /// </summary>
    [DisplayName("处理器类型")]
    public string? HandlerType { get; set; }
}
