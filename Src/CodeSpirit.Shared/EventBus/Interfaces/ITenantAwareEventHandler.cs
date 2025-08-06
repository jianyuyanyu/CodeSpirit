using System.Threading.Tasks;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 租户感知事件处理器接口
/// 提供租户上下文和安全验证
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface ITenantAwareEventHandler<in TEvent> : IEventHandler<TEvent>
    where TEvent : ITenantAwareEvent
{
    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    Task<bool> CanHandleEventAsync(TEvent @event);
    
    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    Task HandleWithTenantContextAsync(TEvent @event, ITenantEventContext tenantContext);
}