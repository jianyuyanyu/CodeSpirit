using AutoMapper;
using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.PublishHistory;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 配置发布历史控制器
/// </summary>
[DisplayName("发布历史")]
[Navigation(Icon = "fa-solid fa-clock-rotate-left", PlatformType = PlatformType.Inherit)]
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
    [DisplayName("获取发布历史列表")]
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
    [DisplayName("获取发布历史详情")]
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
    [Operation(
        "发布对比", 
        OperationActionType.ReturnForm,
        null, 
        null, 
        null, 
        Icon = "fa fa-code-compare", 
        DialogSize = DialogSize.Full,
        Data = "{\"id\": \"${id}\"}",
        Actions = @"[
            {
                ""type"": ""button"",
                ""label"": ""回滚到此版本"",
                ""level"": ""warning"",
                ""icon"": ""fa fa-rotate-left"",
                ""actionType"": ""ajax"",
                ""api"": {
                    ""method"": ""post"",
                    ""url"": ""/config/api/config/ConfigPublishHistories/${id}/rollback""
                },
                ""confirmText"": ""确定要回滚到此版本吗？<br /><strong>注意：回滚操作仅将配置恢复为草稿状态，不会自动发布。<br />您需要进入配置管理界面手动发布配置后才能生效。</strong>"",
                ""reload"": ""window"",
                ""close"": true
            },
            {
                ""type"": ""button"",
                ""label"": ""关闭"",
                ""actionType"": ""close""
            }
        ]"
    )]
    public async Task<ActionResult<ApiResponse<ConfigPublishHistoryCompareDto>>> GetCompare(int id)
    {
        var result = await _publishHistoryService.GetPublishHistoryCompareAsync(id);
        return SuccessResponse(result);
    }
} 