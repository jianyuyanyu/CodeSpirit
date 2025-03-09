using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.Messaging.Hubs;

/// <summary>
/// 聊天实时通信Hub
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IMessageService _messageService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="chatService">聊天服务</param>
    /// <param name="messageService">消息服务</param>
    public ChatHub(IChatService chatService, IMessageService messageService)
    {
        _chatService = chatService;
        _messageService = messageService;
    }

    /// <summary>
    /// 加入对话组
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    /// <summary>
    /// 离开对话组
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    /// <summary>
    /// 发送消息到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="content">消息内容</param>
    /// <param name="senderId">发送者ID</param>
    /// <param name="senderName">发送者名称</param>
    public async Task SendMessage(Guid conversationId, string content, string senderId, string senderName)
    {
        try
        {
            var message = await _chatService.SendMessageAsync(conversationId, content, senderId, senderName);
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 获取对话历史消息
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    public async Task GetConversationHistory(Guid conversationId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var (messages, totalCount) = await _chatService.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
            await Clients.Caller.SendAsync("ReceiveHistory", messages, totalCount);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 标记对话消息为已读
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    public async Task MarkAsRead(Guid conversationId, string userId)
    {
        try
        {
            await _chatService.MarkConversationAsReadAsync(conversationId, userId);
            await Clients.Group(conversationId.ToString()).SendAsync("MessageRead", userId, conversationId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 获取用户的所有对话
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task GetUserConversations(string userId)
    {
        try
        {
            var conversations = await _chatService.GetUserConversationsAsync(userId);
            await Clients.Caller.SendAsync("ReceiveConversations", conversations);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 创建新对话
    /// </summary>
    /// <param name="title">对话标题</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="creatorName">创建者名称</param>
    /// <param name="participantIds">参与者ID列表</param>
    public async Task CreateConversation(string title, string creatorId, string creatorName, List<string> participantIds)
    {
        try
        {
            var conversation = await _chatService.CreateConversationAsync(title, creatorId, creatorName, participantIds);
            await Clients.Caller.SendAsync("ConversationCreated", conversation);
            
            // 通知所有参与者有新对话
            foreach (var participantId in participantIds)
            {
                await Clients.User(participantId).SendAsync("NewConversation", conversation);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 添加用户到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名称</param>
    public async Task AddUserToConversation(Guid conversationId, string userId, string userName)
    {
        try
        {
            await _chatService.AddParticipantAsync(conversationId, userId, userName);
            
            // 通知对话组有新成员加入
            await Clients.Group(conversationId.ToString()).SendAsync("UserJoined", conversationId, userId, userName);
            
            // 通知被添加的用户
            await Clients.User(userId).SendAsync("AddedToConversation", conversationId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// 从对话中移除用户
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    public async Task RemoveUserFromConversation(Guid conversationId, string userId)
    {
        try
        {
            await _chatService.RemoveParticipantAsync(conversationId, userId);
            
            // 通知对话组有成员离开
            await Clients.Group(conversationId.ToString()).SendAsync("UserLeft", conversationId, userId);
            
            // 通知被移除的用户
            await Clients.User(userId).SendAsync("RemovedFromConversation", conversationId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }
} 