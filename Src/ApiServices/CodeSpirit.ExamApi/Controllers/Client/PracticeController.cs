using CodeSpirit.Aggregator.Attributes;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Controllers.Client;

/// <summary>
/// 客户端练习控制器
/// </summary>
[DisplayName("练习")]
[Route("api/client/practice")]
[NoAudit("练习系统客户端接口频繁调用，不需要记录审计日志")]
[DisableAggregator]
public class PracticeController : ApiControllerBase
{
    private readonly IPracticeSettingService _practiceSettingService;
    private readonly IPracticeRecordService _practiceRecordService;
    private readonly ILogger<PracticeController> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="practiceSettingService">练习设置服务</param>
    /// <param name="practiceRecordService">练习记录服务</param>
    /// <param name="logger">日志服务</param>
    /// <param name="currentUser">当前用户</param>
    public PracticeController(
        IPracticeSettingService practiceSettingService,
        IPracticeRecordService practiceRecordService,
        ILogger<PracticeController> logger,
        ICurrentUser currentUser)
    {
        _practiceSettingService = practiceSettingService;
        _practiceRecordService = practiceRecordService;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取可用的练习设置列表
    /// </summary>
    /// <returns>练习设置列表</returns>
    [HttpGet("settings")]
    [DisplayName("获取可用练习设置")]
    public async Task<ActionResult<ApiResponse<List<PracticeSettingDto>>>> GetAvailablePracticeSettings()
    {
        // 从Token中获取当前学生ID (仅是示例，实际应根据认证系统获取)
        long studentId = GetCurrentUserId();

        // 仅获取已发布的练习设置
        var queryDto = new PracticeSettingQueryDto
        {
            Status = PracticeSettingStatus.Published
        };
        var result = await _practiceSettingService.GetPracticeSettingsAsync(queryDto);
        return SuccessResponse(result.Items);
    }

    /// <summary>
    /// 获取学生的练习统计数据
    /// </summary>
    /// <returns>练习统计数据</returns>
    [HttpGet("analysis")]
    [DisplayName("获取练习统计分析")]
    public async Task<ActionResult<ApiResponse<PracticeStatisticsDto>>> GetPracticeAnalysis()
    {
        long studentId = GetCurrentUserId();
        var statistics = await _practiceRecordService.GetStudentPracticeStatisticsAsync(studentId);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 获取学生的练习记录
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习会话记录列表</returns>
    [HttpGet("records")]
    [DisplayName("获取练习记录")]
    public async Task<ActionResult<ApiResponse<PageList<PracticeSessionDto>>>> GetPracticeRecords([FromQuery] PracticeSessionQueryDto queryDto)
    {
        long studentId = GetCurrentUserId();
        queryDto.StudentId = studentId;
        var records = await _practiceRecordService.GetPracticeSessionsAsync(queryDto);
        return SuccessResponse(records);
    }

    /// <summary>
    /// 开始练习
    /// </summary>
    /// <param name="id">练习设置ID</param>
    /// <returns>练习开始结果，包含练习数据和记录ID</returns>
    [HttpPost("{id}/start")]
    [DisplayName("开始练习")]
    public async Task<ActionResult<ApiResponse<PracticeStartResultDto>>> StartPractice(long id)
    {
        long studentId = GetCurrentUserId();
        
        // 开始练习并获取完整的练习数据
        var result = await _practiceRecordService.StartPracticeWithDataAsync(studentId, id);
        
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取练习基本信息
    /// </summary>
    /// <param name="id">练习设置ID</param>
    /// <returns>练习基本信息</returns>
    [HttpGet("{id}/basic")]
    [DisplayName("获取练习基本信息")]
    public async Task<ActionResult<ApiResponse<PracticeBasicInfoDto>>> GetPracticeBasicInfo(long id)
    {
        long studentId = GetCurrentUserId();
        var practiceInfo = await _practiceSettingService.GetPracticeBasicInfoAsync(id, studentId);
        return SuccessResponse(practiceInfo);
    }

    /// <summary>
    /// 保存答案
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="answerDto">答案数据</param>
    /// <returns>保存结果</returns>
    [HttpPost("{recordId}/save-answer")]
    [DisplayName("保存答案")]
    public async Task<ActionResult<ApiResponse>> SaveAnswer(long recordId, [FromBody] PracticeAnswerDto answerDto)
    {
        long studentId = GetCurrentUserId();
        await _practiceRecordService.SaveAnswerAsync(recordId, studentId, answerDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量保存答案
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>保存结果</returns>
    [HttpPost("{recordId}/save-answers")]
    [DisplayName("批量保存答案")]
    public async Task<ActionResult<ApiResponse>> SaveAnswers(long recordId, [FromBody] List<PracticeAnswerDto> answers)
    {
        long studentId = GetCurrentUserId();
        await _practiceRecordService.SaveAnswersAsync(recordId, studentId, answers);
        return SuccessResponse();
    }

    /// <summary>
    /// 提交练习
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>提交结果</returns>
    [HttpPost("{recordId}/submit")]
    [DisplayName("提交练习")]
    public async Task<ActionResult<ApiResponse>> SubmitPractice(long recordId, [FromBody] List<PracticeAnswerDto> answers)
    {
        long studentId = GetCurrentUserId();
        await _practiceRecordService.SubmitPracticeAsync(recordId, studentId, answers);
        return SuccessResponse();
    }

    /// <summary>
    /// 完成练习（自动完成或手动完成）
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <returns>完成结果</returns>
    [HttpPost("{recordId}/complete")]
    [DisplayName("完成练习")]
    public async Task<ActionResult<ApiResponse>> CompletePractice(long recordId)
    {
        long studentId = GetCurrentUserId();
        await _practiceRecordService.CompletePracticeAsync(recordId, studentId);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取练习结果
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <returns>练习结果</returns>
    [HttpGet("result/{recordId}")]
    [DisplayName("获取练习结果")]
    public async Task<ActionResult<ApiResponse<PracticeResultDto>>> GetPracticeResult(long recordId)
    {
        long studentId = GetCurrentUserId();
        var result = await _practiceRecordService.GetPracticeResultAsync(recordId, studentId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID，若未登录则返回0</returns>
    protected long GetCurrentUserId()
    {
        return _currentUser.Id.HasValue ? _currentUser.Id.Value : 0;
    }
}