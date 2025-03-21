using System.Threading.Tasks;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 事件发布者接口
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">要发布的事件</param>
    Task PublishAsync<TEvent>(TEvent @event);
} 