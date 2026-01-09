using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务执行上下文
/// </summary>
internal class TaskExecutionContext
{
    /// <summary>
    /// 执行记录
    /// </summary>
    public TaskExecution Execution { get; set; } = null!;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

    /// <summary>
    /// 超时取消令牌源
    /// </summary>
    public CancellationTokenSource? TimeoutCancellationTokenSource { get; set; }
}
