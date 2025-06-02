using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 练习管理
/// </summary>
[DisplayName("练习管理")]
[Navigation(Icon = "fa-solid fa-dumbbell", PlatformType = PlatformType.Tenant)]
public class PracticeSettingsController : ApiControllerBase
{
    private readonly IPracticeSettingService _practiceSettingService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="practiceSettingService">练习服务</param>
    public PracticeSettingsController(IPracticeSettingService practiceSettingService)
    {
        _practiceSettingService = practiceSettingService;
    }

    /// <summary>
    /// 获取练习分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习分页列表</returns>
    [HttpGet]
    [DisplayName("获取练习列表")]
    public async Task<ActionResult<ApiResponse<PageList<PracticeSettingDto>>>> GetPracticeSettings([FromQuery] PracticeSettingQueryDto queryDto)
    {
        var result = await _practiceSettingService.GetPracticeSettingsAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取练习详情
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>练习详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取练习详情")]
    public async Task<ActionResult<ApiResponse<PracticeSettingDto>>> GetPracticeSetting(long id)
    {
        var result = await _practiceSettingService.GetAsync(id);
        if (result == null)
        {
            return NotFound("练习不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建练习
    /// </summary>
    /// <param name="createDto">创建练习DTO</param>
    /// <returns>创建的练习</returns>
    [HttpPost]
    [DisplayName("创建练习")]
    public async Task<ActionResult<ApiResponse<PracticeSettingDto>>> CreatePracticeSetting(CreatePracticeSettingDto createDto)
    {
        var result = await _practiceSettingService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新练习
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <param name="updateDto">更新练习DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [DisplayName("编辑")]
    public async Task<ActionResult<ApiResponse>> UpdatePracticeSetting(long id, UpdatePracticeSettingDto updateDto)
    {
        await _practiceSettingService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除练习
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此练习吗？")]
    [DisplayName("删除练习")]
    public async Task<ActionResult<ApiResponse>> DeletePracticeSetting(long id)
    {
        await _practiceSettingService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 发布练习
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此练习吗？", visibleOn: "status == 1")]
    [DisplayName("发布练习")]
    public async Task<ActionResult<ApiResponse>> PublishPracticeSetting(long id)
    {
        await _practiceSettingService.PublishPracticeSettingAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 禁用练习
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/disable")]
    [Operation("禁用", "ajax", null, "确定要禁用此练习吗？", visibleOn: "status == 2")]
    [DisplayName("禁用练习")]
    public async Task<ActionResult<ApiResponse>> DisablePracticeSetting(long id)
    {
        await _practiceSettingService.DisablePracticeSettingAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 启用练习
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/enable")]
    [Operation("启用", "ajax", null, "确定要启用此练习吗？", visibleOn: "status == 3")]
    [DisplayName("启用练习")]
    public async Task<ActionResult<ApiResponse>> EnablePracticeSetting(long id)
    {
        await _practiceSettingService.EnablePracticeSettingAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取指定试卷的所有练习
    /// </summary>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>练习列表</returns>
    [HttpGet("by-exam-paper/{examPaperId}")]
    [DisplayName("获取试卷的练习列表")]
    public async Task<ActionResult<ApiResponse<List<PracticeSettingDto>>>> GetPracticeSettingsByExamPaper(long examPaperId)
    {
        var results = await _practiceSettingService.GetPracticeSettingsByExamPaperIdAsync(examPaperId);
        return SuccessResponse(results);
    }

    /// <summary>
    /// 批量删除练习
    /// </summary>
    /// <param name="ids">练习ID列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的练习吗？", isBulkOperation: true)]
    [DisplayName("批量删除练习")]
    public async Task<ActionResult<ApiResponse>> BatchDeletePracticeSettings([FromBody] IEnumerable<long> ids)
    {
        var result = await _practiceSettingService.BatchDeleteAsync(ids);
        return SuccessResponse($"成功删除{result.successCount}个练习，失败{result.failedIds.Count}个");
    }
} 