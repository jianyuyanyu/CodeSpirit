using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷题目管理控制器
/// </summary>
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-question-circle", PlatformType = PlatformType.Tenant)]
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
    /// <param name="surveyId">问卷ID</param>
    /// <returns>题目列表</returns>
    [HttpGet]
    [DisplayName("获取题目列表")]
    public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetQuestions([FromQuery] int surveyId)
    {
        var questions = await _questionService.GetQuestionsBySurveyIdAsync(surveyId);
        return SuccessResponse(questions);
    }

    /// <summary>
    /// 获取题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取题目详情")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> GetQuestion(int id)
    {
        var question = await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).GetAsync(id);
        return SuccessResponse(question);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>创建的题目</returns>
    [HttpPost]
    [DisplayName("创建题目")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion([FromBody] CreateQuestionDto createDto)
    {
        var question = await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).CreateAsync(createDto);
        return SuccessResponseWithCreate(nameof(GetQuestion), question);
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateDto">更新题目DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新题目")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestion(int id, [FromBody] UpdateQuestionDto updateDto)
    {
        await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).UpdateAsync(id, updateDto);
        return SuccessResponse("题目更新成功");
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除题目")]
    public async Task<ActionResult<ApiResponse>> DeleteQuestion(int id)
    {
        await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).DeleteAsync(id);
        return SuccessResponse("题目删除成功");
    }

    /// <summary>
    /// 批量排序题目
    /// </summary>
    /// <param name="request">排序请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("reorder")]
    [Operation("排序", "form")]
    [DisplayName("题目排序")]
    public async Task<ActionResult<ApiResponse>> ReorderQuestions([FromBody] ReorderQuestionsRequest request)
    {
        await _questionService.ReorderQuestionsAsync(request.SurveyId, request.QuestionOrders);
        return SuccessResponse("题目排序成功");
    }

    /// <summary>
    /// 复制题目
    /// </summary>
    /// <param name="id">源题目ID</param>
    /// <param name="request">复制请求</param>
    /// <returns>复制的题目</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "form")]
    [DisplayName("复制题目")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CopyQuestion(int id, [FromBody] CopyQuestionRequest request)
    {
        var copiedQuestion = await _questionService.CopyQuestionToSurveyAsync(id, request.TargetSurveyId);
        return SuccessResponse(copiedQuestion);
    }
}

/// <summary>
/// 题目排序请求
/// </summary>
public class ReorderQuestionsRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 题目ID和新顺序的映射
    /// </summary>
    [Required]
    [DisplayName("排序信息")]
    public Dictionary<int, int> QuestionOrders { get; set; } = new();
}

/// <summary>
/// 复制题目请求
/// </summary>
public class CopyQuestionRequest
{
    /// <summary>
    /// 目标问卷ID
    /// </summary>
    [Required]
    [DisplayName("目标问卷ID")]
    public int TargetSurveyId { get; set; }
}
