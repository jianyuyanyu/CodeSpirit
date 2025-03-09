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
} 