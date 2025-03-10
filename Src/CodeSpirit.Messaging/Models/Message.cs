namespace CodeSpirit.Messaging.Models;

/// <summary>
/// 表示系统中的一条消息
/// </summary>
public class Message
{
    /// <summary>
    /// 消息唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 消息类型 (0: 系统通知, 1: 用户消息)
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 发送者ID (系统通知时可为null)
    /// </summary>
    public string SenderId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    public string SenderName { get; set; }

    /// <summary>
    /// 接收者ID
    /// </summary>
    public string RecipientId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
} 