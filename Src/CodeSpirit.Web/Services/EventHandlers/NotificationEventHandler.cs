using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.Notifications;
using CodeSpirit.Shared.Notifications.Events;
using CodeSpirit.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace CodeSpirit.Web.Services.EventHandlers;

/// <summary>
/// 通知事件处理器
/// </summary>
public class NotificationEventHandler : IEventHandler<SessionNotificationEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationEventHandler> _logger;

    public NotificationEventHandler(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleAsync(SessionNotificationEvent @event)
    {
        try
        {
            var groupId = @event.SessionId != null ? 
                $"{@event.Topic}:{@event.SessionId}" : 
                @event.Topic;
                
            await _hubContext.Clients.Group(groupId)
                .SendAsync("ReceiveNotification", @event);
                
            _logger.LogInformation(
                "已处理通知事件 {EventId}, 发送到组 {GroupId}, 类型: {Type}", 
                @event.Id, groupId, @event.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理通知事件失败");
            throw;
        }
    }
} 