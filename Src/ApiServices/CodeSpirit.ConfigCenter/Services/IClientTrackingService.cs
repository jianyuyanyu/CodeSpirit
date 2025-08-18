using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 客户端连接跟踪服务接口
/// </summary>
public interface IClientTrackingService : ISingletonDependency
{
    /// <summary>
    /// 注册客户端连接
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    /// <param name="clientInfo">客户端信息</param>
    void RegisterConnection(string connectionId, ClientConnection clientInfo);
    
    /// <summary>
    /// 更新客户端订阅组
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    /// <param name="group">组名</param>
    /// <param name="subscribe">是否订阅（true）或取消订阅（false）</param>
    void UpdateSubscription(string connectionId, string group, bool subscribe);
    
    /// <summary>
    /// 更新客户端活动时间
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    void UpdateLastActivity(string connectionId);
    
    /// <summary>
    /// 移除客户端连接
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    void RemoveConnection(string connectionId);
    
    /// <summary>
    /// 获取所有在线客户端
    /// </summary>
    /// <returns>客户端连接列表</returns>
    IEnumerable<ClientConnection> GetAllConnections();
    
    /// <summary>
    /// 获取指定应用的所有在线客户端
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>客户端连接列表</returns>
    IEnumerable<ClientConnection> GetConnectionsByApp(string appId);
    
    /// <summary>
    /// 获取指定应用和环境的所有在线客户端
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>客户端连接列表</returns>
    IEnumerable<ClientConnection> GetConnectionsByAppAndEnvironment(string appId, string environment);
    
    /// <summary>
    /// 获取单个客户端连接信息
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>客户端连接信息</returns>
    ClientConnection GetConnection(string connectionId);
} 