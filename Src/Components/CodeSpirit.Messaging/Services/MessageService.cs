using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Repositories;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 消息服务实现
/// </summary>
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="messageRepository">消息仓储</param>
    public MessageService(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetUserMessagesAsync(string userId, int pageNumber = 1, int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.GetUserMessagesAsync(userId, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetUnreadMessagesAsync(string userId, int pageNumber = 1, int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.GetUnreadMessagesAsync(userId, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.GetUnreadMessageCountAsync(userId);
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.MarkAsReadAsync(messageId, userId);
    }

    /// <inheritdoc />
    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.MarkAllAsReadAsync(userId);
    }

    /// <inheritdoc />
    public async Task<Message> SendSystemNotificationAsync(string title, string content, string recipientId)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(recipientId);

        var message = new Message
        {
            Title = title,
            Content = content,
            RecipientId = recipientId,
            Type = MessageType.SystemNotification,
            SenderName = "系统通知"
        };

        return await _messageRepository.AddMessageAsync(message);
    }

    /// <inheritdoc />
    public async Task<List<Message>> SendSystemNotificationAsync(string title, string content, List<string> recipientIds)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(recipientIds);

        var messages = new List<Message>();

        foreach (var recipientId in recipientIds)
        {
            var message = await SendSystemNotificationAsync(title, content, recipientId);
            messages.Add(message);
        }

        return messages;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMessageAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.DeleteMessageAsync(messageId, userId);
    }

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetMessagesAsync(
        MessageType? type = null,
        string? title = null,
        string? senderId = null,
        string? senderName = null,
        string? recipientId = null,
        bool? isRead = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return await _messageRepository.GetMessagesAsync(
            type, title, senderId, senderName, recipientId, isRead, 
            startDate, endDate, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<bool> BatchDeleteMessagesAsync(List<Guid> messageIds)
    {
        return await _messageRepository.BatchDeleteMessagesAsync(messageIds);
    }

    /// <inheritdoc />
    public async Task<bool> BatchMarkAsReadAsync(List<Guid> messageIds)
    {
        // 获取当前用户ID - 这里应该使用实际的用户认证逻辑
        string currentUserId = GetCurrentUserId();
        return await _messageRepository.BatchMarkAsReadAsync(messageIds, currentUserId);
    }

    /// <summary>
    /// 获取当前登录用户ID
    /// </summary>
    /// <returns>当前用户ID</returns>
    private string GetCurrentUserId()
    {
        // 实际项目中，应该从 HttpContext 或其他认证上下文中获取当前用户ID
        // 这里临时返回一个默认值
        return "system";
    }

    /// <inheritdoc />
    public async Task<Message?> GetMessageByIdAsync(Guid messageId)
    {
        return await _messageRepository.GetMessageByIdAsync(messageId);
    }
} 