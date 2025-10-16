namespace CodeSpirit.ScheduledTasks.Models;

/// <summary>
/// 定时任务状态枚举
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 已禁用
    /// </summary>
    [Display(Name = "已禁用")]
    Disabled = 0,

    /// <summary>
    /// 已启用
    /// </summary>
    [Display(Name = "已启用")]
    Enabled = 1,

    /// <summary>
    /// 正在执行
    /// </summary>
    [Display(Name = "正在执行")]
    Running = 2,

    /// <summary>
    /// 执行完成
    /// </summary>
    [Display(Name = "执行完成")]
    Completed = 3,

    /// <summary>
    /// 执行失败
    /// </summary>
    [Display(Name = "执行失败")]
    Failed = 4,

    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 5,

    /// <summary>
    /// 超时
    /// </summary>
    [Display(Name = "超时")]
    Timeout = 6,

    /// <summary>
    /// 已跳过
    /// </summary>
    [Display(Name = "已跳过")]
    Skipped = 7
}

/// <summary>
/// 任务类型枚举
/// </summary>
public enum TaskType
{
    /// <summary>
    /// Cron定时任务
    /// </summary>
    [Display(Name = "Cron定时任务")]
    Cron = 1,

    /// <summary>
    /// 延迟任务
    /// </summary>
    [Display(Name = "延迟任务")]
    Delay = 2,

    /// <summary>
    /// 一次性任务
    /// </summary>
    [Display(Name = "一次性任务")]
    OneTime = 3
}

/// <summary>
/// 任务执行策略枚举
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>
    /// 单实例执行
    /// </summary>
    [Display(Name = "单实例执行")]
    SingleInstance = 1,

    /// <summary>
    /// 分布式执行
    /// </summary>
    [Display(Name = "分布式执行")]
    Distributed = 2
}
