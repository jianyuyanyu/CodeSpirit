using CodeSpirit.Messaging.Models;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 聊天服务接口
/// </summary>
public interface IChatService
{
    /// <summary>
    /// 获取用户的所有对话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>对话列表</returns>
    Task<List<Conversation>> GetUserConversationsAsync(string userId);
    
    /// <summary>
    /// 获取对话详情
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
    /// <param name="title">对话标题</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="creatorName">创建者名称</param>
    /// <param name="participantIds">参与者ID列表</param>
    /// <returns>创建的对话</returns>
    Task<Conversation> CreateConversationAsync(string title, string creatorId, string creatorName, List<string> participantIds);
    
    /// <summary>
    /// 发送消息到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="content">消息内容</param>
    /// <param name="senderId">发送者ID</param>
    /// <param name="senderName">发送者名称</param>
    /// <returns>发送的消息</returns>
    Task<Message> SendMessageAsync(Guid conversationId, string content, string senderId, string senderName);
    
    /// <summary>
    /// 向对话添加参与者
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名称</param>
    /// <returns>是否成功</returns>
    Task<bool> AddParticipantAsync(Guid conversationId, string userId, string userName);
    
    /// <summary>
    /// 从对话中移除参与者
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveParticipantAsync(Guid conversationId, string userId);
    
    /// <summary>
    /// 获取用户在对话中的未读消息数
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    Task<int> GetUnreadMessagesCountAsync(Guid conversationId, string userId);
    
    /// <summary>
    /// 标记对话中消息为已读
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkConversationAsReadAsync(Guid conversationId, string userId);
    
    /// <summary>
    /// 创建或获取两个用户之间的私聊对话
    /// </summary>
    /// <param name="userId1">用户1 ID</param>
    /// <param name="userName1">用户1 名称</param>
    /// <param name="userId2">用户2 ID</param>
    /// <param name="userName2">用户2 名称</param>
    /// <returns>对话实体</returns>
    Task<Conversation> GetOrCreatePrivateConversationAsync(string userId1, string userName1, string userId2, string userName2);
    
    /// <summary>
    /// 获取所有对话
    /// </summary>
    /// <param name="title">标题（模糊查询）</param>
    /// <param name="participantId">参与者ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>对话分页列表</returns>
    Task<(List<Conversation> Conversations, int TotalCount)> GetConversationsAsync(
        string? title = null,
        string? participantId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20);
    
    /// <summary>
    /// 批量删除对话
    /// </summary>
    /// <param name="conversationIds">对话ID列表</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchDeleteConversationsAsync(List<Guid> conversationIds);
} 