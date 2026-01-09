using CodeSpirit.ScheduledTasks.Dto;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务执行通知器接口
/// </summary>
public interface ITaskExecutionNotifier
{
    /// <summary>
    /// 发送任务执行完成通知
    /// </summary>
    /// <param name="notification">通知信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> NotifyAsync(TaskExecutionNotification notification, CancellationToken cancellationToken = default);
}
