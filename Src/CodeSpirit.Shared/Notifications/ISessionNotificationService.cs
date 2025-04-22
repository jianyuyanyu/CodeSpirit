using CodeSpirit.Shared.Notifications.Events;

namespace CodeSpirit.Shared.Notifications;

/// <summary>
/// 通知服务接口
/// </summary>
public interface ISessionNotificationService
{
    /// <summary>
    /// 发送通知
    /// </summary>
    /// <param name="message">通知消息</param>
    Task SendNotificationAsync(SessionNotificationEvent message);
} 