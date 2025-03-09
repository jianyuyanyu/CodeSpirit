using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.MessagingApi.Controllers;

/// <summary>
/// 聊天控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Module("default", displayName: "默认")]
public class ChatController(IChatService chatService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;

    /// <summary>
    /// 获取用户的所有对话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>对话列表</returns>
    [HttpGet("user/{userId}/conversations")]
    public async Task<IActionResult> GetUserConversations(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var conversations = await _chatService.GetUserConversationsAsync(userId);
        return Ok(conversations);
    }

    /// <summary>
    /// 获取对话详情
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <returns>对话详情</returns>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        var conversation = await _chatService.GetConversationByIdAsync(conversationId);
        if (conversation == null)
        {
            return NotFound(new { Message = "对话不存在" });
        }
        
        return Ok(conversation);
    }

    /// <summary>
    /// 获取对话消息
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>消息列表</returns>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetConversationMessages(Guid conversationId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var (messages, totalCount) = await _chatService.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
        return Ok(new
        {
            Messages = messages,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// 创建新对话
    /// </summary>
    /// <param name="request">创建对话请求</param>
    /// <returns>创建的对话</returns>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Title);
        ArgumentNullException.ThrowIfNull(request.CreatorId);
        ArgumentNullException.ThrowIfNull(request.CreatorName);
        ArgumentNullException.ThrowIfNull(request.ParticipantIds);
        
        var conversation = await _chatService.CreateConversationAsync(
            request.Title,
            request.CreatorId,
            request.CreatorName,
            request.ParticipantIds);
        
        return CreatedAtAction(nameof(GetConversation), new { conversationId = conversation.Id }, conversation);
    }

    /// <summary>
    /// 发送消息到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>发送的消息</returns>
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.SenderId);
        ArgumentNullException.ThrowIfNull(request.SenderName);
        
        try
        {
            var message = await _chatService.SendMessageAsync(
                conversationId,
                request.Content,
                request.SenderId,
                request.SenderName);
            
            return Ok(message);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Message = "对话不存在" });
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { Message = "发送者不是对话参与者" });
        }
    }

    /// <summary>
    /// 添加用户到对话
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="request">添加用户请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("conversations/{conversationId}/participants")]
    public async Task<IActionResult> AddParticipant(Guid conversationId, [FromBody] AddParticipantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UserId);
        ArgumentNullException.ThrowIfNull(request.UserName);
        
        var result = await _chatService.AddParticipantAsync(conversationId, request.UserId, request.UserName);
        if (!result)
        {
            return NotFound(new { Message = "对话不存在" });
        }
        
        return Ok(new { Success = true });
    }

    /// <summary>
    /// 从对话中移除用户
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("conversations/{conversationId}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(Guid conversationId, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var result = await _chatService.RemoveParticipantAsync(conversationId, userId);
        if (!result)
        {
            return NotFound(new { Message = "对话不存在或用户不是对话参与者" });
        }
        
        return Ok(new { Success = true });
    }

    /// <summary>
    /// 标记对话中消息为已读
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid conversationId, [FromBody] string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var result = await _chatService.MarkConversationAsReadAsync(conversationId, userId);
        if (!result)
        {
            return NotFound(new { Message = "对话不存在" });
        }
        
        return Ok(new { Success = true });
    }

    /// <summary>
    /// 获取或创建两用户间的私聊对话
    /// </summary>
    /// <param name="request">私聊请求</param>
    /// <returns>对话详情</returns>
    [HttpPost("private")]
    public async Task<IActionResult> GetOrCreatePrivateConversation([FromBody] PrivateConversationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UserId1);
        ArgumentNullException.ThrowIfNull(request.UserName1);
        ArgumentNullException.ThrowIfNull(request.UserId2);
        ArgumentNullException.ThrowIfNull(request.UserName2);
        
        var conversation = await _chatService.GetOrCreatePrivateConversationAsync(
            request.UserId1,
            request.UserName1,
            request.UserId2,
            request.UserName2);
        
        return Ok(conversation);
    }
}

/// <summary>
/// 创建对话请求
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// 对话标题
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// 创建者ID
    /// </summary>
    public string CreatorId { get; set; }
    
    /// <summary>
    /// 创建者名称
    /// </summary>
    public string CreatorName { get; set; }
    
    /// <summary>
    /// 参与者ID列表
    /// </summary>
    public List<string> ParticipantIds { get; set; } = new();
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// 发送者ID
    /// </summary>
    public string SenderId { get; set; }
    
    /// <summary>
    /// 发送者名称
    /// </summary>
    public string SenderName { get; set; }
}

/// <summary>
/// 添加参与者请求
/// </summary>
public class AddParticipantRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; set; }
}

/// <summary>
/// 私聊对话请求
/// </summary>
public class PrivateConversationRequest
{
    /// <summary>
    /// 用户1 ID
    /// </summary>
    public string UserId1 { get; set; }
    
    /// <summary>
    /// 用户1 名称
    /// </summary>
    public string UserName1 { get; set; }
    
    /// <summary>
    /// 用户2 ID
    /// </summary>
    public string UserId2 { get; set; }
    
    /// <summary>
    /// 用户2 名称
    /// </summary>
    public string UserName2 { get; set; }
} 