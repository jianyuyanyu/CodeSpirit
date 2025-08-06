using System;
using System.Reflection;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.Shared.EventBus.Events;

/// <summary>
/// 租户感知事件基类
/// 提供租户ID的自动设置和验证
/// </summary>
public abstract class TenantAwareEventBase : ITenantAwareEvent
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 事件ID（用于去重和追踪）
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 事件来源（发起服务）
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// 事件版本（用于事件演化）
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// 构造函数 - 自动设置基础信息
    /// </summary>
    protected TenantAwareEventBase()
    {
        // 构造时自动设置源信息
        Source = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
    }
    
    /// <summary>
    /// 验证事件数据的完整性
    /// </summary>
    /// <returns>验证是否通过</returns>
    public virtual bool IsValid()
    {
        return !string.IsNullOrEmpty(TenantId) 
               && !string.IsNullOrEmpty(EventId)
               && Timestamp != default
               && !string.IsNullOrEmpty(Source);
    }
}