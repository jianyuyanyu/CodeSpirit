using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.Web.Hubs;

/// <summary>
/// 通用通知Hub
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 加入主题组
    /// </summary>
    public async Task JoinTopic(string topic, string sessionId = null)
    {
        var groupId = sessionId != null ? $"{topic}:{sessionId}" : topic;
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("客户端 {ConnectionId} 加入主题组 {GroupId}", Context.ConnectionId, groupId);
    }

    /// <summary>
    /// 离开主题组
    /// </summary>
    public async Task LeaveTopic(string topic, string sessionId = null)
    {
        var groupId = sessionId != null ? $"{topic}:{sessionId}" : topic;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("客户端 {ConnectionId} 离开主题组 {GroupId}", Context.ConnectionId, groupId);
    }

    /// <summary>
    /// 连接建立
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端 {ConnectionId} 已连接", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 连接断开
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation("客户端 {ConnectionId} 已断开连接", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
} 