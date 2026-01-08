using CodeSpirit.ConfigCenter.Events;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置通知服务
/// </summary>
public interface IConfigNotificationService : IScopedDependency
{
    /// <summary>
    /// 发送配置变更通知
    /// </summary>
    Task NotifyConfigChangedAsync(string appId, long version);
}

/// <summary>
/// 配置通知服务实现 - 使用事件总线实现分布式通知
/// </summary>
public class ConfigNotificationService : IConfigNotificationService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConfigNotificationService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigNotificationService(
        IEventBus eventBus,
        ILogger<ConfigNotificationService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// 发送配置变更通知
    /// </summary>
    /// <remarks>
    /// 通过事件总线发布事件，所有配置中心实例都会收到通知并推送给各自的SSE客户端
    /// </remarks>
    public async Task NotifyConfigChangedAsync(string appId, long version)
    {
        var @event = new ConfigChangedEvent
        {
            AppId = appId,
            Version = version,
            Timestamp = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(@event);
        _logger.LogInformation("已发布配置变更事件: AppId={AppId}, Version={Version}", appId, version);
    }
} 