using System.ComponentModel;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 任务统计信息
/// </summary>
public class TaskStatistics
{
    /// <summary>
    /// 总任务数
    /// </summary>
    [DisplayName("总任务数")]
    public int TotalTasks { get; set; }

    /// <summary>
    /// 启用任务数
    /// </summary>
    [DisplayName("启用任务数")]
    public int EnabledTasks { get; set; }

    /// <summary>
    /// 禁用任务数
    /// </summary>
    [DisplayName("禁用任务数")]
    public int DisabledTasks { get; set; }

    /// <summary>
    /// 正在执行任务数
    /// </summary>
    [DisplayName("正在执行任务数")]
    public int RunningTasks { get; set; }

    /// <summary>
    /// 今日执行次数
    /// </summary>
    [DisplayName("今日执行次数")]
    public int TodayExecutions { get; set; }

    /// <summary>
    /// 今日成功次数
    /// </summary>
    [DisplayName("今日成功次数")]
    public int TodaySuccessExecutions { get; set; }

    /// <summary>
    /// 今日失败次数
    /// </summary>
    [DisplayName("今日失败次数")]
    public int TodayFailedExecutions { get; set; }

    /// <summary>
    /// 成功率
    /// </summary>
    [DisplayName("成功率")]
    public double SuccessRate { get; set; }

    /// <summary>
    /// 按状态分组统计
    /// </summary>
    [DisplayName("状态统计")]
    public Dictionary<TaskStatus, int> StatusStatistics { get; set; } = new();

    /// <summary>
    /// 按类型分组统计
    /// </summary>
    [DisplayName("类型统计")]
    public Dictionary<TaskType, int> TypeStatistics { get; set; } = new();
}
