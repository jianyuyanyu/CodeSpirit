using System.ComponentModel;
using CodeSpirit.Messaging.Models;

namespace CodeSpirit.MessagingApi.Dtos.Responses;

/// <summary>
/// 会话DTO
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    [DisplayName("ID")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// 会话标题
    /// </summary>
    [DisplayName("标题")]
    public string Title { get; set; }
    
    /// <summary>
    /// 参与者列表
    /// </summary>
    [DisplayName("参与者")]
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    
    /// <summary>
    /// 消息数量
    /// </summary>
    [DisplayName("消息数量")]
    public int MessageCount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    [DisplayName("最后活动时间")]
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// 会话参与者DTO
/// </summary>
public class ConversationParticipantDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string UserId { get; set; }
    
    /// <summary>
    /// 用户名称
    /// </summary>
    [DisplayName("用户名称")]
    public string UserName { get; set; }
} 