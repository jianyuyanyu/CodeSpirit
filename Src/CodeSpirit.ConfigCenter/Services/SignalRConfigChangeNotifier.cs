using Microsoft.AspNetCore.SignalR;
using CodeSpirit.ConfigCenter.Hubs;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// SignalR 配置变更通知器实现
/// </summary>
public class SignalRConfigChangeNotifier : IConfigChangeNotifier
{
    private readonly IHubContext<ConfigHub> _hubContext;
    private readonly ILogger<SignalRConfigChangeNotifier> _logger;
    private readonly Dictionary<string, Func<Task>> _callbacks;

    public SignalRConfigChangeNotifier(
        IHubContext<ConfigHub> hubContext,
        ILogger<SignalRConfigChangeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _callbacks = new Dictionary<string, Func<Task>>();
    }

    public async Task NotifyConfigChangedAsync(string appId, string environment)
    {
        var groupName = GetAppConfigGroupName(appId, environment);
        await _hubContext.Clients.Group(groupName).SendAsync("ConfigChanged", new 
        {
            AppId = appId,
            Environment = environment,
            Timestamp = DateTime.UtcNow
        });
        
        if (_callbacks.TryGetValue(groupName, out var callback))
        {
            await callback();
        }
        
        _logger.LogInformation("Published config change: {AppId}/{Environment}", appId, environment);
    }

    public Task SubscribeAsync(string appId, string environment, Func<Task> callback)
    {
        var groupName = GetAppConfigGroupName(appId, environment);
        _callbacks[groupName] = callback;
        _logger.LogInformation("Subscribed to config changes: {AppId}/{Environment}", appId, environment);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string appId, string environment)
    {
        var groupName = GetAppConfigGroupName(appId, environment);
        _callbacks.Remove(groupName);
        _logger.LogInformation("Unsubscribed from config changes: {AppId}/{Environment}", appId, environment);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 获取应用配置组名称，保持与ConfigHub一致
    /// </summary>
    private string GetAppConfigGroupName(string appId, string environment)
    {
        return $"config:{appId}:{environment}";
    }
} 