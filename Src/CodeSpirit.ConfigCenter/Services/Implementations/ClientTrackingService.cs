using CodeSpirit.ConfigCenter.Models;
using System.Collections.Concurrent;

namespace CodeSpirit.ConfigCenter.Services.Implementations;

/// <summary>
/// 客户端连接跟踪服务实现
/// </summary>
public class ClientTrackingService : IClientTrackingService
{
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly ILogger<ClientTrackingService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ClientTrackingService(ILogger<ClientTrackingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 注册客户端连接
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    /// <param name="clientInfo">客户端信息</param>
    public void RegisterConnection(string connectionId, ClientConnection clientInfo)
    {
        ArgumentNullException.ThrowIfNull(clientInfo);
        
        if (string.IsNullOrEmpty(connectionId))
        {
            throw new ArgumentException("连接ID不能为空", nameof(connectionId));
        }

        if (_connections.TryGetValue(connectionId, out var existingConnection))
        {
            // 合并现有信息，而不是完全覆盖
            clientInfo.ClientId ??= existingConnection.ClientId;
            clientInfo.AppId ??= existingConnection.AppId;
            clientInfo.Environment ??= existingConnection.Environment;
            clientInfo.HostName ??= existingConnection.HostName;
            clientInfo.Version ??= existingConnection.Version;
            // 保留原始连接时间
            clientInfo.ConnectedTime = existingConnection.ConnectedTime;
            clientInfo.SubscribedGroups = existingConnection.SubscribedGroups;
        }

        // 确保连接ID设置正确
        clientInfo.ConnectionId = connectionId;
        var now = DateTime.UtcNow;
        clientInfo.ConnectedTime = now;
        clientInfo.LastActiveTime = now;

        _connections[connectionId] = clientInfo;

        _logger.LogInformation("客户端连接注册: {ConnectionId}, 应用: {AppId}, 环境: {Environment}", 
            connectionId, clientInfo.AppId, clientInfo.Environment);
    }

    /// <summary>
    /// 更新客户端订阅组
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    /// <param name="group">组名</param>
    /// <param name="subscribe">是否订阅（true）或取消订阅（false）</param>
    public void UpdateSubscription(string connectionId, string group, bool subscribe)
    {
        if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(group))
        {
            return;
        }
        
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            if (subscribe)
            {
                if (!connection.SubscribedGroups.Contains(group))
                {
                    connection.SubscribedGroups.Add(group);
                    _logger.LogDebug("客户端 {ConnectionId} 订阅了组: {Group}", connectionId, group);
                }
            }
            else
            {
                if (connection.SubscribedGroups.Contains(group))
                {
                    connection.SubscribedGroups.Remove(group);
                    _logger.LogDebug("客户端 {ConnectionId} 取消订阅了组: {Group}", connectionId, group);
                }
            }
            
            // 更新最后活动时间
            connection.LastActiveTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 更新客户端活动时间
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    public void UpdateLastActivity(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
        {
            return;
        }
        
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.LastActiveTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 移除客户端连接
    /// </summary>
    /// <param name="connectionId">SignalR连接ID</param>
    public void RemoveConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
        {
            return;
        }
        
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger.LogInformation("客户端连接断开: {ConnectionId}, 应用: {AppId}, 环境: {Environment}", 
                connectionId, connection.AppId, connection.Environment);
        }
    }

    /// <summary>
    /// 获取所有在线客户端
    /// </summary>
    /// <returns>客户端连接列表</returns>
    public IEnumerable<ClientConnection> GetAllConnections()
    {
        return _connections.Values.ToList();
    }

    /// <summary>
    /// 获取指定应用的所有在线客户端
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>客户端连接列表</returns>
    public IEnumerable<ClientConnection> GetConnectionsByApp(string appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return Enumerable.Empty<ClientConnection>();
        }
        
        return _connections.Values
            .Where(c => c.AppId == appId)
            .ToList();
    }

    /// <summary>
    /// 获取指定应用和环境的所有在线客户端
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <returns>客户端连接列表</returns>
    public IEnumerable<ClientConnection> GetConnectionsByAppAndEnvironment(string appId, string environment)
    {
        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(environment))
        {
            return Enumerable.Empty<ClientConnection>();
        }
        
        return _connections.Values
            .Where(c => c.AppId == appId && c.Environment == environment)
            .ToList();
    }
    
    /// <summary>
    /// 获取单个客户端连接信息
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>客户端连接信息</returns>
    public ClientConnection GetConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
        {
            return null;
        }
        
        _connections.TryGetValue(connectionId, out var connection);
        return connection;
    }
} 