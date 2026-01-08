using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers.ClientApi;

/// <summary>
/// 配置变更事件推送端点
/// </summary>
[Route("api/config/client/events")]
[ApiController]
[AllowAnonymous]
public class ConfigEventsController : ControllerBase
{
    private readonly SseConnectionManager _connectionManager;
    private readonly ILogger<ConfigEventsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigEventsController(
        SseConnectionManager connectionManager,
        ILogger<ConfigEventsController> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// SSE 端点 - 客户端订阅配置变更事件
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpGet("{appId}")]
    [DisplayName("订阅配置变更事件")]
    public async Task Subscribe(string appId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appId))
        {
            _logger.LogWarning("SSE订阅请求缺少AppId");
            Response.StatusCode = 400;
            return;
        }

        // 设置SSE响应头
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no"); // 禁用Nginx缓冲

        try
        {
            // 发送初始连接成功消息
            var connectedMessage = $"data: {{\"type\":\"Connected\",\"appId\":\"{appId}\"}}\n\n";
            await Response.WriteAsync(connectedMessage, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation("SSE连接已建立: AppId={AppId}", appId);

            // 注册连接（异步更新健康状态）
            await _connectionManager.AddConnectionAsync(appId, Response);

            // 保持连接直到取消，定期发送心跳
            var heartbeatInterval = TimeSpan.FromSeconds(30);
            var lastHeartbeat = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                // 检查是否需要发送心跳
                if (DateTime.UtcNow - lastHeartbeat >= heartbeatInterval)
                {
                    await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    lastHeartbeat = DateTime.UtcNow;
                }

                // 短暂延迟避免CPU占用过高
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE连接已取消: AppId={AppId}", appId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE连接发生错误: AppId={AppId}", appId);
        }
        finally
        {
            // 移除连接（异步更新健康状态）
            var connection = new SseConnection(appId, Response);
            await _connectionManager.RemoveConnectionAsync(appId, connection);
            _logger.LogInformation("SSE连接已关闭: AppId={AppId}", appId);
        }
    }
}

