namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 通知配置
/// </summary>
public class NotificationConfig
{
    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.OnFailure;

    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// 邮件接收者列表
    /// </summary>
    public List<string>? EmailRecipients { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 连续失败多少次后通知
    /// </summary>
    public int FailureThreshold { get; set; } = 1;
}
