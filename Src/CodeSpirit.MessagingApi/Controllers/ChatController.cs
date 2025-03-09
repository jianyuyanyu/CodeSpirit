using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.MessagingApi.Controllers;

/// <summary>
/// 聊天控制器
/// </summary>
[DisplayName("聊天")]
[Module("default")]
public class ChatController : ApiControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    /// <summary>
    /// 初始化聊天控制器
    /// </summary>
    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        ArgumentNullException.ThrowIfNull(chatService);
        ArgumentNullException.ThrowIfNull(logger);

        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>会话列表</returns>
    [HttpGet("user/{userId}/conversations")]
    public async Task<IActionResult> GetUserConversations(string userId)
    {
        try
        {
            var conversations = await _chatService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户会话失败: {UserId}", userId);
            return BadRequest(new { message = "获取用户会话失败" });
        }
    }

    /// <summary>
    /// 获取指定会话详情
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>会话详情</returns>
    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        try
        {
            var conversation = await _chatService.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                return NotFound(new { message = "会话不存在" });
            }

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会话详情失败: {ConversationId}", conversationId);
            return BadRequest(new { message = "获取会话详情失败" });
        }
    }

    /// <summary>
    /// 获取会话消息列表
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>消息列表</returns>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetConversationMessages(Guid conversationId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会话消息失败: {ConversationId}", conversationId);
            return BadRequest(new { message = "获取会话消息失败" });
        }
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    /// <param name="request">创建会话请求</param>
    /// <returns>创建的会话</returns>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            var conversation = await _chatService.CreateConversationAsync(
                request.Title,
                request.CreatorId,
                request.CreatorName,
                request.ParticipantIds);

            return CreatedAtAction(nameof(GetConversation), new { conversationId = conversation.Id }, conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建会话失败");
            return BadRequest(new { message = "创建会话失败" });
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>发送的消息</returns>
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var message = await _chatService.SendMessageAsync(
                conversationId,
                request.Content,
                request.SenderId,
                request.SenderName);

            return Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败: {ConversationId}", conversationId);
            return BadRequest(new { message = "发送消息失败" });
        }
    }

    /// <summary>
    /// 添加参与者到会话
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">添加参与者请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("conversations/{conversationId}/participants")]
    public async Task<IActionResult> AddParticipant(Guid conversationId, [FromBody] AddParticipantRequest request)
    {
        try
        {
            var result = await _chatService.AddParticipantAsync(
                conversationId,
                request.UserId,
                request.UserName);

            if (!result)
            {
                return BadRequest(new { message = "添加参与者失败" });
            }

            return Ok(new { message = "添加参与者成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加参与者失败: {ConversationId}, {UserId}", conversationId, request.UserId);
            return BadRequest(new { message = "添加参与者失败" });
        }
    }

    /// <summary>
    /// 从会话中移除参与者
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("conversations/{conversationId}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(Guid conversationId, string userId)
    {
        try
        {
            var result = await _chatService.RemoveParticipantAsync(conversationId, userId);
            if (!result)
            {
                return BadRequest(new { message = "移除参与者失败" });
            }

            return Ok(new { message = "移除参与者成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除参与者失败: {ConversationId}, {UserId}", conversationId, userId);
            return BadRequest(new { message = "移除参与者失败" });
        }
    }

    /// <summary>
    /// 标记会话为已读
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid conversationId, [FromBody] string userId)
    {
        try
        {
            var result = await _chatService.MarkConversationAsReadAsync(conversationId, userId);
            if (!result)
            {
                return BadRequest(new { message = "标记会话为已读失败" });
            }

            return Ok(new { message = "标记会话为已读成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记会话为已读失败: {ConversationId}, {UserId}", conversationId, userId);
            return BadRequest(new { message = "标记会话为已读失败" });
        }
    }

    /// <summary>
    /// 获取或创建私聊会话
    /// </summary>
    /// <param name="request">私聊会话请求</param>
    /// <returns>会话详情</returns>
    [HttpPost("private")]
    public async Task<IActionResult> GetOrCreatePrivateConversation([FromBody] PrivateConversationRequest request)
    {
        try
        {
            var conversation = await _chatService.GetOrCreatePrivateConversationAsync(
                request.UserId1,
                request.UserName1,
                request.UserId2,
                request.UserName2);

            return Ok(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或创建私聊会话失败");
            return BadRequest(new { message = "获取或创建私聊会话失败" });
        }
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