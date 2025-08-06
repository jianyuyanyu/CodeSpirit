using System;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 租户感知事件接口
/// 所有需要租户隔离的事件都应实现此接口
/// </summary>
public interface ITenantAwareEvent
{
    /// <summary>
    /// 租户ID
    /// </summary>
    string TenantId { get; set; }
    
    /// <summary>
    /// 事件ID（用于去重和追踪）
    /// </summary>
    string EventId { get; set; }
    
    /// <summary>
    /// 事件时间戳
    /// </summary>
    DateTime Timestamp { get; set; }
    
    /// <summary>
    /// 事件来源（发起服务）
    /// </summary>
    string Source { get; set; }
    
    /// <summary>
    /// 事件版本（用于事件演化）
    /// </summary>
    string Version { get; set; }
}