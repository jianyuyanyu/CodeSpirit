using CodeSpirit.ExamApi.Dtos.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Dtos.Student;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;

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
    private readonly ICurrentUser _currentUser;
    private readonly IStudentService _studentService;
    private readonly IDistributedCache _distributedCache;
    private const string CLIENT_IP_HEADER = "X-Client-IP";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientService">客户端服务</param>
    /// <param name="logger">日志服务</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="studentService">考生服务</param>
    /// <param name="distributedCache">分布式缓存服务</param>
    public IndexController(
        IClientService clientService,
        ILogger<IndexController> logger,
        ICurrentUser currentUser,
        IStudentService studentService,
        IDistributedCache distributedCache)
    {
        _clientService = clientService;
        _logger = logger;
        _currentUser = currentUser;
        _studentService = studentService;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID，若未登录则返回0</returns>
    protected long GetCurrentUserId()
    {
        return _currentUser.Id.HasValue ? _currentUser.Id.Value : 0;
    }

    /// <summary>
    /// 验证当前用户是否已登录
    /// </summary>
    /// <returns>若用户已登录返回true，否则返回false</returns>
    private bool EnsureUserLoggedIn()
    {
        var userId = GetCurrentUserId();
        return userId > 0;
    }

    /// <summary>
    /// 获取可参加的考试列表
    /// </summary>
    /// <returns>可参加的考试列表</returns>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<List<ClientExamDto>>>> GetAvailableExams()
    {
        var currentUserId = GetCurrentUserId();
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
        var currentUserId = GetCurrentUserId();
        var result = await _clientService.GetExamHistoryAsync(currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <param name="answers">考试答案</param>
    /// <returns>操作结果，包含是否可以查看结果</returns>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitExam(long id, [FromBody] List<ClientExamAnswerDto> answers)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        // 如果有未保存的答案，先保存这些答案
        if (answers != null && answers.Count > 0)
        {
            _logger.LogInformation("提交考试前保存 {Count} 个答案", answers.Count);
            await _clientService.SaveAnswerAsync(id, currentUserId, answers);
        }

        // 调用提交服务更改考试状态（不再传递答案参数）
        var (success, enableViewResult) = await _clientService.SubmitExamAsync(id, currentUserId, null);

        // 返回提交结果和是否允许查看结果
        return Ok(new ApiResponse<dynamic>(0, "提交成功！", new { success, enableViewResult }));
    }

    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试结果</returns>
    [HttpGet("result/{id}")]
    public async Task<ActionResult<ApiResponse<ClientExamResultDto>>> GetExamResult(long id)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }
        var result = await _clientService.GetExamResultAsync(id, currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试基本信息
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试基本信息</returns>
    [HttpGet("{id}/basic")]
    public async Task<ActionResult<ApiResponse<ClientExamBasicInfoDto>>> GetExamBasicInfo(long id)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        // 获取用户IP和设备信息
        var userIp = GetClientIpAddress();
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        // 获取考试详情，包括题目列表
        var examDetail = await _clientService.GetExamDetailAsync(id, currentUserId, userIp, deviceInfo);
        
        // 获取基本信息
        var basicInfo = await _clientService.GetExamBasicInfoAsync(id, currentUserId);

        // 将题目信息添加到基本信息中
        basicInfo.Questions = examDetail.Questions.ToList();

        foreach (var item in basicInfo.Questions)
        {
            item.Content = WebUtility.HtmlDecode(item.Content);
            foreach (var option in item.Options)
            {
                option.Label = WebUtility.HtmlDecode(option.Label);
            }
        }
        
        // 获取用户已提交的答案
        var recordId = basicInfo.RecordId;
        if (recordId.HasValue)
        {
            try
            {
                _logger.LogDebug("获取考试答案，考试ID: {ExamId}, 记录ID: {RecordId}, 用户ID: {UserId}", 
                    id, recordId.Value, currentUserId);
                    
                var submittedAnswers = await _clientService.GetSubmittedAnswersAsync(recordId.Value, currentUserId);
                
                foreach (var item in basicInfo.Questions)
                {
                    item.Answer = submittedAnswers.FirstOrDefault(p => p.QuestionId == item.QuestionId)?.Answer ?? string.Empty;
                }
                
                _logger.LogDebug("成功获取考试答案，题目数: {QuestionCount}, 答案数: {AnswerCount}", 
                    basicInfo.Questions.Count, submittedAnswers.Count);
            }
            catch (Exception ex)
            {
                // 记录错误但不影响整体返回
                _logger.LogError(ex, "获取考试答案时出现未处理错误，考试ID: {ExamId}, 记录ID: {RecordId}, 用户ID: {UserId}", 
                    id, recordId.Value, currentUserId);
            }
        }
        else
        {
            _logger.LogWarning("无法获取答案：考试记录ID为空，考试ID: {ExamId}, 用户ID: {UserId}", id, currentUserId);
        }
        return SuccessResponse(basicInfo);
    }

    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试记录ID</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult<object>> StartExam(long id)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        var userIp = GetClientIpAddress();
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        try
        {
            var record = await _clientService.CreateExamRecordAsync(id, currentUserId, userIp, deviceInfo);
            return new { id = record.ExamSettingId };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "创建考试记录时发生业务逻辑错误，用户 {UserId}，考试 {ExamId}", currentUserId, id);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建考试记录时发生未处理错误，用户 {UserId}，考试 {ExamId}", currentUserId, id);
            return StatusCode(500, new ApiResponse(-1, "创建考试记录失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <returns>客户端IP地址</returns>
    private string GetClientIpAddress()
    {
        try
        {
            // 优化IP获取逻辑，提高性能
            if (HttpContext.Request.Headers.TryGetValue(CLIENT_IP_HEADER, out var clientIp) && !string.IsNullOrEmpty(clientIp))
            {
                return clientIp.ToString().Trim();
            }

            // 按优先级尝试获取IP地址
            if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIps))
            {
                var ips = forwardedIps.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    // 通常第一个是客户端真实IP
                    return ips[0]?.Trim() ?? "未知";
                }
            }

            // 尝试从X-Real-IP获取
            if (HttpContext.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                return realIp.ToString().Trim();
            }

            // 使用HttpContext.Connection.RemoteIpAddress
            if (HttpContext.Connection.RemoteIpAddress != null)
            {
                return HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return "未知";
        }
        catch (Exception ex)
        {
            // 记录异常但不中断流程
            _logger.LogError(ex, "获取客户端IP地址时发生异常");
            return "未知";
        }
    }

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/screen-switch")]
    public async Task<ActionResult<ApiResponse>> RecordScreenSwitch(long id)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            var userIp = GetClientIpAddress();

            // 直接调用服务记录切屏，不使用缓存
            await _clientService.RecordScreenSwitchAsync(id, currentUserId, userIp);
            return SuccessResponse();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "记录切屏事件时发生业务逻辑错误，用户 {UserId}，考试记录 {RecordId}", currentUserId, id);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录切屏事件时发生未处理错误，用户 {UserId}，考试记录 {RecordId}", currentUserId, id);
            // 返回成功而不是错误，避免影响用户体验
            return SuccessResponse();
        }
    }

    /// <summary>
    /// 获取考生个人信息
    /// </summary>
    /// <returns>考生个人信息</returns>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<ClientProfileDto>>> GetProfile()
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            var result = await _clientService.GetStudentProfileAsync(currentUserId);
            return SuccessResponse(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "获取考生个人信息时发生业务逻辑错误，用户 {UserId}", currentUserId);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考生个人信息时发生未处理错误，用户 {UserId}", currentUserId);
            return StatusCode(500, new ApiResponse(-1, "获取考生信息失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 保存单个题目的答案
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <param name="answer">单个题目的答案</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/save-answer")]
    public async Task<ActionResult<ApiResponse>> SaveAnswer(long id, [FromBody] ClientExamAnswerDto answer)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            if (answer == null)
            {
                return BadRequest(new ApiResponse(-1, "答案数据无效"));
            }

            // 将单个答案转换为列表，以便重用现有的服务方法
            var answers = new List<ClientExamAnswerDto> { answer };

            // 保存单个答案到服务器
            var success = await _clientService.SaveAnswerAsync(id, currentUserId, answers);

            return SuccessResponse();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "保存答案时发生业务逻辑错误，用户 {UserId}，考试记录 {RecordId}，题目 {QuestionId}",
                currentUserId, id, answer?.QuestionId);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存答案时发生未处理错误，用户 {UserId}，考试记录 {RecordId}，题目 {QuestionId}",
                currentUserId, id, answer?.QuestionId);
            // 返回成功而不是错误，避免影响用户体验
            return SuccessResponse();
        }
    }

    /// <summary>
    /// 保存多个题目的答案
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <param name="answers">多个题目的答案</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/save-answers")]
    public async Task<ActionResult<ApiResponse>> SaveAnswers(long id, [FromBody] List<ClientExamAnswerDto> answers)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            if (answers == null || !answers.Any())
            {
                return BadRequest(new ApiResponse(-1, "答案数据无效"));
            }

            // 保存多个答案到服务器
            var success = await _clientService.SaveAnswerAsync(id, currentUserId, answers);

            return SuccessResponse();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "批量保存答案时发生业务逻辑错误，用户 {UserId}，考试记录 {RecordId}", currentUserId, id);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量保存答案时发生未处理错误，用户 {UserId}，考试记录 {RecordId}", currentUserId, id);
            // 返回成功而不是错误，避免影响用户体验
            return SuccessResponse();
        }
    }
}