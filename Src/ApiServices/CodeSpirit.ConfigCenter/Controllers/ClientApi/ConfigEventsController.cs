using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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

        // 禁用响应缓冲，确保SSE数据能够立即发送
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // 设置SSE响应头（使用 Append 确保兼容性）
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no"); // 禁用Nginx缓冲
        
        // 设置状态码（必须在StartAsync之前设置）
        Response.StatusCode = 200;

        // 立即启动响应，确保响应头被发送到客户端
        // 这对于 HttpCompletionOption.ResponseHeadersRead 的客户端非常重要
        // 必须在设置所有响应头之后调用
        await Response.StartAsync(cancellationToken);

        try
        {
            // 发送初始连接成功消息
            var connectedMessage = $"data: {{\"type\":\"Connected\",\"appId\":\"{appId}\"}}\n\n";
            
            _logger.LogDebug("正在发送SSE Connected消息: AppId={AppId}, Message={Message}", appId, connectedMessage.Replace("\n", "\\n"));
            
            await Response.WriteAsync(connectedMessage, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation("SSE连接已建立并发送Connected消息: AppId={AppId}", appId);

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
                    _logger.LogDebug("发送SSE心跳: AppId={AppId}", appId);
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

