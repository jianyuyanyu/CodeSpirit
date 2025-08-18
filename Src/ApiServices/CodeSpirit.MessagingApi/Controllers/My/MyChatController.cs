using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.MessagingApi.Controllers.Default;

/// <summary>
/// 聊天控制器
/// </summary>
[DisplayName("聊天")]
[Module("default")]
[Route("api/messaging/chat/my")]
public class MyChatController : ApiControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<MyChatController> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 初始化聊天控制器
    /// </summary>
    public MyChatController(
        IChatService chatService,
        ILogger<MyChatController> logger,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(chatService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(currentUser);

        _chatService = chatService;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取当前用户所有会话
    /// </summary>
    /// <returns>会话列表</returns>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        try
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var userId = _currentUser.Id.ToString();
            var conversations = await _chatService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户会话失败");
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
    /// 创建新会话（使用当前用户作为创建者）
    /// </summary>
    /// <param name="request">创建会话请求</param>
    /// <returns>创建的会话</returns>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateMyConversation([FromBody] CreateMyConversationRequest request)
    {
        try
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var userId = _currentUser.Id.ToString();
            var userName = _currentUser.UserName;

            var conversation = await _chatService.CreateConversationAsync(
                request.Title,
                userId,
                userName,
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
    /// 当前用户发送消息
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>发送的消息</returns>
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMyMessage(Guid conversationId, [FromBody] SendMyMessageRequest request)
    {
        try
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var userId = _currentUser.Id.ToString();
            var userName = _currentUser.UserName;

            var message = await _chatService.SendMessageAsync(
                conversationId,
                request.Content,
                userId,
                userName);

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
    /// 标记当前用户的会话为已读
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkMyConversationAsRead(Guid conversationId)
    {
        try
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var userId = _currentUser.Id.ToString();
            var result = await _chatService.MarkConversationAsReadAsync(conversationId, userId);
            if (!result)
            {
                return BadRequest(new { message = "标记会话为已读失败" });
            }

            return Ok(new { message = "标记会话为已读成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记会话为已读失败: {ConversationId}", conversationId);
            return BadRequest(new { message = "标记会话为已读失败" });
        }
    }

    /// <summary>
    /// 当前用户创建私聊会话
    /// </summary>
    /// <param name="request">私聊会话请求</param>
    /// <returns>会话详情</returns>
    [HttpPost("my/private")]
    public async Task<IActionResult> GetOrCreateMyPrivateConversation([FromBody] MyPrivateConversationRequest request)
    {
        try
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var userId = _currentUser.Id.ToString();
            var userName = _currentUser.UserName;

            var conversation = await _chatService.GetOrCreatePrivateConversationAsync(
                userId,
                userName,
                request.OtherUserId,
                request.OtherUserName);

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
/// 创建当前用户的对话请求
/// </summary>
public class CreateMyConversationRequest
{
    /// <summary>
    /// 对话标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 参与者ID列表
    /// </summary>
    public List<string> ParticipantIds { get; set; } = new();
}

/// <summary>
/// 当前用户发送消息请求
/// </summary>
public class SendMyMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; }
}

/// <summary>
/// 当前用户私聊对话请求
/// </summary>
public class MyPrivateConversationRequest
{
    /// <summary>
    /// 对方用户ID
    /// </summary>
    public string OtherUserId { get; set; }

    /// <summary>
    /// 对方用户名称
    /// </summary>
    public string OtherUserName { get; set; }
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