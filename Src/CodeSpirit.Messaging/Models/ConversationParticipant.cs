namespace CodeSpirit.Messaging.Models;

/// <summary>
/// 表示对话中的参与者
/// </summary>
public class ConversationParticipant
{
    /// <summary>
    /// 参与者ID
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// 对话ID
    /// </summary>
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// 参与者名称
    /// </summary>
    public string UserName { get; set; }
    
    /// <summary>
    /// 最后读取消息的时间
    /// </summary>
    public DateTime? LastReadAt { get; set; }
    
    /// <summary>
    /// 是否已退出对话
    /// </summary>
    public bool HasLeft { get; set; }
    
    /// <summary>
    /// 加入对话的时间
    /// </summary>
    public DateTime JoinedAt { get; set; }
} 