using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Repositories;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 消息服务实现
/// </summary>
public class MessageService(IMessageRepository messageRepository) : IMessageService
{
    private readonly IMessageRepository _messageRepository = messageRepository;

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetUserMessagesAsync(string userId, int pageNumber = 1, int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _messageRepository.GetUserMessagesAsync(userId, pageNumber, pageSize);
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
} 