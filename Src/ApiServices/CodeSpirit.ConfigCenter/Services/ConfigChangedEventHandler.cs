using CodeSpirit.ConfigCenter.Events;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置变更事件处理器 - 订阅事件并推送给本地SSE连接
/// </summary>
public class ConfigChangedEventHandler : IEventHandler<ConfigChangedEvent>, IScopedDependency
{
    private readonly SseConnectionManager _sseConnectionManager;
    private readonly ILogger<ConfigChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigChangedEventHandler(
        SseConnectionManager sseConnectionManager,
        ILogger<ConfigChangedEventHandler> logger)
    {
        _sseConnectionManager = sseConnectionManager;
        _logger = logger;
    }

    /// <summary>
    /// 处理配置变更事件
    /// </summary>
    /// <remarks>
    /// 每个配置中心实例都会收到此事件，然后推送给自己的SSE客户端
    /// </remarks>
    public async Task HandleAsync(ConfigChangedEvent @event)
    {
        try
        {
            _logger.LogInformation(
                "收到配置变更事件: AppId={AppId}, Version={Version}, 开始推送给本地SSE客户端",
                @event.AppId, @event.Version);

            // 推送给本实例的所有SSE客户端
            await _sseConnectionManager.NotifyConfigChangedAsync(@event.AppId, @event.Version);

            _logger.LogInformation(
                "配置变更事件处理完成: AppId={AppId}, Version={Version}",
                @event.AppId, @event.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理配置变更事件失败: AppId={AppId}, Version={Version}",
                @event.AppId, @event.Version);
            // 不抛出异常，避免影响事件总线
        }
    }
}

