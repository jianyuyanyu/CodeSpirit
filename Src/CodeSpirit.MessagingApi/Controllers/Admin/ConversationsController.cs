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
/// 会话管理
/// </summary>
[DisplayName("会话管理")]
[Navigation(Icon = "fa-solid fa-comments")]
public class ConversationsController : ApiControllerBase
{
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="chatService">聊天服务</param>
    /// <param name="mapper">对象映射器</param>
    public ConversationsController(IChatService chatService, IMapper mapper)
    {
        _chatService = chatService;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取会话分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>会话分页列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ConversationDto>>>> GetConversations([FromQuery] ConversationQueryDto queryDto)
    {
        var result = await _chatService.GetConversationsAsync(
            queryDto.Title,
            queryDto.ParticipantId,
            queryDto.StartDate,
            queryDto.EndDate,
            queryDto.Page,
            queryDto.PerPage);
            
        List<Conversation> conversations = result.Conversations;
        int totalCount = result.TotalCount;

        var conversationDtos = conversations.Select(c => _mapper.Map<ConversationDto>(c)).ToList();
        var pageList = new PageList<ConversationDto>(conversationDtos, totalCount);
        
        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 导出会话列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>导出的会话列表</returns>
    [HttpGet("Export")]
    public async Task<ActionResult<ApiResponse<PageList<ConversationDto>>>> Export([FromQuery] ConversationQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000;
        int page = 1;
        int perPage = MaxExportLimit;

        var result = await _chatService.GetConversationsAsync(
            queryDto.Title,
            queryDto.ParticipantId,
            queryDto.StartDate,
            queryDto.EndDate,
            page,
            perPage);
            
        List<Conversation> conversations = result.Conversations;
        int totalCount = result.TotalCount;

        if (conversations.Count == 0)
        {
            return BadResponse<PageList<ConversationDto>>("没有数据可供导出");
        }

        var conversationDtos = conversations.Select(c => _mapper.Map<ConversationDto>(c)).ToList();
        var pageList = new PageList<ConversationDto>(conversationDtos, totalCount);
        
        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 获取会话详情
    /// </summary>
    /// <param name="id">会话ID</param>
    /// <returns>会话详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Detail(Guid id)
    {
        var conversation = await _chatService.GetConversationByIdAsync(id);
        if (conversation == null)
        {
            return BadResponse<ConversationDto>("会话不存在");
        }

        var conversationDto = _mapper.Map<ConversationDto>(conversation);
        return SuccessResponse(conversationDto);
    }

    /// <summary>
    /// 获取会话消息
    /// </summary>
    /// <param name="id">会话ID</param>
    /// <param name="page">页码</param>
    /// <param name="perPage">每页条数</param>
    /// <returns>会话消息列表</returns>
    [HttpGet("{id}/Messages")]
    public async Task<ActionResult<ApiResponse<PageList<MessageDto>>>> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        var result = await _chatService.GetConversationMessagesAsync(id, page, perPage);
        
        List<Message> messages = result.Messages;
        int totalCount = result.TotalCount;

        var messageDtos = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();
        var pageList = new PageList<MessageDto>(messageDtos, totalCount);
        
        return SuccessResponse(pageList);
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="id">会话ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此会话吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteConversation(Guid id)
    {
        bool success = await _chatService.BatchDeleteConversationsAsync(new List<Guid> { id });
        return success
            ? SuccessResponse("会话删除成功")
            : BadResponse("会话删除失败");
    }

    /// <summary>
    /// 批量删除会话
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("Batch/Delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的会话吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchDeleteMessagesDto request)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return BadResponse("请选择要删除的会话");
        }

        bool success = await _chatService.BatchDeleteConversationsAsync(request.Ids);
        return success
            ? SuccessResponse("批量删除成功")
            : BadResponse("批量删除失败");
    }
} 