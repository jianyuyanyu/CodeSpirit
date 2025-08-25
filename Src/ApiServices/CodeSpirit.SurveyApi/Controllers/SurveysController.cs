using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷管理控制器
/// </summary>
[DisplayName("问卷管理")]
[Navigation(Icon = "fa-solid fa-poll", PlatformType = PlatformType.Tenant)]
public class SurveysController : ApiControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// 初始化问卷管理控制器
    /// </summary>
    /// <param name="surveyService">问卷服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveysController(
        ISurveyService surveyService,
        ILogger<SurveysController> logger)
    {
        ArgumentNullException.ThrowIfNull(surveyService);
        ArgumentNullException.ThrowIfNull(logger);

        _surveyService = surveyService;
        _logger = logger;
    }

    /// <summary>
    /// 获取问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷列表分页结果</returns>
    [HttpGet]
    [DisplayName("获取问卷列表")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetSurveys([FromQuery] SurveyQueryDto queryDto)
    {
        var surveys = await _surveyService.GetSurveysAsync(queryDto);
        return SuccessResponse(surveys);
    }

    /// <summary>
    /// 获取问卷详情
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取问卷详情")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurvey(int id)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        return SuccessResponse(survey);
    }

    /// <summary>
    /// 创建问卷
    /// </summary>
    /// <param name="createDto">创建问卷DTO</param>
    /// <returns>创建的问卷</returns>
    [HttpPost]
    [DisplayName("创建问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto createDto)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).CreateAsync(createDto);
        return SuccessResponseWithCreate(nameof(GetSurvey), survey);
    }

    /// <summary>
    /// 更新问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <param name="updateDto">更新问卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新问卷")]
    public async Task<ActionResult<ApiResponse>> UpdateSurvey(int id, [FromBody] UpdateSurveyDto updateDto)
    {
        await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).UpdateAsync(id, updateDto);
        return SuccessResponse("问卷更新成功");
    }

    /// <summary>
    /// 删除问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除问卷")]
    public async Task<ActionResult<ApiResponse>> DeleteSurvey(int id)
    {
        await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).DeleteAsync(id);
        return SuccessResponse("问卷删除成功");
    }

    /// <summary>
    /// 发布问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此问卷吗？", "status == 'Draft'")]
    [DisplayName("发布问卷")]
    public async Task<ActionResult<ApiResponse>> PublishSurvey(int id)
    {
        await _surveyService.PublishSurveyAsync(id);
        return SuccessResponse("问卷发布成功");
    }

    /// <summary>
    /// 关闭问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/close")]
    [Operation("关闭", "ajax", null, "确定要关闭此问卷吗？", "status == 'Published'")]
    [DisplayName("关闭问卷")]
    public async Task<ActionResult<ApiResponse>> CloseSurvey(int id)
    {
        await _surveyService.CloseSurveyAsync(id);
        return SuccessResponse("问卷关闭成功");
    }

    /// <summary>
    /// 归档问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/archive")]
    [Operation("归档", "ajax", null, "确定要归档此问卷吗？", "status != 'Archived'")]
    [DisplayName("归档问卷")]
    public async Task<ActionResult<ApiResponse>> ArchiveSurvey(int id)
    {
        await _surveyService.ArchiveSurveyAsync(id);
        return SuccessResponse("问卷归档成功");
    }

    /// <summary>
    /// 复制问卷
    /// </summary>
    /// <param name="request">复制请求</param>
    /// <returns>复制的问卷</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "form")]
    [DisplayName("复制问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CopySurvey([FromRoute] int id, [FromBody] CopySurveyRequest request)
    {
        var survey = await _surveyService.CopySurveyAsync(id, request.Title);
        return SuccessResponse(survey);
    }

    /// <summary>
    /// 获取问卷统计信息
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>统计信息</returns>
    [HttpGet("{id}/statistics")]
    [DisplayName("获取统计信息")]
    public async Task<ActionResult<ApiResponse<SurveyStatisticsDto>>> GetSurveyStatistics(int id)
    {
        var statistics = await _surveyService.GetSurveyStatisticsAsync(id);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 获取我的问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>我的问卷列表</returns>
    [HttpGet("my")]
    [DisplayName("我的问卷")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetMySurveys([FromQuery] SurveyQueryDto queryDto)
    {
        var surveys = await _surveyService.GetMySurveysAsync(queryDto);
        return SuccessResponse(surveys);
    }

    /// <summary>
    /// 获取问卷模板列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>模板列表</returns>
    [HttpGet("templates")]
    [DisplayName("问卷模板")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetSurveyTemplates([FromQuery] SurveyQueryDto queryDto)
    {
        var templates = await _surveyService.GetSurveyTemplatesAsync(queryDto);
        return SuccessResponse(templates);
    }

    /// <summary>
    /// 从模板创建问卷
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>创建的问卷</returns>
    [HttpPost("templates/{templateId}/create")]
    [Operation("从模板创建", "form")]
    [DisplayName("从模板创建问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateFromTemplate(int templateId, [FromBody] CreateFromTemplateRequest request)
    {
        var survey = await _surveyService.CreateFromTemplateAsync(templateId, request.Title);
        return SuccessResponse(survey);
    }
}

/// <summary>
/// 复制问卷请求
/// </summary>
public class CopySurveyRequest
{
    /// <summary>
    /// 新问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 从模板创建问卷请求
/// </summary>
public class CreateFromTemplateRequest
{
    /// <summary>
    /// 新问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;
}
