using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.Notifications.Events;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.Notifications;

/// <summary>
/// 通知服务实现
/// </summary>
public class SessionNotificationService : ISessionNotificationService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<SessionNotificationService> _logger;

    public SessionNotificationService(
        IEventBus eventBus,
        ILogger<SessionNotificationService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendNotificationAsync(SessionNotificationEvent @event)
    {
        try
        {
            await _eventBus.PublishAsync(@event);
            
            _logger.LogInformation(
                "已发布通知事件 {EventId}, 主题: {Topic}, 类型: {Type}", 
                @event.Id, @event.Topic, @event.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布通知事件失败");
            throw;
        }
    }
} 