using CodeSpirit.Messaging.Models;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 消息服务接口
/// </summary>
public interface IMessageService
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
    /// 发送系统通知
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="recipientId">接收者ID</param>
    /// <returns>创建的消息</returns>
    Task<Message> SendSystemNotificationAsync(string title, string content, string recipientId);
    
    /// <summary>
    /// 发送系统通知给多个用户
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="recipientIds">接收者ID列表</param>
    /// <returns>创建的消息列表</returns>
    Task<List<Message>> SendSystemNotificationAsync(string title, string content, List<string> recipientIds);
    
    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> DeleteMessageAsync(Guid messageId, string userId);
} 