using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.WrongQuestion;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 错题管理控制器
/// </summary>
[DisplayName("错题管理")]
[Navigation(Icon = "fa-solid fa-circle-exclamation", PlatformType = PlatformType.Tenant)]
public class WrongQuestionsController : ApiControllerBase
{
    private readonly IWrongQuestionService _wrongQuestionService;
    private readonly ILogger<WrongQuestionsController> _logger;

    /// <summary>
    /// 初始化错题管理控制器
    /// </summary>
    /// <param name="wrongQuestionService">错题服务</param>
    /// <param name="logger">日志记录器</param>
    public WrongQuestionsController(
        IWrongQuestionService wrongQuestionService,
        ILogger<WrongQuestionsController> logger)
    {
        ArgumentNullException.ThrowIfNull(wrongQuestionService);
        ArgumentNullException.ThrowIfNull(logger);

        _wrongQuestionService = wrongQuestionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取错题列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>错题列表分页结果</returns>
    [HttpGet]
    [DisplayName("获取错题列表")]
    public async Task<ActionResult<ApiResponse<PageList<WrongQuestionDto>>>> GetWrongQuestions([FromQuery] WrongQuestionQueryDto queryDto)
    {
        PageList<WrongQuestionDto> wrongQuestions = await _wrongQuestionService.GetWrongQuestionsAsync(queryDto);
        return SuccessResponse(wrongQuestions);
    }

    /// <summary>
    /// 获取考生的错题列表
    /// </summary>
    /// <param name="studentId">考生ID</param>
    /// <returns>考生的错题列表</returns>
    [HttpGet("students/{studentId:long}")]
    [DisplayName("获取考生错题列表")]
    public async Task<ActionResult<ApiResponse<List<WrongQuestionDto>>>> GetStudentWrongQuestions(long studentId)
    {
        if (studentId <= 0)
        {
            return BadRequest("无效的考生ID");
        }
        
        List<WrongQuestionDto> wrongQuestions = await _wrongQuestionService.GetStudentWrongQuestionsAsync(studentId);
        return SuccessResponse(wrongQuestions);
    }

    /// <summary>
    /// 获取错题详情
    /// </summary>
    /// <param name="id">错题ID</param>
    /// <returns>错题详细信息</returns>
    [HttpGet("{id:long}")]
    [DisplayName("获取错题详情")]
    public async Task<ActionResult<ApiResponse<WrongQuestionDto>>> GetWrongQuestion(long id)
    {
        WrongQuestionDto wrongQuestion = await _wrongQuestionService.GetAsync(id);
        return SuccessResponse(wrongQuestion);
    }

    /// <summary>
    /// 创建错题
    /// </summary>
    /// <param name="createDto">创建错题请求数据</param>
    /// <returns>创建的错题信息</returns>
    [HttpPost]
    [DisplayName("创建错题")]
    public async Task<ActionResult<ApiResponse<WrongQuestionDto>>> CreateWrongQuestion(CreateWrongQuestionDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        WrongQuestionDto wrongQuestionDto = await _wrongQuestionService.CreateAsync(createDto);
        return SuccessResponse(wrongQuestionDto);
    }

    /// <summary>
    /// 记录错题（如果已存在则更新错误次数）
    /// </summary>
    /// <param name="createDto">错题记录数据</param>
    /// <returns>错题记录结果</returns>
    [HttpPost("record")]
    [DisplayName("记录错题")]
    public async Task<ActionResult<ApiResponse<WrongQuestionDto>>> RecordWrongQuestion(CreateWrongQuestionDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        WrongQuestionDto wrongQuestionDto = await _wrongQuestionService.RecordWrongQuestionAsync(createDto);
        return SuccessResponse(wrongQuestionDto);
    }

    /// <summary>
    /// 更新错题
    /// </summary>
    /// <param name="id">错题ID</param>
    /// <param name="updateDto">更新错题请求数据</param>
    /// <returns>更新操作结果</returns>
    [HttpPut("{id:long}")]
    [DisplayName("更新错题")]
    public async Task<ActionResult<ApiResponse>> UpdateWrongQuestion(long id, UpdateWrongQuestionDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        await _wrongQuestionService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除错题
    /// </summary>
    /// <param name="id">错题ID</param>
    /// <returns>删除操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此错题记录吗？")]
    [DisplayName("删除错题")]
    public async Task<ActionResult<ApiResponse>> DeleteWrongQuestion(long id)
    {
        await _wrongQuestionService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除错题
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>批量删除操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的错题记录吗？", IsBulkOperation = true)]
    [DisplayName("批量删除错题")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteWrongQuestions([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        (int successCount, List<long> failedIds) = await _wrongQuestionService.BatchDeleteAsync(request.Ids);
        
        return failedIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个错题记录，但以下错题记录删除失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功删除 {successCount} 个错题记录！");
    }
} 