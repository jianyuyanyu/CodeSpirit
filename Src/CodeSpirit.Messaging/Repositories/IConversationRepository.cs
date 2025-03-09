using CodeSpirit.Messaging.Models;

namespace CodeSpirit.Messaging.Repositories;

/// <summary>
/// 对话仓储接口
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// 获取用户的所有对话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>对话列表</returns>
    Task<List<Conversation>> GetUserConversationsAsync(string userId);
    
    /// <summary>
    /// 获取特定对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <returns>对话实体</returns>
    Task<Conversation> GetConversationByIdAsync(Guid conversationId);
    
    /// <summary>
    /// 获取对话消息
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>消息列表</returns>
    Task<(List<Message> Messages, int TotalCount)> GetConversationMessagesAsync(Guid conversationId, int pageNumber = 1, int pageSize = 20);
    
    /// <summary>
    /// 创建新对话
    /// </summary>
    /// <param name="conversation">对话实体</param>
    /// <returns>创建的对话</returns>
    Task<Conversation> CreateConversationAsync(Conversation conversation);
    
    /// <summary>
    /// 添加参与者到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="participant">参与者</param>
    /// <returns>是否成功</returns>
    Task<bool> AddParticipantAsync(Guid conversationId, ConversationParticipant participant);
    
    /// <summary>
    /// 从对话中移除参与者
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveParticipantAsync(Guid conversationId, string userId);
    
    /// <summary>
    /// 添加消息到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="message">消息</param>
    /// <returns>添加的消息</returns>
    Task<Message> AddMessageToConversationAsync(Guid conversationId, Message message);
    
    /// <summary>
    /// 更新对话的最后活动时间
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateLastActivityAsync(Guid conversationId);
    
    /// <summary>
    /// 获取用户在特定对话中的未读消息数
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数</returns>
    Task<int> GetUnreadMessagesCountAsync(Guid conversationId, string userId);
} 