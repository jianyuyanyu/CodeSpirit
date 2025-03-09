using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.MessagingApi.Controllers;

/// <summary>
/// 消息控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Module("default", displayName: "默认")]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;

    /// <summary>
    /// 获取用户消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>用户消息列表</returns>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserMessages(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
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

    /// <summary>
    /// 获取未读消息数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读消息数量</returns>
    [HttpGet("user/{userId}/unread/count")]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var count = await _messageService.GetUnreadMessageCountAsync(userId);
        return Ok(new { Count = count });
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
        ArgumentNullException.ThrowIfNull(userId);
        
        var result = await _messageService.MarkAsReadAsync(messageId, userId);
        if (!result)
        {
            return NotFound(new { Message = "消息不存在或不属于该用户" });
        }
        
        return Ok(new { Success = true });
    }

    /// <summary>
    /// 标记所有消息为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("user/{userId}/read/all")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        
        var result = await _messageService.MarkAllAsReadAsync(userId);
        return Ok(new { Success = result });
    }

    /// <summary>
    /// 发送系统通知
    /// </summary>
    /// <param name="notification">通知信息</param>
    /// <returns>创建的消息</returns>
    [HttpPost("system/notify")]
    public async Task<IActionResult> SendSystemNotification([FromBody] SystemNotificationRequest notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(notification.Title);
        ArgumentNullException.ThrowIfNull(notification.Content);
        
        if (notification.RecipientIds != null && notification.RecipientIds.Any())
        {
            var messages = await _messageService.SendSystemNotificationAsync(
                notification.Title, 
                notification.Content, 
                notification.RecipientIds);
            
            return Ok(new { Messages = messages });
        }
        
        if (!string.IsNullOrEmpty(notification.RecipientId))
        {
            var message = await _messageService.SendSystemNotificationAsync(
                notification.Title,
                notification.Content,
                notification.RecipientId);
            
            return Ok(new { Message = message });
        }
        
        return BadRequest(new { Message = "必须指定接收者ID或接收者ID列表" });
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
        ArgumentNullException.ThrowIfNull(userId);
        
        var result = await _messageService.DeleteMessageAsync(messageId, userId);
        if (!result)
        {
            return NotFound(new { Message = "消息不存在或不属于该用户" });
        }
        
        return Ok(new { Success = true });
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