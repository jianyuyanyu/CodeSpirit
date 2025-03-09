namespace CodeSpirit.Messaging.Models;

/// <summary>
/// 表示用户之间的对话
/// </summary>
public class Conversation
{
    /// <summary>
    /// 对话唯一标识
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// 对话名称/标题
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// 对话参与者列表
    /// </summary>
    public List<ConversationParticipant> Participants { get; set; } = new();
    
    /// <summary>
    /// 对话消息列表
    /// </summary>
    public List<Message> Messages { get; set; } = new();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityAt { get; set; }
} 