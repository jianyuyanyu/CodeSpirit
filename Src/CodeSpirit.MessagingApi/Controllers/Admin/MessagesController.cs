using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Messaging.Models;
using CodeSpirit.Messaging.Services;
using CodeSpirit.MessagingApi.Dtos.Requests;
using CodeSpirit.MessagingApi.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.MessagingApi.Controllers.Admin;

/// <summary>
/// 消息管理
/// </summary>
[DisplayName("消息管理")]
[Navigation(Icon = "fa-solid fa-envelope")]
public class MessagesController : ApiControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="messageService">消息服务</param>
    /// <param name="mapper">对象映射器</param>
    public MessagesController(IMessageService messageService, IMapper mapper)
    {
        _messageService = messageService;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取消息分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>消息分页列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<MessageDto>>>> GetMessages([FromQuery] MessageQueryDto queryDto)
    {
        var result = await _messageService.GetMessagesAsync(
            queryDto.Type,
            queryDto.Title,
            queryDto.SenderId,
            queryDto.SenderName,
            queryDto.RecipientId,
            queryDto.IsRead,
            queryDto.StartDate,
            queryDto.EndDate,
            queryDto.Page,
            queryDto.PerPage);
            
        List<Message> messages = result.Messages;
        int totalCount = result.TotalCount;

        var messageDtos = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();
        var pageList = new PageList<MessageDto>(messageDtos, totalCount);
        
        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 导出消息列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>导出的消息列表</returns>
    [HttpGet("Export")]
    public async Task<ActionResult<ApiResponse<PageList<MessageDto>>>> Export([FromQuery] MessageQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000;
        int page = 1;
        int perPage = MaxExportLimit;

        var result = await _messageService.GetMessagesAsync(
            queryDto.Type,
            queryDto.Title,
            queryDto.SenderId,
            queryDto.SenderName,
            queryDto.RecipientId,
            queryDto.IsRead,
            queryDto.StartDate,
            queryDto.EndDate,
            page,
            perPage);
            
        List<Message> messages = result.Messages;
        int totalCount = result.TotalCount;

        if (messages.Count == 0)
        {
            return BadResponse<PageList<MessageDto>>("没有数据可供导出");
        }

        var messageDtos = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();
        var pageList = new PageList<MessageDto>(messageDtos, totalCount);
        
        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 获取消息详情
    /// </summary>
    /// <param name="id">消息ID</param>
    /// <returns>消息详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Detail(Guid id)
    {
        var message = await _messageService.GetMessageByIdAsync(id);
        if (message == null)
        {
            return BadResponse<MessageDto>("消息不存在");
        }

        var messageDto = _mapper.Map<MessageDto>(message);
        return SuccessResponse(messageDto);
    }

    /// <summary>
    /// 发送系统通知
    /// </summary>
    /// <param name="request">系统通知请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("SystemNotification")]
    public async Task<ActionResult<ApiResponse>> SendSystemNotification(SystemNotificationRequest request)
    {
        if (string.IsNullOrEmpty(request.RecipientId) && (request.RecipientIds == null || !request.RecipientIds.Any()))
        {
            return BadResponse("接收者ID不能为空");
        }

        if (!string.IsNullOrEmpty(request.RecipientId))
        {
            await _messageService.SendSystemNotificationAsync(request.Title, request.Content, request.RecipientId);
        }
        else
        {
            await _messageService.SendSystemNotificationAsync(request.Title, request.Content, request.RecipientIds);
        }

        return SuccessResponse("系统通知发送成功");
    }

    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="id">消息ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此消息吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteMessage(Guid id)
    {
        bool success = await _messageService.BatchDeleteMessagesAsync(new List<Guid> { id });
        return success
            ? SuccessResponse("消息删除成功")
            : BadResponse("消息删除失败");
    }

    /// <summary>
    /// 批量删除消息
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("Batch/Delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的消息吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete(BatchDeleteMessagesDto request)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return BadResponse("请选择要删除的消息");
        }

        bool success = await _messageService.BatchDeleteMessagesAsync(request.Ids);
        return success
            ? SuccessResponse("批量删除成功")
            : BadResponse("批量删除失败");
    }

    /// <summary>
    /// 标记消息为已读
    /// </summary>
    /// <param name="id">消息ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/MarkAsRead")]
    [Operation("标记为已读", "ajax", null, "确定要将此消息标记为已读吗？", "isRead == false")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid id)
    {
        var message = await _messageService.GetMessageByIdAsync(id);
        if (message == null)
        {
            return BadResponse("消息不存在");
        }

        bool success = await _messageService.MarkAsReadAsync(id, message.RecipientId);
        return success
            ? SuccessResponse("标记已读成功")
            : BadResponse("标记已读失败");
    }

    /// <summary>
    /// 批量标记消息为已读
    /// </summary>
    /// <param name="request">批量标记已读请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("Batch/MarkAsRead")]
    [Operation("批量标记已读", "ajax", null, "确定要将选中的消息标记为已读吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchMarkAsRead(BatchMarkAsReadDto request)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return BadResponse("请选择要标记的消息");
        }

        bool success = await _messageService.BatchMarkAsReadAsync(request.Ids);
        return success
            ? SuccessResponse("批量标记已读成功")
            : BadResponse("批量标记已读失败");
    }
} 