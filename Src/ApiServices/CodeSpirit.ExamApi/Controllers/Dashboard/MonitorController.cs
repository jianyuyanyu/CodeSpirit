using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.Monitor;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers.Dashboard;

/// <summary>
/// 监考大屏控制器
/// </summary>
[DisplayName("监考大屏")]
[Navigation(Icon = "fa-solid fa-desktop", Hidden = true)]
public class MonitorController : ApiControllerBase
{
    private readonly IMonitorService _monitorService;
    private readonly ILogger<MonitorController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="monitorService">监考服务</param>
    /// <param name="logger">日志服务</param>
    public MonitorController(IMonitorService monitorService, ILogger<MonitorController> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    /// <summary>
    /// 获取考试监控信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试监控信息</returns>
    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<ApiResponse<ExamMonitorDto>>> GetExamMonitor(long examId)
    {
        try
        {
            var result = await _monitorService.GetExamMonitorAsync(examId);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试监控信息失败, 考试ID: {ExamId}", examId);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
    }

    /// <summary>
    /// 获取考生监控信息
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考生监控信息</returns>
    [HttpGet("student/{recordId}")]
    public async Task<ActionResult<ApiResponse<ExamStudentMonitorDto>>> GetStudentMonitor(long recordId)
    {
        try
        {
            var result = await _monitorService.GetStudentMonitorAsync(recordId);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考生监控信息失败, 考试记录ID: {RecordId}", recordId);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
    }

    /// <summary>
    /// 结束考生考试
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("student/{recordId}/terminate")]
    [Operation("强制交卷", "ajax", null, "确定要强制结束该考生的考试吗？")]
    public async Task<ActionResult<ApiResponse>> TerminateStudentExam(long recordId)
    {
        try
        {
            await _monitorService.TerminateStudentExamAsync(recordId);
            return SuccessResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制结束考生考试失败, 考试记录ID: {RecordId}", recordId);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
    }

    /// <summary>
    /// 标记作弊
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="reason">作弊原因</param>
    /// <returns>操作结果</returns>
    [HttpPost("student/{recordId}/flag-cheating")]
    [Operation("标记作弊", "ajax", null, "确定要标记该考生作弊吗？")]
    public async Task<ActionResult<ApiResponse>> FlagStudentCheating(long recordId, [FromQuery] string reason)
    {
        try
        {
            await _monitorService.FlagStudentCheatingAsync(recordId, reason);
            return SuccessResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记考生作弊失败, 考试记录ID: {RecordId}, 原因: {Reason}", recordId, reason);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
    }
}