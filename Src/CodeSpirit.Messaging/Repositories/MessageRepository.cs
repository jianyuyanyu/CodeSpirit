using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Messaging.Repositories;

/// <summary>
/// 消息仓储实现
/// </summary>
public class MessageRepository(MessagingDbContext dbContext) : IMessageRepository
{
    private readonly MessagingDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetUserMessagesAsync(string userId, int pageNumber = 1, int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var query = _dbContext.Messages
            .Where(m => m.RecipientId == userId)
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (messages, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await _dbContext.Messages
            .CountAsync(m => m.RecipientId == userId && !m.IsRead);
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.RecipientId == userId);

        if (message == null)
        {
            return false;
        }

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var unreadMessages = await _dbContext.Messages
            .Where(m => m.RecipientId == userId && !m.IsRead)
            .ToListAsync();

        if (!unreadMessages.Any())
        {
            return true;
        }

        var now = DateTime.UtcNow;
        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<Message> AddMessageAsync(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;
        message.IsRead = false;

        await _dbContext.Messages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        return message;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMessageAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.RecipientId == userId);

        if (message == null)
        {
            return false;
        }

        _dbContext.Messages.Remove(message);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    /// <inheritdoc />
    public async Task<Message> GetMessageByIdAsync(Guid messageId)
    {
        return await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
} 