using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目版本管理控制器
/// </summary>
[DisplayName("题目版本管理")]
[Navigation(Icon = "fa-solid fa-code-branch")]
public class QuestionVersionsController : ApiControllerBase
{
    private readonly IQuestionVersionService _questionVersionService;
    private readonly ILogger<QuestionVersionsController> _logger;

    /// <summary>
    /// 初始化题目版本管理控制器
    /// </summary>
    /// <param name="questionVersionService">题目版本服务</param>
    /// <param name="logger">日志记录器</param>
    public QuestionVersionsController(
        IQuestionVersionService questionVersionService,
        ILogger<QuestionVersionsController> logger)
    {
        ArgumentNullException.ThrowIfNull(questionVersionService);
        ArgumentNullException.ThrowIfNull(logger);

        _questionVersionService = questionVersionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目版本列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目版本列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<QuestionVersionDto>>>> GetQuestionVersions([FromQuery] QuestionVersionQueryDto queryDto)
    {
        PageList<QuestionVersionDto> questionVersions = await _questionVersionService.GetQuestionVersionsAsync(queryDto);
        return SuccessResponse(questionVersions);
    }

    /// <summary>
    /// 获取题目的所有版本
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>题目的所有版本列表</returns>
    [HttpGet("questions/{questionId:long}/versions")]
    public async Task<ActionResult<ApiResponse<List<QuestionVersionDto>>>> GetVersionsByQuestionId(long questionId)
    {
        if (questionId <= 0)
        {
            return BadRequest("无效的题目ID");
        }
        
        List<QuestionVersionDto> versions = await _questionVersionService.GetVersionsByQuestionIdAsync(questionId);
        return SuccessResponse(versions);
    }

    /// <summary>
    /// 获取题目的特定版本
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="version">版本号</param>
    /// <returns>题目版本详情</returns>
    [HttpGet("questions/{questionId:long}/versions/{version:int}")]
    public async Task<ActionResult<ApiResponse<QuestionVersionDto>>> GetQuestionVersionByVersion(long questionId, int version)
    {
        if (questionId <= 0)
        {
            return BadRequest("无效的题目ID");
        }
        
        if (version <= 0)
        {
            return BadRequest("无效的版本号");
        }
        
        QuestionVersionDto questionVersion = await _questionVersionService.GetQuestionVersionAsync(questionId, version);
        return SuccessResponse(questionVersion);
    }

    /// <summary>
    /// 获取题目版本详情
    /// </summary>
    /// <param name="id">题目版本ID</param>
    /// <returns>题目版本详细信息</returns>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<QuestionVersionDto>>> GetQuestionDetail(long id)
    {
        QuestionVersionDto questionVersion = await _questionVersionService.GetAsync(id);
        return SuccessResponse(questionVersion);
    }

    /// <summary>
    /// 创建题目版本
    /// </summary>
    /// <param name="createDto">创建题目版本请求数据</param>
    /// <returns>创建的题目版本信息</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuestionVersionDto>>> CreateQuestionVersion(CreateQuestionVersionDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        QuestionVersionDto questionVersionDto = await _questionVersionService.CreateNewVersionAsync(createDto);
        return SuccessResponse(questionVersionDto);
    }

    /// <summary>
    /// 更新题目版本
    /// </summary>
    /// <param name="id">题目版本ID</param>
    /// <param name="updateDto">更新题目版本请求数据</param>
    /// <returns>更新操作结果</returns>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestionVersion(long id, [FromBody]UpdateQuestionVersionDto updateDto)
    {
        await _questionVersionService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除题目版本
    /// </summary>
    /// <param name="id">题目版本ID</param>
    /// <returns>删除操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此题目版本吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteQuestionVersion(long id)
    {
        await _questionVersionService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除题目版本
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>批量删除操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的题目版本吗？", IsBulkOperation = true)]
    public async Task<ActionResult<ApiResponse>> BatchDeleteQuestionVersions([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        (int successCount, List<long> failedIds) = await _questionVersionService.BatchDeleteAsync(request.Ids);
        
        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个题目版本，但以下题目版本删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个题目版本！");
    }
} 