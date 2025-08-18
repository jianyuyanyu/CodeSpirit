namespace CodeSpirit.MessagingApi.Dtos.Requests;

/// <summary>
/// 系统通知请求
/// </summary>
public class SystemNotificationRequest
{
    /// <summary>
    /// 标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 单个接收者ID（与接收者列表二选一）
    /// </summary>
    public string? RecipientId { get; set; }

    /// <summary>
    /// 接收者ID列表（与单个接收者二选一）
    /// </summary>
    public List<string>? RecipientIds { get; set; }
}