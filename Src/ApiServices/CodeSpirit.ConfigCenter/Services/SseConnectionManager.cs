using System.Collections.Concurrent;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// SSE 连接管理器 - 维护所有客户端连接并管理健康状态
/// </summary>
public class SseConnectionManager : ISingletonDependency
{
    private readonly ConcurrentDictionary<string, List<SseConnection>> _connections = new();
    private readonly ILogger<SseConnectionManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    // 健康状态缓存键前缀
    private const string HealthStatusCacheKeyPrefix = "configcenter:health:";
    
    // 健康状态缓存选项：缓存2分钟，使用分布式缓存
    private static readonly CacheOptions HealthStatusCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        Level = CacheLevel.L2Only // 使用分布式缓存，多实例共享
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    public SseConnectionManager(
        IServiceProvider serviceProvider,
        ILogger<SseConnectionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 注册客户端连接
    /// </summary>
    public async Task AddConnectionAsync(string appId, HttpResponse response)
    {
        var connection = new SseConnection(appId, response);
        _connections.AddOrUpdate(
            appId,
            new List<SseConnection> { connection },
            (key, list) =>
            {
                lock (list)
                {
                    list.Add(connection);
                }
                return list;
            });

        var connectionCount = GetConnectionCount(appId);
        _logger.LogInformation("SSE连接已注册: AppId={AppId}, 当前连接数={Count}", appId, connectionCount);

        // 更新健康状态：有连接 = 服务健康
        await UpdateHealthStatusAsync(appId, isHealthy: true);
    }

    /// <summary>
    /// 移除断开的连接
    /// </summary>
    public async Task RemoveConnectionAsync(string appId, SseConnection connection)
    {
        bool isLastConnection = false;
        
        if (_connections.TryGetValue(appId, out var list))
        {
            lock (list)
            {
                list.Remove(connection);
                if (list.Count == 0)
                {
                    _connections.TryRemove(appId, out _);
                    isLastConnection = true;
                }
            }
            
            var remainingCount = GetConnectionCount(appId);
            _logger.LogInformation("SSE连接已移除: AppId={AppId}, 剩余连接数={Count}", appId, remainingCount);
            
            // 如果没有连接了，标记为不健康
            if (isLastConnection)
            {
                await UpdateHealthStatusAsync(appId, isHealthy: false);
            }
        }
    }

    /// <summary>
    /// 向指定应用的所有客户端推送事件
    /// </summary>
    public async Task NotifyConfigChangedAsync(string appId, long version)
    {
        if (!_connections.TryGetValue(appId, out var list))
        {
            _logger.LogDebug("应用 {AppId} 没有活跃的SSE连接", appId);
            return;
        }

        var message = $"data: {{\"type\":\"ConfigChanged\",\"appId\":\"{appId}\",\"version\":{version}}}\n\n";
        var tasks = new List<Task>();
        
        lock (list)
        {
            foreach (var conn in list.ToList()) // 创建副本避免并发修改
            {
                tasks.Add(SendMessageSafely(conn, message));
            }
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("已向应用 {AppId} 的 {Count} 个客户端推送配置变更通知", appId, tasks.Count);
    }

    /// <summary>
    /// 安全发送消息（捕获异常）
    /// </summary>
    private Task SendMessageSafely(SseConnection connection, string message)
    {
        return SendMessageSafelyInternal(connection, message);
    }

    /// <summary>
    /// 安全发送消息内部实现
    /// </summary>
    private async Task SendMessageSafelyInternal(SseConnection connection, string message)
    {
        try
        {
            await connection.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送SSE消息失败，连接将被移除: AppId={AppId}", connection.AppId);
            await RemoveConnectionAsync(connection.AppId, connection);
        }
    }

    /// <summary>
    /// 获取指定应用的连接数
    /// </summary>
    public int GetConnectionCount(string appId)
    {
        if (_connections.TryGetValue(appId, out var list))
        {
            lock (list)
            {
                return list.Count;
            }
        }
        return 0;
    }

    /// <summary>
    /// 获取所有活跃连接数
    /// </summary>
    public int GetTotalConnectionCount()
    {
        return _connections.Values.Sum(list =>
        {
            lock (list)
            {
                return list.Count;
            }
        });
    }

    /// <summary>
    /// 更新服务健康状态
    /// </summary>
    private async Task UpdateHealthStatusAsync(string appId, bool isHealthy)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            
            if (cacheService == null)
            {
                _logger.LogDebug("缓存服务未配置，跳过健康状态更新");
                return;
            }

            var cacheKey = GetHealthStatusCacheKey(appId);
            await cacheService.SetAsync(cacheKey, isHealthy, HealthStatusCacheOptions);
            
            _logger.LogDebug("已更新服务 {AppId} 健康状态: {IsHealthy}", appId, isHealthy ? "健康" : "不健康");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新服务 {AppId} 健康状态失败", appId);
        }
    }

    /// <summary>
    /// 获取健康状态缓存键
    /// </summary>
    private static string GetHealthStatusCacheKey(string appId)
    {
        return $"{HealthStatusCacheKeyPrefix}{appId}";
    }

    /// <summary>
    /// 获取服务健康状态（供外部调用）
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>健康状态，null表示未知</returns>
    public async Task<bool?> GetHealthStatusAsync(string appId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            
            if (cacheService == null)
            {
                return null;
            }

            var cacheKey = GetHealthStatusCacheKey(appId);
            var cached = await cacheService.GetAsync<bool?>(cacheKey);
            
            // 如果缓存中没有，但有活跃连接，则认为是健康的
            if (cached == null && GetConnectionCount(appId) > 0)
            {
                await UpdateHealthStatusAsync(appId, isHealthy: true);
                return true;
            }
            
            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取服务 {AppId} 健康状态失败", appId);
            return null;
        }
    }

    /// <summary>
    /// 获取健康状态缓存键（静态方法，供外部调用）
    /// </summary>
    public static string GetHealthStatusCacheKeyForService(string appId)
    {
        return $"{HealthStatusCacheKeyPrefix}{appId}";
    }
}

/// <summary>
/// SSE 连接封装
/// </summary>
public class SseConnection
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; }

    /// <summary>
    /// HTTP响应对象
    /// </summary>
    public HttpResponse Response { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SseConnection(string appId, HttpResponse response)
    {
        AppId = appId;
        Response = response;
    }

    /// <summary>
    /// 发送SSE消息
    /// </summary>
    public async Task SendAsync(string message)
    {
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }
}


