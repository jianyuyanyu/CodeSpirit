using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 定时任务查询参数
/// </summary>
public class ScheduledTaskQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务类型筛选
    /// </summary>
    [DisplayName("任务类型")]
    public TaskType? Type { get; set; }

    /// <summary>
    /// 任务状态筛选
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// 任务分组筛选
    /// </summary>
    [DisplayName("任务分组")]
    public string? Group { get; set; }

    /// <summary>
    /// 是否来自配置文件
    /// </summary>
    [DisplayName("配置来源")]
    public bool? IsFromConfiguration { get; set; }

    /// <summary>
    /// 处理器类型筛选
    /// </summary>
    [DisplayName("处理器类型")]
    public string? HandlerType { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? IsEnabled { get; set; }
}
