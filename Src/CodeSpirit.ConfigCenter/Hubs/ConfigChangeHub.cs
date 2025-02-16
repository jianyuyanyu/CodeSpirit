using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.ConfigCenter.Hubs;

/// <summary>
/// 配置变更通知 Hub
/// </summary>
public class ConfigChangeHub : Hub
{
    /// <summary>
    /// 加入应用配置组
    /// </summary>
    public async Task JoinAppGroup(string appId, string environment)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{appId}:{environment}");
    }

    /// <summary>
    /// 离开应用配置组
    /// </summary>
    public async Task LeaveAppGroup(string appId, string environment)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{appId}:{environment}");
    }
} 