using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试记录控制器
/// </summary>
[DisplayName("考试记录管理")]
[Navigation(Icon = "fa-solid fa-clipboard-check")]
public class ExamRecordsController : ApiControllerBase
{
    private readonly IExamRecordService _examRecordService;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examRecordService">考试记录服务</param>
    public ExamRecordsController(IExamRecordService examRecordService)
    {
        _examRecordService = examRecordService;
    }
    
    /// <summary>
    /// 获取考试记录列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>考试记录列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ExamRecordDto>>>> GetExamRecords([FromQuery] ExamRecordQueryDto queryDto)
    {
        var records = await _examRecordService.GetPagedListAsync(queryDto);
        return SuccessResponse(records);
    }
    
    /// <summary>
    /// 获取考试记录详情
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试记录详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExamRecordDto>>> GetExamRecordDetail(long id)
    {
        var record = await _examRecordService.GetExamRecordDetailAsync(id);
        return SuccessResponse(record);
    }
    
    /// <summary>
    /// 开始考试
    /// </summary>
    /// <param name="startExamDto">开始考试参数</param>
    /// <returns>考试记录</returns>
    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<ExamRecordDto>>> StartExam(StartExamDto startExamDto)
    {
        var record = await _examRecordService.StartExamAsync(startExamDto);
        return SuccessResponse(record);
    }
    
    /// <summary>
    /// 提交答案
    /// </summary>
    /// <param name="submitAnswerDto">提交答案参数</param>
    /// <returns>操作结果</returns>
    [HttpPost("submit-answer")]
    public async Task<ActionResult<ApiResponse>> SubmitAnswer(SubmitAnswerDto submitAnswerDto)
    {
        await _examRecordService.SubmitAnswerAsync(submitAnswerDto);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 完成考试
    /// </summary>
    /// <param name="finishExamDto">完成考试参数</param>
    /// <returns>考试结果</returns>
    [HttpPost("finish")]
    public async Task<ActionResult<ApiResponse<ExamRecordDto>>> FinishExam(FinishExamDto finishExamDto)
    {
        var result = await _examRecordService.FinishExamAsync(finishExamDto);
        return SuccessResponse(result);
    }
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{recordId}/screen-switch")]
    public async Task<ActionResult<ApiResponse>> RecordScreenSwitch(long recordId)
    {
        await _examRecordService.RecordScreenSwitchAsync(recordId);
        return SuccessResponse();
    }
    
    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <returns>考试统计信息</returns>
    [HttpGet("statistics/{examSettingId}")]
    [Operation("考试统计", "link", null, null)]
    public async Task<ActionResult<ApiResponse<ExamStatisticsDto>>> GetExamStatistics(long examSettingId)
    {
        var statistics = await _examRecordService.GetExamStatisticsAsync(examSettingId);
        return SuccessResponse(statistics);
    }
    
    /// <summary>
    /// 获取错题列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>错题列表</returns>
    [HttpGet("wrong-questions")]
    [Operation("错题管理", "link", null, null)]
    public async Task<ActionResult<ApiResponse<PageList<WrongQuestionDto>>>> GetWrongQuestions([FromQuery] WrongQuestionQueryDto queryDto)
    {
        var wrongQuestions = await _examRecordService.GetWrongQuestionsAsync(queryDto);
        return SuccessResponse(wrongQuestions);
    }
} 