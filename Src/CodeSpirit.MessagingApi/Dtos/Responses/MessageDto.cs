using System.ComponentModel;
using CodeSpirit.Messaging.Models;

namespace CodeSpirit.MessagingApi.Dtos.Responses;

/// <summary>
/// 消息DTO
/// </summary>
public class MessageDto
{
    /// <summary>
    /// 消息唯一标识
    /// </summary>
    [DisplayName("ID")]
    public Guid Id { get; set; }

    /// <summary>
    /// 消息类型 (0: 系统通知, 1: 用户消息)
    /// </summary>
    [DisplayName("消息类型")]
    public MessageType Type { get; set; }

    /// <summary>
    /// 消息类型名称
    /// </summary>
    [DisplayName("消息类型")]
    public string TypeName => Type.ToString();

    /// <summary>
    /// 消息标题
    /// </summary>
    [DisplayName("标题")]
    public string Title { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [DisplayName("内容")]
    public string Content { get; set; }

    /// <summary>
    /// 发送者ID
    /// </summary>
    [DisplayName("发送者ID")]
    public string SenderId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    [DisplayName("发送者")]
    public string SenderName { get; set; }

    /// <summary>
    /// 接收者ID
    /// </summary>
    [DisplayName("接收者ID")]
    public string RecipientId { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [DisplayName("是否已读")]
    public bool IsRead { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("发送时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 读取时间
    /// </summary>
    [DisplayName("读取时间")]
    public DateTime? ReadAt { get; set; }
} 