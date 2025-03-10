using CodeSpirit.Messaging.Models;

namespace CodeSpirit.Messaging.Repositories;

/// <summary>
/// 消息仓储接口
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// 获取用户的所有消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>用户消息列表</returns>
    Task<(List<Message> Messages, int TotalCount)> GetUserMessagesAsync(string userId, int pageNumber = 1, int pageSize = 20);
    
    /// <summary>
    /// 获取用户的未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    Task<int> GetUnreadMessageCountAsync(string userId);
    
    /// <summary>
    /// 标记消息为已读
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> MarkAsReadAsync(Guid messageId, string userId);
    
    /// <summary>
    /// 标记用户的所有消息为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> MarkAllAsReadAsync(string userId);
    
    /// <summary>
    /// 添加新消息
    /// </summary>
    /// <param name="message">消息实体</param>
    /// <returns>添加的消息</returns>
    Task<Message> AddMessageAsync(Message message);
    
    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> DeleteMessageAsync(Guid messageId, string userId);
    
    /// <summary>
    /// 获取指定消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <returns>消息实体</returns>
    Task<Message> GetMessageByIdAsync(Guid messageId);

    /// <summary>
    /// 获取消息分页列表
    /// </summary>
    /// <param name="type">消息类型</param>
    /// <param name="title">标题（模糊查询）</param>
    /// <param name="senderId">发送者ID</param>
    /// <param name="senderName">发送者名称（模糊查询）</param>
    /// <param name="recipientId">接收者ID</param>
    /// <param name="isRead">是否已读</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>消息分页列表</returns>
    Task<(List<Message> Messages, int TotalCount)> GetMessagesAsync(
        MessageType? type = null,
        string? title = null,
        string? senderId = null,
        string? senderName = null,
        string? recipientId = null,
        bool? isRead = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// 批量删除消息
    /// </summary>
    /// <param name="messageIds">消息ID列表</param>
    /// <returns>操作是否成功</returns>
    Task<bool> BatchDeleteMessagesAsync(List<Guid> messageIds);

    /// <summary>
    /// 批量标记消息为已读
    /// </summary>
    /// <param name="messageIds">消息ID列表</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> BatchMarkAsReadAsync(List<Guid> messageIds, string userId);
} 