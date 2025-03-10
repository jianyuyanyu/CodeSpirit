using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 消息服务实现
/// </summary>
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly MessagingDbContext _dbContext;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="messageRepository">消息仓储</param>
    /// <param name="dbContext">数据库上下文</param>
    public MessageService(IMessageRepository messageRepository, MessagingDbContext dbContext)
    {
        _messageRepository = messageRepository;
        _dbContext = dbContext;
    }

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

    /// <summary>
    /// 获取消息分页列表
    /// </summary>
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
        var query = _dbContext.Messages.AsQueryable();

        // 应用过滤条件
        if (type.HasValue)
        {
            query = query.Where(m => m.Type == type.Value);
        }

        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where(m => m.Title.Contains(title));
        }

        if (!string.IsNullOrEmpty(senderId))
        {
            query = query.Where(m => m.SenderId == senderId);
        }

        if (!string.IsNullOrEmpty(senderName))
        {
            query = query.Where(m => m.SenderName.Contains(senderName));
        }

        if (!string.IsNullOrEmpty(recipientId))
        {
            query = query.Where(m => m.RecipientId == recipientId);
        }

        if (isRead.HasValue)
        {
            query = query.Where(m => m.IsRead == isRead.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= endDate.Value.AddDays(1).AddSeconds(-1));
        }

        // 计算总数
        int totalCount = await query.CountAsync();

        // 分页并排序
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (messages, totalCount);
    }

    /// <summary>
    /// 批量删除消息
    /// </summary>
    public async Task<bool> BatchDeleteMessagesAsync(List<Guid> messageIds)
    {
        try
        {
            var messages = await _dbContext.Messages
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();

            if (!messages.Any())
            {
                return false;
            }

            _dbContext.Messages.RemoveRange(messages);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 批量标记消息为已读
    /// </summary>
    public async Task<bool> BatchMarkAsReadAsync(List<Guid> messageIds)
    {
        try
        {
            var messages = await _dbContext.Messages
                .Where(m => messageIds.Contains(m.Id) && !m.IsRead)
                .ToListAsync();

            if (!messages.Any())
            {
                return true; // 没有需要标记的消息，视为成功
            }

            var now = DateTime.Now;
            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = now;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取消息详情
    /// </summary>
    public async Task<Message?> GetMessageByIdAsync(Guid messageId)
    {
        return await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
} 