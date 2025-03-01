using AutoMapper;
using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.PublishHistory;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 配置发布历史控制器
/// </summary>
[DisplayName("发布历史")]
[Navigation(Icon = "fa-solid fa-clock-rotate-left")]
public class ConfigPublishHistoriesController : ApiControllerBase
{
    private readonly IConfigPublishHistoryService _publishHistoryService;
    private readonly IMapper _mapper;
    private readonly ILogger<ConfigPublishHistoriesController> _logger;

    /// <summary>
    /// 初始化配置发布历史控制器
    /// </summary>
    public ConfigPublishHistoriesController(
        IConfigPublishHistoryService publishHistoryService,
        IMapper mapper,
        ILogger<ConfigPublishHistoriesController> logger)
    {
        _publishHistoryService = publishHistoryService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用配置发布历史列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>发布历史列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ConfigPublishHistoryDto>>>> GetPublishHistories(
        [FromQuery] ConfigPublishHistoryQueryDto queryDto)
    {
        var histories = await _publishHistoryService.GetPublishHistoryListAsync(queryDto);

        // 创建DTO分页列表
        var dtoItems = _mapper.Map<List<ConfigPublishHistoryDto>>(histories.Items);
        var result = new PageList<ConfigPublishHistoryDto>(dtoItems, histories.Total);

        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    /// <param name="id">发布历史ID</param>
    /// <returns>发布历史详情</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ConfigPublishHistoryDto>>> GetPublishHistoryDetail(int id)
    {
        var history = await _publishHistoryService.GetPublishHistoryDetailAsync(id);
        return SuccessResponse(history);
    }

    /// <summary>
    /// 回滚到指定的发布历史版本
    /// </summary>
    /// <param name="id">发布历史ID</param>
    /// <returns>回滚结果</returns>
    [HttpPost("{id:int}/rollback")]
    [Operation("回滚", "ajax", null, "确定要回滚到此版本吗？")]
    public async Task<ActionResult<ApiResponse>> RollbackToHistory(int id)
    {
        var (success, message) = await _publishHistoryService.RollbackToHistoryAsync(id);

        if (success)
        {
            return SuccessResponse(message);
        }
        else
        {
            return BadResponse(message);
        }
    }

    /// <summary>
    /// 获取配置发布历史对比
    /// </summary>
    /// <param name="id">发布历史ID</param>
    /// <returns>配置对比结果</returns>
    [HttpGet("{id}/compare")]
    [Operation(label: "发布对比", actionType: "return-form", null)]
    public async Task<ActionResult<ApiResponse<ConfigPublishHistoryCompareDto>>> GetCompare(int id)
    {
        var result = await _publishHistoryService.GetPublishHistoryCompareAsync(id);
        return SuccessResponse(result);
    }
} 