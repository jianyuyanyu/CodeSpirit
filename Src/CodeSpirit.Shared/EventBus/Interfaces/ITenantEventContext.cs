using System;

namespace CodeSpirit.Shared.EventBus.Interfaces;

/// <summary>
/// 租户事件上下文接口
/// 提供事件处理过程中的租户信息和服务
/// </summary>
public interface ITenantEventContext : IDisposable
{
    /// <summary>
    /// 当前租户ID
    /// </summary>
    string TenantId { get; }
    
    /// <summary>
    /// 是否允许跨租户操作
    /// </summary>
    bool AllowCrossTenantAccess { get; }
    
    /// <summary>
    /// 事件处理的用户ID
    /// </summary>
    long? UserId { get; }
    
    /// <summary>
    /// 事件处理的用户名
    /// </summary>
    string? UserName { get; }
    
    /// <summary>
    /// 获取租户专用的服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    T GetTenantService<T>() where T : class;
    
    /// <summary>
    /// 获取租户专用的作用域服务提供者
    /// </summary>
    /// <returns>服务提供者</returns>
    IServiceProvider GetTenantServiceProvider();
    
    /// <summary>
    /// 设置当前用户的额外信息（可选，仅在需要时调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    void SetCurrentUserInfo(long? userId = null, string? userName = null);
}