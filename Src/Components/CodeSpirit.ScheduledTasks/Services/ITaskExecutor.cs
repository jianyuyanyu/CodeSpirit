using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务执行器接口
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="triggerType">触发类型，默认为 "Scheduled"</param>
    /// <returns>执行记录</returns>
    Task<TaskExecution> ExecuteAsync(ScheduledTask task, CancellationToken cancellationToken = default, string triggerType = "Scheduled");

    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否取消成功</returns>
    Task<bool> CancelAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取正在执行的任务
    /// </summary>
    /// <returns>执行中的任务列表</returns>
    Task<List<TaskExecution>> GetRunningExecutionsAsync();

    /// <summary>
    /// 检查任务是否正在执行
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>是否正在执行</returns>
    Task<bool> IsTaskRunningAsync(string taskId);
}
