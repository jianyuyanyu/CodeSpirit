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

        // 查询用户特定消息和全局通知
        var query = _dbContext.Messages
            .Where(m => m.RecipientId == userId || m.RecipientId == "all")
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 获取用户已读状态
        var messageIds = messages.Select(m => m.Id).ToList();
        var readStatuses = await _dbContext.UserMessageReads
            .Where(r => r.UserId == userId && messageIds.Contains(r.MessageId))
            .ToDictionaryAsync(r => r.MessageId, r => r);

        return (messages, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // 查询用户特定消息和全局通知
        var messageIds = await _dbContext.Messages
            .Where(m => m.RecipientId == userId || m.RecipientId == "all")
            .Select(m => m.Id)
            .ToListAsync();

        // 查询用户已读消息
        var readMessageIds = await _dbContext.UserMessageReads
            .Where(r => r.UserId == userId && r.IsRead && messageIds.Contains(r.MessageId))
            .Select(r => r.MessageId)
            .ToListAsync();

        // 未读消息数量 = 总消息数 - 已读消息数
        return messageIds.Count - readMessageIds.Count;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsReadAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // 验证消息存在且适用于该用户
        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && (m.RecipientId == userId || m.RecipientId == "all"));

        if (message == null)
        {
            return false;
        }

        // 查找现有的已读记录
        var readStatus = await _dbContext.UserMessageReads
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (readStatus == null)
        {
            // 创建新的已读记录
            readStatus = new UserMessageRead
            {
                MessageId = messageId,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow
            };
            await _dbContext.UserMessageReads.AddAsync(readStatus);
        }
        else
        {
            // 更新现有记录
            readStatus.IsRead = true;
            readStatus.ReadAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // 获取所有适用于该用户的未读消息
        var messageIds = await _dbContext.Messages
            .Where(m => m.RecipientId == userId || m.RecipientId == "all")
            .Select(m => m.Id)
            .ToListAsync();

        // 获取用户已有的已读记录
        var existingReadIds = await _dbContext.UserMessageReads
            .Where(r => r.UserId == userId && messageIds.Contains(r.MessageId))
            .Select(r => r.MessageId)
            .ToListAsync();

        // 更新现有记录
        await _dbContext.UserMessageReads
            .Where(r => r.UserId == userId && messageIds.Contains(r.MessageId) && !r.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsRead, true)
                .SetProperty(r => r.ReadAt, DateTime.UtcNow));

        // 需要添加的新记录
        var newReadIds = messageIds.Except(existingReadIds).ToList();
        if (newReadIds.Any())
        {
            var now = DateTime.UtcNow;
            var newReadStatuses = newReadIds.Select(messageId => new UserMessageRead
            {
                MessageId = messageId,
                UserId = userId,
                IsRead = true,
                ReadAt = now
            }).ToList();

            await _dbContext.UserMessageReads.AddRangeAsync(newReadStatuses);
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

        await _dbContext.Messages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        return message;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMessageAsync(Guid messageId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && (m.RecipientId == userId || m.RecipientId == "all"));

        if (message == null)
        {
            return false;
        }

        // 如果是全局消息，只删除用户的已读状态而不删除消息本身
        if (message.RecipientId == "all")
        {
            var readStatus = await _dbContext.UserMessageReads
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

            if (readStatus != null)
            {
                // 标记为已删除（这里可以扩展 UserMessageRead 实体添加 IsDeleted 字段）
                // 或者直接移除已读记录
                _dbContext.UserMessageReads.Remove(readStatus);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        // 如果是用户特定消息，则可以直接删除
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

        // 如果需要按已读/未读状态筛选，需要联合查询 UserMessageRead 表
        if (isRead.HasValue && !string.IsNullOrEmpty(recipientId))
        {
            // 查询指定用户的已读消息ID
            var readMessageIds = await _dbContext.UserMessageReads
                .Where(r => r.UserId == recipientId && r.IsRead == isRead.Value)
                .Select(r => r.MessageId)
                .ToListAsync();

            if (isRead.Value)
            {
                // 查询已读消息
                query = query.Where(m => readMessageIds.Contains(m.Id));
            }
            else
            {
                // 查询未读消息
                query = query.Where(m => !readMessageIds.Contains(m.Id));
            }
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<bool> BatchMarkAsReadAsync(List<Guid> messageIds, string userId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(messageIds);
            ArgumentNullException.ThrowIfNull(userId);
            
            if (!messageIds.Any())
            {
                return true; // 没有需要标记的消息，视为成功
            }
            
            // 验证所有消息存在
            var messages = await _dbContext.Messages
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();

            // 如果部分消息不存在，仅处理存在的消息
            var existingMessageIds = messages.Select(m => m.Id).ToList();
            
            // 已存在的用户消息关系记录
            var existingReadRecords = await _dbContext.UserMessageReads
                .Where(r => r.UserId == userId && existingMessageIds.Contains(r.MessageId))
                .ToListAsync();
            
            var existingReadMessageIds = existingReadRecords.Select(r => r.MessageId).ToList();
            
            // 更新现有记录
            foreach (var record in existingReadRecords.Where(r => !r.IsRead))
            {
                record.IsRead = true;
                record.ReadAt = DateTime.UtcNow;
            }
            
            // 创建新记录
            var newReadRecords = existingMessageIds
                .Except(existingReadMessageIds)
                .Select(messageId => new UserMessageRead
                {
                    MessageId = messageId,
                    UserId = userId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                })
                .ToList();
            
            if (newReadRecords.Any())
            {
                await _dbContext.UserMessageReads.AddRangeAsync(newReadRecords);
            }
            
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
} 