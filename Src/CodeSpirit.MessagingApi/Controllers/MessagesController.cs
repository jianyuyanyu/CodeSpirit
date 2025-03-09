using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.MessagingApi.Controllers;

/// <summary>
/// 消息控制器
/// </summary>
[DisplayName("消息")]
[Module("default")]
public class MessagesController : ApiControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;

    /// <summary>
    /// 初始化消息控制器
    /// </summary>
    public MessagesController(
        IMessageService messageService,
        ILogger<MessagesController> logger)
    {
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(logger);

        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户消息列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>消息列表及分页信息</returns>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserMessages(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var (messages, totalCount) = await _messageService.GetUserMessagesAsync(userId, pageNumber, pageSize);

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
            _logger.LogError(ex, "获取用户消息失败: {UserId}", userId);
            return BadRequest(new { message = "获取用户消息失败" });
        }
    }

    /// <summary>
    /// 获取用户未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    [HttpGet("user/{userId}/unread/count")]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        try
        {
            var count = await _messageService.GetUnreadMessageCountAsync(userId);
            return Ok(new { UnreadCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取未读消息数量失败: {UserId}", userId);
            return BadRequest(new { message = "获取未读消息数量失败" });
        }
    }

    /// <summary>
    /// 标记消息为已读
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid messageId, [FromBody] string userId)
    {
        try
        {
            var result = await _messageService.MarkAsReadAsync(messageId, userId);
            if (!result)
            {
                return BadRequest(new { message = "标记消息已读失败" });
            }

            return Ok(new { message = "已成功标记消息为已读" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记消息已读失败: {MessageId}, {UserId}", messageId, userId);
            return BadRequest(new { message = "标记消息已读失败" });
        }
    }

    /// <summary>
    /// 标记所有消息为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("user/{userId}/read/all")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        try
        {
            var result = await _messageService.MarkAllAsReadAsync(userId);
            if (!result)
            {
                return BadRequest(new { message = "标记所有消息已读失败" });
            }

            return Ok(new { message = "已成功标记所有消息为已读" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记所有消息已读失败: {UserId}", userId);
            return BadRequest(new { message = "标记所有消息已读失败" });
        }
    }

    /// <summary>
    /// 发送系统通知
    /// </summary>
    /// <param name="notification">系统通知请求</param>
    /// <returns>发送结果</returns>
    [HttpPost("system/notify")]
    public async Task<IActionResult> SendSystemNotification([FromBody] SystemNotificationRequest notification)
    {
        try
        {
            if (notification.RecipientId == null &&
                (notification.RecipientIds == null || notification.RecipientIds.Count == 0))
            {
                return BadRequest(new { message = "必须指定至少一个接收者" });
            }

            // 单个接收者
            if (!string.IsNullOrEmpty(notification.RecipientId))
            {
                var message = await _messageService.SendSystemNotificationAsync(
                    notification.Title,
                    notification.Content,
                    notification.RecipientId);

                return Ok(new { message = "系统通知发送成功", messageId = message.Id });
            }
            // 多个接收者
            else
            {
                var messages = await _messageService.SendSystemNotificationAsync(
                    notification.Title,
                    notification.Content,
                    notification.RecipientIds);

                return Ok(new
                {
                    message = "系统通知发送成功",
                    messageCount = messages.Count,
                    messageIds = messages.Select(m => m.Id)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送系统通知失败");
            return BadRequest(new { message = "发送系统通知失败" });
        }
    }

    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId, [FromQuery] string userId)
    {
        try
        {
            var result = await _messageService.DeleteMessageAsync(messageId, userId);
            if (!result)
            {
                return BadRequest(new { message = "删除消息失败" });
            }

            return Ok(new { message = "消息删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除消息失败: {MessageId}, {UserId}", messageId, userId);
            return BadRequest(new { message = "删除消息失败" });
        }
    }
}

/// <summary>
/// 系统通知请求模型
/// </summary>
public class SystemNotificationRequest
{
    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 单个接收者ID
    /// </summary>
    public string RecipientId { get; set; }

    /// <summary>
    /// 多个接收者ID列表
    /// </summary>
    public List<string> RecipientIds { get; set; }
}