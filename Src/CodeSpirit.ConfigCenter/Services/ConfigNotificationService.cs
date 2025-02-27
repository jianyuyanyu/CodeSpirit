using CodeSpirit.ConfigCenter.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置通知服务
/// </summary>
public interface IConfigNotificationService
{
    /// <summary>
    /// 发送配置变更通知
    /// </summary>
    Task NotifyConfigChangedAsync(string appId, string environment);
}

/// <summary>
/// 配置通知服务实现
/// </summary>
public class ConfigNotificationService : IConfigNotificationService
{
    private readonly IHubContext<ConfigHub> _hubContext;
    private readonly ILogger<ConfigNotificationService> _logger;

    public ConfigNotificationService(
        IHubContext<ConfigHub> hubContext,
        ILogger<ConfigNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// 发送配置变更通知
    /// </summary>
    public async Task NotifyConfigChangedAsync(string appId, string environment)
    {
        var groupName = GetAppConfigGroupName(appId, environment);
        
        _logger.LogInformation("正在发送应用 {AppId} 在 {Environment} 环境的配置变更通知", 
            appId, environment);
            
        await _hubContext.Clients.Group(groupName).SendAsync("ConfigChanged", new 
        {
            AppId = appId,
            Environment = environment,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取应用配置组名称
    /// </summary>
    private string GetAppConfigGroupName(string appId, string environment)
    {
        return $"config:{appId}:{environment}";
    }
} 