using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using CodeSpirit.ExamApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;

using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试设置管理
/// </summary>
[DisplayName("考试管理")]
[Navigation(Icon = "fa-solid fa-calendar-check", PlatformType = PlatformType.Tenant)]
public class ExamSettingsController : ApiControllerBase
{
    private readonly IExamSettingService _examSettingService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ExamSettingsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamSettingsController(
        IExamSettingService examSettingService,
        ICurrentUser currentUser,
        ILogger<ExamSettingsController> logger)
    {
        _examSettingService = examSettingService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考试设置分页列表</returns>
    [HttpGet]
    [DisplayName("获取考试设置列表")]
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
    [DisplayName("获取考试设置详情")]
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
    /// 获取已发布考试列表用于选择
    /// </summary>
    /// <returns>已发布考试列表</returns>
    [HttpGet("select-published")]
    [DisplayName("获取已发布考试列表")]
    public async Task<ActionResult<ApiResponse<List<OptionDto<long>>>>> GetPublishedExamSettingsForSelect()
    {
        // 创建查询对象，只筛选已发布状态的考试
        var queryDto = new ExamSettingQueryDto
        {
            Page = 1,
            PerPage = int.MaxValue // 获取所有已发布考试
        };

        var result = await _examSettingService.GetExamSettingsAsync(queryDto);

        // 筛选已发布状态的考试，并转换为OptionDto格式
        var publishedExams = result.Items
            .Where(e => e.Status == ExamSettingStatus.Published)
            .Select(e => new OptionDto<long>
            {
                Id = e.Id,
                Name = e.Name
            })
            .ToList();

        return SuccessResponse(publishedExams);
    }

    /// <summary>
    /// 创建考试设置
    /// </summary>
    /// <param name="createDto">创建考试设置DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建考试设置")]
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
    [DisplayName("更新考试设置")]
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
    [DisplayName("删除考试设置")]
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
    [Operation("发布", "ajax", null, "确定要发布此试卷吗？", visibleOn: "status == 0")]
    [DisplayName("发布考试设置")]
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
    [Operation("取消发布", "ajax", null, "确定要取消发布此试卷吗？", visibleOn: "status == 1")]
    [DisplayName("取消发布考试设置")]
    public async Task<ActionResult<ApiResponse>> UnpublishExamSetting(long id)
    {
        await _examSettingService.UnpublishExamSettingAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 跳转到监考大屏
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>跳转响应</returns>
    [HttpGet("{id}/monitor-dashboard")]
    [Operation("监考大屏", "ajax", null, "确定要打开监考大屏吗？", visibleOn: "status == 1")]
    [DisplayName("监考大屏")]
    public async Task<ActionResult<ApiResponse>> Go_Monitor_dashboard(long id)
    {
        try
        {
            // 验证考试设置是否存在
            var examSetting = await _examSettingService.GetAsync(id);
            if (examSetting == null)
            {
                return BadRequest(ApiResponse.Error(1, "考试设置不存在"));
            }

            // 从当前用户获取租户ID（安全方式）
            var tenantId = _currentUser.TenantId;
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(ApiResponse.Error(1, "无法获取当前租户信息"));
            }

            // 构建监考大屏URL
            var monitorUrl = $"/{tenantId}/exam/monitor2/{id}";

            // 返回跳转响应
            return Ok(ApiResponse.SuccessWithRedirect(
                url: monitorUrl,
                msg: "正在跳转到监考大屏...",
                redirectType: RedirectType.Blank,
                delay: 1000
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "监考大屏跳转失败，考试ID: {ExamId}", id);
            return BadRequest(ApiResponse.Error(1, "跳转失败，请重试"));
        }
    }
}