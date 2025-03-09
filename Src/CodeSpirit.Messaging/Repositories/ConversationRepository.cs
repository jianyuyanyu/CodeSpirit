using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Messaging.Repositories;

/// <summary>
/// 对话仓储实现
/// </summary>
public class ConversationRepository(MessagingDbContext dbContext) : IConversationRepository
{
    private readonly MessagingDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await _dbContext.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(p => p.UserId == userId && !p.HasLeft))
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Conversation> GetConversationByIdAsync(Guid conversationId)
    {
        return await _dbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetConversationMessagesAsync(Guid conversationId, int pageNumber = 1, int pageSize = 20)
    {
        var query = _dbContext.Messages
            .Where(m => EF.Property<Guid>(m, "ConversationId") == conversationId)
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (messages, totalCount);
    }

    /// <inheritdoc />
    public async Task<Conversation> CreateConversationAsync(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        conversation.Id = Guid.NewGuid();
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.LastActivityAt = DateTime.UtcNow;

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        return conversation;
    }

    /// <inheritdoc />
    public async Task<bool> AddParticipantAsync(Guid conversationId, ConversationParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);

        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return false;
        }

        // 检查用户是否已在对话中
        var existingParticipant = conversation.Participants
            .FirstOrDefault(p => p.UserId == participant.UserId);

        if (existingParticipant != null)
        {
            // 如果用户已离开对话，则重新加入
            if (existingParticipant.HasLeft)
            {
                existingParticipant.HasLeft = false;
                existingParticipant.JoinedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            
            return true;
        }

        participant.JoinedAt = DateTime.UtcNow;
        conversation.Participants.Add(participant);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveParticipantAsync(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return false;
        }

        var participant = conversation.Participants
            .FirstOrDefault(p => p.UserId == userId && !p.HasLeft);

        if (participant == null)
        {
            return false;
        }

        // 标记用户已离开对话而非直接删除
        participant.HasLeft = true;
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    /// <inheritdoc />
    public async Task<Message> AddMessageToConversationAsync(Guid conversationId, Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            throw new ArgumentException($"Conversation with ID {conversationId} not found", nameof(conversationId));
        }

        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;
        message.IsRead = false;
        message.Type = MessageType.UserMessage;

        // 设置外键
        _dbContext.Entry(message).Property("ConversationId").CurrentValue = conversationId;

        await _dbContext.Messages.AddAsync(message);

        // 更新对话的最后活动时间
        conversation.LastActivityAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        
        return message;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateLastActivityAsync(Guid conversationId)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return false;
        }

        conversation.LastActivityAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessagesCountAsync(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var participant = await _dbContext.ConversationParticipants
            .FirstOrDefaultAsync(p => EF.Property<Guid>(p, "ConversationId") == conversationId && p.UserId == userId);

        if (participant == null)
        {
            return 0;
        }

        return await _dbContext.Messages
            .CountAsync(m => 
                EF.Property<Guid>(m, "ConversationId") == conversationId && 
                !m.IsRead && 
                m.RecipientId == userId);
    }
} 