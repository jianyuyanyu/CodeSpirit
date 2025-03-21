using System.Threading.Tasks;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 事件处理器接口
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventHandler<in TEvent>
{
    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>任务</returns>
    Task HandleAsync(TEvent @event);
} 