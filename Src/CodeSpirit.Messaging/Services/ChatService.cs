using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Repositories;

namespace CodeSpirit.Messaging.Services;

/// <summary>
/// 聊天服务实现
/// </summary>
public class ChatService(IConversationRepository conversationRepository) : IChatService
{
    private readonly IConversationRepository _conversationRepository = conversationRepository;

    /// <inheritdoc />
    public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _conversationRepository.GetUserConversationsAsync(userId);
    }

    /// <inheritdoc />
    public async Task<Conversation> GetConversationByIdAsync(Guid conversationId)
    {
        return await _conversationRepository.GetConversationByIdAsync(conversationId);
    }

    /// <inheritdoc />
    public async Task<(List<Message> Messages, int TotalCount)> GetConversationMessagesAsync(Guid conversationId, int pageNumber = 1, int pageSize = 20)
    {
        return await _conversationRepository.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<Conversation> CreateConversationAsync(string title, string creatorId, string creatorName, List<string> participantIds)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(creatorId);
        ArgumentNullException.ThrowIfNull(creatorName);
        ArgumentNullException.ThrowIfNull(participantIds);

        var conversation = new Conversation
        {
            Title = title,
            Participants = new List<ConversationParticipant>
            {
                new ConversationParticipant
                {
                    UserId = creatorId,
                    UserName = creatorName,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        // 添加其他参与者
        foreach (var participantId in participantIds.Where(id => id != creatorId))
        {
            // 这里简化处理，实际应用中应该获取用户名
            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = participantId,
                UserName = $"User {participantId}",
                JoinedAt = DateTime.UtcNow
            });
        }

        return await _conversationRepository.CreateConversationAsync(conversation);
    }

    /// <inheritdoc />
    public async Task<Message> SendMessageAsync(Guid conversationId, string content, string senderId, string senderName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(senderId);
        ArgumentNullException.ThrowIfNull(senderName);

        var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new ArgumentException($"Conversation with ID {conversationId} not found", nameof(conversationId));
        }

        // 验证发送者是否是对话的参与者
        if (!conversation.Participants.Any(p => p.UserId == senderId && !p.HasLeft))
        {
            throw new InvalidOperationException("Sender is not a participant in this conversation");
        }

        // 获取所有接收者
        var recipients = conversation.Participants
            .Where(p => p.UserId != senderId && !p.HasLeft)
            .ToList();

        // 创建消息
        var message = new Message
        {
            Content = content,
            SenderId = senderId,
            SenderName = senderName,
            RecipientId = recipients.First().UserId, // 这里简化为只发给第一个接收者，实际可能需要复制消息给所有人
            Type = MessageType.UserMessage,
            Title = $"来自 {senderName} 的消息"
        };

        // 添加消息到对话
        return await _conversationRepository.AddMessageToConversationAsync(conversationId, message);
    }

    /// <inheritdoc />
    public async Task<bool> AddParticipantAsync(Guid conversationId, string userId, string userName)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(userName);

        var participant = new ConversationParticipant
        {
            UserId = userId,
            UserName = userName,
            JoinedAt = DateTime.UtcNow
        };

        return await _conversationRepository.AddParticipantAsync(conversationId, participant);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveParticipantAsync(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _conversationRepository.RemoveParticipantAsync(conversationId, userId);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessagesCountAsync(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        return await _conversationRepository.GetUnreadMessagesCountAsync(conversationId, userId);
    }

    /// <inheritdoc />
    public async Task<bool> MarkConversationAsReadAsync(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var messages = await _conversationRepository.GetConversationMessagesAsync(conversationId);
        if (messages.Messages.Count == 0)
        {
            return true;
        }

        // 更新参与者的最后读取时间
        var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
        if (conversation == null)
        {
            return false;
        }

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant != null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            await _conversationRepository.UpdateLastActivityAsync(conversationId);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<Conversation> GetOrCreatePrivateConversationAsync(string userId1, string userName1, string userId2, string userName2)
    {
        ArgumentNullException.ThrowIfNull(userId1);
        ArgumentNullException.ThrowIfNull(userName1);
        ArgumentNullException.ThrowIfNull(userId2);
        ArgumentNullException.ThrowIfNull(userName2);

        // 获取用户1的所有对话
        var conversations = await _conversationRepository.GetUserConversationsAsync(userId1);

        // 查找是否已有1对1对话
        var privateConversation = conversations.FirstOrDefault(c => 
            c.Participants.Count == 2 && 
            c.Participants.Any(p => p.UserId == userId2 && !p.HasLeft));

        if (privateConversation != null)
        {
            return privateConversation;
        }

        // 创建新的1对1对话
        return await CreateConversationAsync(
            $"{userName1} 与 {userName2} 的对话", 
            userId1, 
            userName1, 
            new List<string> { userId2 });
    }
} 