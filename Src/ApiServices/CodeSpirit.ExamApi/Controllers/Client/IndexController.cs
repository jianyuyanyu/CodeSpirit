using CodeSpirit.Aggregator.Attributes;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Services;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;

namespace CodeSpirit.ExamApi.Controllers.Client;

/// <summary>
/// 考试客户端接口
/// </summary>
[Authorize]
[DisplayName("考试客户端")]
[Route("api/exam/client")]
[NoAudit("考试客户端接口频繁调用，不需要记录审计日志")]
[DisableAggregator]
public class IndexController : ApiControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<IndexController> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly IStudentService _studentService;
    private readonly IClientIpService _clientIpService;
    private readonly IExamSettingService _examSettingService;
    private readonly IDistributedCache _distributedCache;
    private const string CLIENT_IP_HEADER = "X-Client-IP";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientService">客户端服务</param>
    /// <param name="logger">日志服务</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="studentService">考生服务</param>
    /// <param name="clientIpService">客户端IP地址获取服务</param>
    /// <param name="examSettingService">考试设置服务</param>
    /// <param name="distributedCache">分布式缓存</param>
    public IndexController(
        IClientService clientService,
        ILogger<IndexController> logger,
        ICurrentUser currentUser,
        IStudentService studentService,
        IClientIpService clientIpService,
        IExamSettingService examSettingService,
        IDistributedCache distributedCache)
    {
        _clientService = clientService;
        _logger = logger;
        _currentUser = currentUser;
        _studentService = studentService;
        _clientIpService = clientIpService;
        _examSettingService = examSettingService;
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
    [OutputCache(Duration = 30, VaryByHeaderNames = new string[] { "Authorization" }, Tags = new string[] { "byUser" })]
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
    [OutputCache(Duration = 600, VaryByHeaderNames = new string[] { "Authorization" }, Tags = new string[] { "byUser" })]
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
            _logger.LogDebug("提交考试前保存 {Count} 个答案", answers.Count);
            await _clientService.SaveAnswerAsync(id, currentUserId, answers);
        }

        // 调用提交服务更改考试状态（不再传递答案参数）
        var (success, enableViewResult) = await _clientService.SubmitExamAsync(id, currentUserId);

        // 返回提交结果和是否允许查看结果
        return Ok(new ApiResponse<dynamic>(0, "提交成功！", new { success, enableViewResult }));
    }

    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试结果</returns>
    [HttpGet("result/{id}")]
    [OutputCache(Duration = 600, VaryByHeaderNames = new string[] { "Authorization" }, VaryByRouteValueNames = new string[] { "id" }, Tags = new string[] { "byUser" })]
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
    [OutputCache(Duration = 60, VaryByHeaderNames = new string[] { "Authorization" },
    VaryByRouteValueNames = new string[] { "id" }, Tags = new string[] { "byUser", "exam-basic" })]
    public async Task<ActionResult<ApiResponse<ClientExamBasicInfoDto>>> GetExamBasicInfo(long id)
    {
        // ⚡ 性能监控：记录方法执行时间
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        // ✅ 使用优化后的缓存方案：
        // 1. 题目数据可以缓存（所有用户共享）
        // 2. 用户的题目顺序保存在 AnswerRecord.OrderNumber 中
        // 3. 直接调用轻量级方法获取题目和答案，最大化利用缓存

        // ⚡ 性能优化：并行获取基本信息和用户记录（两者无依赖关系）
        var basicInfoTask = _clientService.GetExamBasicInfoWithCacheAsync(id);
        var userRecordTask = _clientService.GetUserExamRecordWithCacheAsync(id, currentUserId);

        await Task.WhenAll(basicInfoTask, userRecordTask);

        var basicInfoCache = await basicInfoTask;
        var userRecord = await userRecordTask;

        if (basicInfoCache == null)
        {
            _logger.LogError("无法获取考试基本信息，考试ID: {ExamId}, 用户ID: {UserId}", id, currentUserId);
            return BadRequest(new ApiResponse(-1, "无法获取考试信息"));
        }

        if (userRecord == null)
        {
            throw new BusinessException("无法获取用户考试记录，请重新登陆！");
        }

        _logger.LogDebug("获取缓存基本信息完成，考试ID: {ExamId}, Name: {Name}, TotalScore: {TotalScore}, Duration: {Duration}",
            id, basicInfoCache.Name, basicInfoCache.TotalScore, basicInfoCache.Duration);

        // ✅ 使用新的轻量级方法直接获取题目和答案（最大化利用缓存）
        var questions = await _examSettingService.GetExamQuestionsWithAnswersAsync(id, userRecord.RecordId);

        _logger.LogDebug("获取考试题目及答案完成，考试ID: {ExamId}, 用户ID: {UserId}, RecordId: {RecordId}, 题目数: {QuestionCount}",
            id, currentUserId, userRecord.RecordId, questions.Count);

        // 合并数据：缓存的基本信息 + 按用户顺序排列的题目列表（已包含答案）
        var basicInfo = new ClientExamBasicInfoDto
        {
            // 使用缓存的共享数据
            Id = basicInfoCache.Id,
            Name = basicInfoCache.Name,
            Description = basicInfoCache.Description ?? string.Empty,
            Duration = basicInfoCache.Duration,
            StartTime = userRecord.StartTime,  // ✅ 使用用户实际开始时间
            EndTime = basicInfoCache.EndTime ?? DateTime.UtcNow.AddMinutes(basicInfoCache.Duration),  // ✅ 使用考试设置的结束时间
            TotalScore = (double)basicInfoCache.TotalScore,
            AllowedScreenSwitchCount = basicInfoCache.AllowedScreenSwitchCount,
            EnableViewResult = basicInfoCache.EnableViewResult,
            MinExamTime = basicInfoCache.MinExamTime ?? 0,

            // 使用用户特定的数据
            RecordId = userRecord.RecordId,
            ScreenSwitchCount = userRecord.ScreenSwitchCount,
            Status = userRecord.Status,  // ✅ 设置考试记录状态
            Questions = questions  // 已包含答案，按用户的OrderNumber排序
        };

        _logger.LogDebug("成功构建考试基本信息，题目数: {QuestionCount}, 已填答案数: {FilledCount}",
            basicInfo.Questions.Count, basicInfo.Questions.Count(q => !string.IsNullOrEmpty(q.Answer)));

        // ⚡ 性能监控：记录执行时间，超过阈值时警告
        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        if (elapsedMs > 500)
        {
            _logger.LogWarning("GetExamBasicInfo 执行时间过长: {ElapsedMs}ms, 考试ID: {ExamId}, 用户ID: {UserId}", 
                elapsedMs, id, currentUserId);
        }
        else
        {
            _logger.LogDebug("GetExamBasicInfo 执行完成: {ElapsedMs}ms, 考试ID: {ExamId}", elapsedMs, id);
        }

        return SuccessResponse(basicInfo);
    }

    /// <summary>
    /// 获取考试轻量信息（用于倒计时页面）
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试轻量信息</returns>
    [HttpGet("{id}/light")]
    [DisplayName("获取考试轻量信息")]
    [OutputCache(Duration = 60, VaryByHeaderNames = new string[] { "Authorization" }, VaryByRouteValueNames = new string[] { "id" }, Tags = new string[] { "byUser", "exam-light" })]
    public async Task<ActionResult<ApiResponse<ClientExamLightInfoDto>>> GetExamLightInfo(long id)
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            var lightInfo = await _clientService.GetExamLightInfoAsync(id, currentUserId);
            return SuccessResponse(lightInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "获取考试轻量信息时发生业务逻辑错误，用户 {UserId}，考试 {ExamId}", currentUserId, id);
            return BadRequest(new ApiResponse(-1, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试轻量信息时发生未处理错误，用户 {UserId}，考试 {ExamId}", currentUserId, id);
            return StatusCode(500, new ApiResponse(-1, "获取考试信息失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试ID</returns>
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
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "创建考试记录时发生业务逻辑错误，用户 {UserId}，考试 {ExamId}", currentUserId, id);
            return BadRequest(new ApiResponse(-1, ex.Message));
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
        return _clientIpService.GetClientIpAddress(HttpContext);
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
    [DisplayName("获取个人信息")]
    [OutputCache(Duration = 60, VaryByHeaderNames = new string[] { "Authorization" }, Tags = new string[] { "byUser" })]
    public async Task<ActionResult<ApiResponse<ClientProfileDto>>> GetProfile()
    {
        var currentUserId = GetCurrentUserId();
        if (!EnsureUserLoggedIn())
        {
            return Unauthorized(new ApiResponse(-1, "请先登录"));
        }

        try
        {
            var result = await _clientService.GetStudentProfileWithCacheAsync(currentUserId);
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
        // ⚡ 性能监控：记录方法执行时间
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
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

            // ⚡ 性能优化：请求去重机制 - 5秒内相同答案不重复保存
            var deduplicationKey = $"answer_dedup_{id}_{currentUserId}_{answer.QuestionId}_{answer.Answer?.GetHashCode() ?? 0}";
            var existingRequest = await _distributedCache.GetStringAsync(deduplicationKey);
            
            if (!string.IsNullOrEmpty(existingRequest))
            {
                _logger.LogDebug("检测到重复的答案保存请求，直接返回成功: {Key}", deduplicationKey);
                return SuccessResponse();
            }

            // 将单个答案转换为列表，以便重用现有的服务方法
            var answers = new List<ClientExamAnswerDto> { answer };

            // 保存单个答案到服务器
            var success = await _clientService.SaveAnswerAsync(id, currentUserId, answers);

            // 记录请求，5秒内防止重复提交
            await _distributedCache.SetStringAsync(deduplicationKey, "1", 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) });

            // ⚡ 性能监控：记录执行时间
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs > 200)
            {
                _logger.LogWarning("SaveAnswer 执行时间过长: {ElapsedMs}ms, 考试记录ID: {RecordId}, 题目ID: {QuestionId}", 
                    elapsedMs, id, answer.QuestionId);
            }

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