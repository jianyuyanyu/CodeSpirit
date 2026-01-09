namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 通知类型
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 全部通知
    /// </summary>
    All = 0,

    /// <summary>
    /// 仅失败时通知
    /// </summary>
    OnFailure = 1,

    /// <summary>
    /// 仅成功时通知
    /// </summary>
    OnSuccess = 2,

    /// <summary>
    /// 不通知
    /// </summary>
    None = 3
}
