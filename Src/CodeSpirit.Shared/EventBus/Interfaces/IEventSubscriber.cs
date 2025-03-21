namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 事件订阅者接口
/// </summary>
public interface IEventSubscriber
{
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