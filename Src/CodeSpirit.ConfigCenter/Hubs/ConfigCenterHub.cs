using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Hubs;

/// <summary>
/// 配置中心Hub，用于实时配置更新通知
/// </summary>
public class ConfigCenterHub : Hub
{
    private readonly ILogger<ConfigCenterHub> _logger;

    public ConfigCenterHub(ILogger<ConfigCenterHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 注册应用配置监听
    /// </summary>
    public async Task RegisterAppConfigListener(string appId, string environment)
    {
        var groupName = GetConfigGroupName(appId, environment);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("客户端 {ConnectionId} 注册了应用 {AppId} 在 {Environment} 环境的配置监听",
            Context.ConnectionId, appId, environment);
    }

    /// <summary>
    /// 取消注册应用配置监听
    /// </summary>
    public async Task UnregisterAppConfigListener(string appId, string environment)
    {
        var groupName = GetConfigGroupName(appId, environment);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("客户端 {ConnectionId} 取消了应用 {AppId} 在 {Environment} 环境的配置监听",
            Context.ConnectionId, appId, environment);
    }

    /// <summary>
    /// 获取配置组名称
    /// </summary>
    private string GetConfigGroupName(string appId, string environment)
    {
        return $"config:{appId}:{environment}";
    }
} 