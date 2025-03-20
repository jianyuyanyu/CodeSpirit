using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目管理控制器
/// </summary>
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-book")]
public class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    /// <summary>
    /// 初始化题目管理控制器
    /// </summary>
    /// <param name="questionService">题目服务</param>
    /// <param name="logger">日志记录器</param>
    public QuestionsController(
        IQuestionService questionService,
        ILogger<QuestionsController> logger)
    {
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(logger);

        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目列表分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<QuestionDto>>>> GetQuestions([FromQuery] QuestionQueryDto queryDto)
    {
        PageList<QuestionDto> questions = await _questionService.GetQuestionsAsync(queryDto);
        return SuccessResponse(questions);
    }

    /// <summary>
    /// 获取题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目详细信息</returns>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> GetQuestion(long id)
    {
        QuestionDto question = await _questionService.GetQuestionAsync(id);
        return SuccessResponse(question);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="createQuestionDto">创建题目请求数据</param>
    /// <returns>创建的题目信息</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion(CreateQuestionDto createQuestionDto)
    {
        ArgumentNullException.ThrowIfNull(createQuestionDto);
        QuestionDto questionDto = await _questionService.CreateQuestionAsync(createQuestionDto);
        return SuccessResponse(questionDto);
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateQuestionDto">更新题目请求数据</param>
    /// <returns>更新后的题目信息</returns>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestion(long id, UpdateQuestionDto updateQuestionDto)
    {
        ArgumentNullException.ThrowIfNull(updateQuestionDto);
        await _questionService.UpdateQuestionAsync(id, updateQuestionDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此题目吗？")]
    public async Task<ActionResult<ApiResponse>> DeleteQuestion(long id)
    {
        await _questionService.DeleteQuestionAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取题目历史版本
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目历史版本列表</returns>
    [HttpGet("{id:long}/versions")]
    [Operation("历史版本", "link", "/questions/${id}/versions", null)]
    public async Task<ActionResult<ApiResponse<List<QuestionVersionDto>>>> GetQuestionVersions(long id)
    {
        var versions = await _questionService.GetQuestionVersionsAsync(id);
        return SuccessResponse(versions);
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的题目吗？", isBulkOperation: true)]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        (int successCount, List<long> failedQuestionIds) = await _questionService.BatchDeleteAsync(request.Ids);

        return failedQuestionIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个题目，但以下题目删除失败: {string.Join(", ", failedQuestionIds)}")
            : SuccessResponse($"成功删除 {successCount} 个题目！");
    }

    /// <summary>
    /// 批量导入题目
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<QuestionBatchImportItemDto> importDto)
    {
        ArgumentNullException.ThrowIfNull(importDto);

        (int successCount, List<string> failedQuestions) = await _questionService.BatchImportAsync(importDto.ImportData);

        return failedQuestions.Any()
            ? SuccessResponse($"成功导入 {successCount} 个题目，但以下题目导入失败: {string.Join(", ", failedQuestions)}")
            : SuccessResponse($"成功导入 {successCount} 个题目！");
    }

    [HttpPost("batch/import-from-text")]
    [HeaderOperation("从文本导入", "form")]
    public async Task<ActionResult<ApiResponse>> BatchImportFromText([FromBody]QuestionImportFromTextDto input)
    {
        (int successCount, List<string> failedQuestions) = await _questionService.ImportFromTextAsync(input);

        return failedQuestions.Any()
            ? SuccessResponse($"成功导入 {successCount} 个题目，但以下题目导入失败: {string.Join(", ", failedQuestions)}")
            : SuccessResponse($"成功导入 {successCount} 个题目！");
    }
} 