using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试设置管理
/// </summary>
[DisplayName("考试管理")]
[Navigation(Icon = "fa-solid fa-calendar-check")]
public class ExamSettingsController : ApiControllerBase
{
    private readonly IExamSettingService _examSettingService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamSettingsController(IExamSettingService examSettingService)
    {
        _examSettingService = examSettingService;
    }

    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考试设置分页列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ExamSettingDto>>>> GetExamSettings([FromQuery] ExamSettingQueryDto queryDto)
    {
        var result = await _examSettingService.GetExamSettingsAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>考试设置详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExamSettingDto>>> GetExamSetting(long id)
    {
        var result = await _examSettingService.GetExamSettingDetailAsync(id);
        if (result == null)
        {
            return NotFound("考试设置不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建考试设置
    /// </summary>
    /// <param name="createDto">创建考试设置DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExamSettingDto>>> CreateExamSetting([FromBody] CreateExamSettingDto createDto)
    {
        var result = await _examSettingService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <param name="updateDto">更新考试设置DTO</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateExamSetting(long id, [FromBody] UpdateExamSettingDto updateDto)
    {
        await _examSettingService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteExamSetting(long id)
    {
        await _examSettingService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>发布结果</returns>
    [HttpPost("{id}/publish")]
    public async Task<ActionResult<ApiResponse>> PublishExamSetting(long id)
    {
        await _examSettingService.PublishExamSettingAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 取消发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>取消发布结果</returns>
    [HttpPost("{id}/unpublish")]
    public async Task<ActionResult<ApiResponse>> UnpublishExamSetting(long id)
    {
        await _examSettingService.UnpublishExamSettingAsync(id);
        return SuccessResponse();
    }
} 