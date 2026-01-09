using System.Text;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Dto;
using Newtonsoft.Json;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// Webhook 任务执行通知器
/// </summary>
public class WebhookTaskExecutionNotifier : ITaskExecutionNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookTaskExecutionNotifier> _logger;
    private readonly ScheduledTasksOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public WebhookTaskExecutionNotifier(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookTaskExecutionNotifier> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 发送任务执行通知
    /// </summary>
    /// <param name="notification">通知信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    public async Task<bool> NotifyAsync(TaskExecutionNotification notification, CancellationToken cancellationToken = default)
    {
        var config = notification.Config;
        
        // 检查是否需要发送通知
        if (config == null || !config.Enabled)
        {
            return true;
        }

        // 根据通知类型判断是否需要发送
        var shouldNotify = config.Type switch
        {
            NotificationType.All => true,
            NotificationType.OnFailure => !notification.IsSuccess,
            NotificationType.OnSuccess => notification.IsSuccess,
            NotificationType.None => false,
            _ => false
        };

        if (!shouldNotify)
        {
            _logger.LogDebug("根据通知配置跳过通知 - TaskId: {TaskId}, Type: {Type}", 
                notification.TaskId, config.Type);
            return true;
        }

        var success = true;

        // 发送 Webhook 通知
        if (!string.IsNullOrEmpty(config.WebhookUrl))
        {
            success &= await SendWebhookNotificationAsync(notification, config.WebhookUrl, cancellationToken);
        }

        return success;
    }

    /// <summary>
    /// 发送 Webhook 通知
    /// </summary>
    /// <param name="notification">通知信息</param>
    /// <param name="webhookUrl">Webhook URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    private async Task<bool> SendWebhookNotificationAsync(
        TaskExecutionNotification notification, 
        string webhookUrl, 
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var payload = new
            {
                taskId = notification.TaskId,
                taskName = notification.TaskName,
                executionId = notification.ExecutionId,
                status = notification.Status,
                isSuccess = notification.IsSuccess,
                startTime = notification.StartTime,
                endTime = notification.EndTime,
                duration = notification.Duration?.TotalSeconds,
                result = notification.Result,
                errorMessage = notification.ErrorMessage,
                executionNode = notification.ExecutionNode,
                retryCount = notification.RetryCount,
                timestamp = DateTime.UtcNow
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook 通知发送成功 - TaskId: {TaskId}, Url: {Url}", 
                    notification.TaskId, webhookUrl);
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook 通知发送失败 - TaskId: {TaskId}, Url: {Url}, StatusCode: {StatusCode}", 
                    notification.TaskId, webhookUrl, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook 通知发送异常 - TaskId: {TaskId}, Url: {Url}", 
                notification.TaskId, webhookUrl);
            return false;
        }
    }
}
