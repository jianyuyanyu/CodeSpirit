using System.Threading.Tasks;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 租户感知事件总线接口
/// 提供租户隔离的事件发布和订阅功能
/// </summary>
public interface ITenantAwareEventBus
{
    /// <summary>
    /// 发布租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>发布任务</returns>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent;
    
    /// <summary>
    /// 发布普通事件（非租户感知）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>发布任务</returns>
    Task PublishNonTenantEventAsync<TEvent>(TEvent @event);
    
    /// <summary>
    /// 订阅租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <returns>订阅任务</returns>
    Task SubscribeAsync<TEvent, THandler>() 
        where TEvent : ITenantAwareEvent
        where THandler : ITenantAwareEventHandler<TEvent>;
    
    /// <summary>
    /// 取消订阅租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    void Unsubscribe<TEvent, THandler>()
        where TEvent : ITenantAwareEvent
        where THandler : ITenantAwareEventHandler<TEvent>;
}