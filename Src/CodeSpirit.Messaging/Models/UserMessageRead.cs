using CodeSpirit.Core;

namespace CodeSpirit.Messaging.Models;

/// <summary>
/// 表示用户对消息的已读状态
/// </summary>
public class UserMessageRead : IMultiTenant
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 消息ID
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 读取时间
    /// </summary>
    public DateTime? ReadAt { get; set; }
    
    /// <summary>
    /// 关联的消息
    /// </summary>
    public Message Message { get; set; }

    /// <summary>
    /// 租户ID - 多租户支持
    /// </summary>
    public string TenantId { get; set; }
} 