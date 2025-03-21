using System.Threading.Tasks;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 事件总线接口
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件</param>
    /// <returns>任务</returns>
    Task PublishAsync<TEvent>(TEvent @event);
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    Task Subscribe<TEvent, THandler>() 
        where THandler : IEventHandler<TEvent>;
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    void Unsubscribe<TEvent, THandler>()
        where THandler : IEventHandler<TEvent>;
} 