using CodeSpirit.ExamApi.Dtos.Client;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.ExamApi.Controllers.Client;

/// <summary>
/// 考试客户端接口
/// </summary>
[DisplayName("考试客户端")]
[Route("api/exam/client")]
public class IndexController : ApiControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<IndexController> _logger;
    private readonly ICurrentUser currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientService">客户端服务</param>
    /// <param name="logger">日志服务</param>
    public IndexController(IClientService clientService, ILogger<IndexController> logger, ICurrentUser currentUser)
    {
        _clientService = clientService;
        _logger = logger;
        this.currentUser = currentUser;
    }

    /// <summary>
    /// 获取可参加的考试列表
    /// </summary>
    /// <returns>可参加的考试列表</returns>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<List<ClientExamDto>>>> GetAvailableExams()
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetAvailableExamsAsync(currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试历史记录
    /// </summary>
    /// <returns>考试历史记录</returns>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<ClientExamHistoryDto>>>> GetExamHistory()
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetExamHistoryAsync(currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ClientExamDetailDto>>> GetExamDetail(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        var result = await _clientService.GetExamDetailAsync(id, currentUserId, userIp, deviceInfo);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <param name="answers">考试答案</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitExam(long id, [FromBody] List<ClientExamAnswerDto> answers)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        await _clientService.SubmitExamAsync(id, currentUserId, answers);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试结果</returns>
    [HttpGet("result/{id}")]
    public async Task<ActionResult<ApiResponse<ClientExamResultDto>>> GetExamResult(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetExamResultAsync(id, currentUserId);
        return SuccessResponse(result);
    }
}