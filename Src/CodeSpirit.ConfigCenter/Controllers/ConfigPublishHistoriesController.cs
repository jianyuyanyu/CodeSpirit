using AutoMapper;
using CodeSpirit.ConfigCenter.Constants;
using CodeSpirit.ConfigCenter.Dtos.Config;
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
[DisplayName("配置发布历史")]
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
        try
        {
            var histories = await _publishHistoryService.GetPublishHistoryListAsync(queryDto);
            
            // 创建DTO分页列表
            var dtoItems = _mapper.Map<List<ConfigPublishHistoryDto>>(histories.Items);
            var result = new PageList<ConfigPublishHistoryDto>(dtoItems, histories.Total);
            
            return SuccessResponse(result);
        }
        catch (AppServiceException ex)
        {
            return BadResponse<PageList<ConfigPublishHistoryDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发布历史列表失败: {AppId}/{Environment}", queryDto.AppId, queryDto.Environment);
            return BadResponse<PageList<ConfigPublishHistoryDto>>("获取发布历史列表失败");
        }
    }

    /// <summary>
    /// 获取发布历史详情
    /// </summary>
    /// <param name="id">发布历史ID</param>
    /// <returns>发布历史详情</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ConfigPublishHistoryDto>>> GetPublishHistoryDetail(int id)
    {
        try
        {
            var history = await _publishHistoryService.GetPublishHistoryDetailAsync(id);
            var historyDto = _mapper.Map<ConfigPublishHistoryDto>(history);
            
            return SuccessResponse(historyDto);
        }
        catch (AppServiceException ex)
        {
            return BadResponse<ConfigPublishHistoryDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发布历史详情失败: ID={Id}", id);
            return BadResponse<ConfigPublishHistoryDto>("获取发布历史详情失败");
        }
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
        try
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
        catch (AppServiceException ex)
        {
            return BadResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚发布历史失败: ID={Id}", id);
            return BadResponse("回滚发布历史失败");
        }
    }
} 