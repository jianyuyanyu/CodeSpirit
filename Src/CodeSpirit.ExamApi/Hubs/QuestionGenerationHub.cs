using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.ExamApi.Hubs;

/// <summary>
/// 题目生成实时通知中心
/// </summary>
public class QuestionGenerationHub : Hub
{
    private readonly ILogger<QuestionGenerationHub> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public QuestionGenerationHub(ILogger<QuestionGenerationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 客户端连接
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端连接: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接
    /// </summary>
    /// <param name="exception">异常信息</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("客户端断开连接: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 加入题目生成组
    /// </summary>
    /// <param name="groupId">组ID</param>
    public async Task JoinGenerationGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation("客户端 {ConnectionId} 加入题目生成组 {GroupId}", Context.ConnectionId, groupId);
    }
} 