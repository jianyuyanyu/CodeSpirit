using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.SurveyApi.Dtos.App;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Controllers.App;

/// <summary>
/// App端问卷回答控制器
/// </summary>
[DisplayName("App问卷回答")]
public class ResponsesController : AppControllerBase
{
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IRepository<SurveyResponse> _responseRepository;
    private readonly IRepository<ResponseAnswer> _answerRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IClientIpService _clientIpService;
    private readonly IMapper _mapper;
    private readonly ILogger<ResponsesController> _logger;

    /// <summary>
    /// 初始化App端问卷回答控制器
    /// </summary>
    /// <param name="surveyRepository">问卷仓储</param>
    /// <param name="responseRepository">回答仓储</param>
    /// <param name="answerRepository">答案仓储</param>
    /// <param name="questionRepository">题目仓储</param>
    /// <param name="clientIpService">客户端IP服务</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志记录器</param>
    public ResponsesController(
        IRepository<Survey> surveyRepository,
        IRepository<SurveyResponse> responseRepository,
        IRepository<ResponseAnswer> answerRepository,
        IRepository<Question> questionRepository,
        IClientIpService clientIpService,
        IMapper mapper,
        ILogger<ResponsesController> logger)
    {
        ArgumentNullException.ThrowIfNull(surveyRepository);
        ArgumentNullException.ThrowIfNull(responseRepository);
        ArgumentNullException.ThrowIfNull(answerRepository);
        ArgumentNullException.ThrowIfNull(questionRepository);
        ArgumentNullException.ThrowIfNull(clientIpService);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(logger);

        _surveyRepository = surveyRepository;
        _responseRepository = responseRepository;
        _answerRepository = answerRepository;
        _questionRepository = questionRepository;
        _clientIpService = clientIpService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 提交问卷回答
    /// </summary>
    /// <param name="request">提交请求</param>
    /// <returns>提交结果</returns>
    [HttpPost("submit")]
    [DisplayName("提交问卷回答")]
    public async Task<ActionResult<ApiResponse<SubmitSurveyResponseDto>>> SubmitSurvey([FromBody] SubmitSurveyRequestDto request)
    {
        try
        {
            // 验证请求数据
            if (!ModelState.IsValid)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("请求数据无效");
            }

            // 验证问卷标识（SurveyId或AccessCode必须提供其一）
            if (!request.SurveyId.HasValue && string.IsNullOrWhiteSpace(request.AccessCode))
            {
                return ErrorResponse<SubmitSurveyResponseDto>("必须提供问卷ID或访问码");
            }

            if (request.SurveyId.HasValue && !string.IsNullOrWhiteSpace(request.AccessCode))
            {
                return ErrorResponse<SubmitSurveyResponseDto>("问卷ID和访问码不能同时提供");
            }

            // 根据标识类型查找问卷
            Survey? survey = null;
            if (request.SurveyId.HasValue)
            {
                // 使用问卷ID查找
                survey = await _surveyRepository.Find(s => s.Id == request.SurveyId.Value)
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrWhiteSpace(request.AccessCode))
            {
                // 验证访问码格式
                if (!AccessCodeGenerator.IsValidAccessCode(request.AccessCode))
                {
                    return ErrorResponse<SubmitSurveyResponseDto>("无效的访问码格式");
                }

                // 使用访问码查找
                survey = await _surveyRepository.Find(s => s.PublicAccessCode == request.AccessCode)
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync();
            }

            if (survey == null)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("问卷不存在");
            }

            if (survey.Status != SurveyStatus.Published)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("问卷未发布");
            }

            if (survey.AccessType != SurveyAccessType.Public)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("问卷不允许公开访问");
            }

            if (survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("问卷已过期");
            }

            // 如果使用访问码，还需要检查分享链接是否过期
            if (!string.IsNullOrWhiteSpace(request.AccessCode))
            {
                if (survey.ShareExpiresAt.HasValue && survey.ShareExpiresAt.Value < DateTime.UtcNow)
                {
                    return ErrorResponse<SubmitSurveyResponseDto>("分享链接已过期");
                }
            }

            // 检查是否已经提交过（基于SessionId）
            var existingResponse = await _responseRepository.Find(r => 
                r.SurveyId == survey.Id && 
                r.SessionId == request.SessionId &&
                r.Status == ResponseStatus.Completed)
                .FirstOrDefaultAsync();

            if (existingResponse != null)
            {
                return ErrorResponse<SubmitSurveyResponseDto>("您已经提交过此问卷");
            }

            // 验证必填题目是否都有回答
            var requiredQuestions = survey.Questions.Where(q => q.IsRequired).ToList();
            var answeredQuestionIds = request.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText) || !string.IsNullOrWhiteSpace(a.AnswerValue))
                .Select(a => a.QuestionId)
                .ToHashSet();

            var missingRequiredQuestions = requiredQuestions
                .Where(q => !answeredQuestionIds.Contains(q.Id))
                .ToList();

            if (missingRequiredQuestions.Any())
            {
                var missingTitles = string.Join(", ", missingRequiredQuestions.Select(q => q.Title));
                return ErrorResponse<SubmitSurveyResponseDto>($"以下必填题目未回答：{missingTitles}");
            }

            // 验证回答的题目是否都属于该问卷
            var surveyQuestionIds = survey.Questions.Select(q => q.Id).ToHashSet();
            var invalidAnswers = request.Answers
                .Where(a => !surveyQuestionIds.Contains(a.QuestionId))
                .ToList();

            if (invalidAnswers.Any())
            {
                return ErrorResponse<SubmitSurveyResponseDto>("提交的答案中包含无效的题目");
            }

            // 获取客户端信息
            var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
            var userAgent = Request.Headers.UserAgent.ToString();

            // 创建问卷回答记录
            var response = new SurveyResponse
            {
                SurveyId = survey.Id,
                RespondentId = request.RespondentId,
                SessionId = request.SessionId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Status = ResponseStatus.Completed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = request.DeviceFingerprint,
                Metadata = request.Metadata
            };

            await _responseRepository.AddAsync(response);

            // 保存答案详情
            foreach (var answerDto in request.Answers)
            {
                var answer = new ResponseAnswer
                {
                    ResponseId = response.Id,
                    QuestionId = answerDto.QuestionId,
                    AnswerText = answerDto.AnswerText,
                    AnswerValue = answerDto.AnswerValue,
                    AnsweredAt = DateTime.UtcNow
                };

                await _answerRepository.AddAsync(answer, false);
            }

            await _answerRepository.SaveChangesAsync();

            _logger.LogInformation("问卷提交成功，问卷ID: {SurveyId}, 访问码: {AccessCode}, 会话ID: {SessionId}, 回答ID: {ResponseId}", 
                survey.Id, request.AccessCode ?? "N/A", request.SessionId, response.Id);

            var result = _mapper.Map<SubmitSurveyResponseDto>(response);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交问卷失败，问卷ID: {SurveyId}, 访问码: {AccessCode}, 会话ID: {SessionId}", 
                request.SurveyId ?? 0, request.AccessCode ?? "N/A", request.SessionId);
            return ErrorResponse<SubmitSurveyResponseDto>("提交问卷失败");
        }
    }

    /// <summary>
    /// 检查是否已经提交过问卷
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>检查结果</returns>
    [HttpGet("check-submitted")]
    [DisplayName("检查提交状态")]
    public async Task<ActionResult<ApiResponse<object>>> CheckSubmitted(
        [FromQuery] int surveyId, 
        [FromQuery] string sessionId)
    {
        try
        {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return ErrorResponse<object>("会话ID不能为空");
        }

            var response = await _responseRepository.Find(r => 
                r.SurveyId == surveyId && 
                r.SessionId == sessionId &&
                r.Status == ResponseStatus.Completed)
                .FirstOrDefaultAsync();

            var result = new
            {
                SurveyId = surveyId,
                SessionId = sessionId,
                IsSubmitted = response != null,
                SubmittedAt = response?.CompletedAt,
                ResponseId = response?.Id
            };

            return SuccessResponse<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查提交状态失败，问卷ID: {SurveyId}, 会话ID: {SessionId}", 
                surveyId, sessionId);
            return ErrorResponse<object>("检查提交状态失败");
        }
    }

    /// <summary>
    /// 开始答题（创建进行中的回答记录）
    /// </summary>
    /// <param name="request">开始答题请求</param>
    /// <returns>回答记录ID</returns>
    [HttpPost("start")]
    [DisplayName("开始答题")]
    public async Task<ActionResult<ApiResponse<object>>> StartSurvey([FromBody] StartSurveyRequestDto request)
    {
        try
        {
            // 验证请求数据
            if (!ModelState.IsValid)
            {
                return ErrorResponse<object>("请求数据无效");
            }

            // 检查问卷是否存在且可以参与
            var survey = await _surveyRepository.Find(s => s.Id == request.SurveyId)
                .FirstOrDefaultAsync();

            if (survey == null)
            {
                return ErrorResponse<object>("问卷不存在");
            }

            if (survey.Status != SurveyStatus.Published || survey.AccessType != SurveyAccessType.Public)
            {
                return ErrorResponse<object>("问卷不可访问");
            }

            if (survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow)
            {
                return ErrorResponse<object>("问卷已过期");
            }

            // 检查是否已经有进行中的回答
            var existingResponse = await _responseRepository.Find(r => 
                r.SurveyId == request.SurveyId && 
                r.SessionId == request.SessionId)
                .FirstOrDefaultAsync();

            if (existingResponse != null)
            {
                if (existingResponse.Status == ResponseStatus.Completed)
                {
                    return ErrorResponse<object>("您已经完成了此问卷");
                }

                // 返回现有的进行中记录
                var existingResult = new
                {
                    ResponseId = existingResponse.Id,
                    SurveyId = existingResponse.SurveyId,
                    SessionId = existingResponse.SessionId,
                    StartedAt = existingResponse.StartedAt,
                    Status = "进行中"
                };

                return SuccessResponse<object>(existingResult);
            }

            // 获取客户端信息
            var ipAddress = _clientIpService.GetClientIpAddress(HttpContext);
            var userAgent = Request.Headers.UserAgent.ToString();

            // 创建新的进行中回答记录
            var response = new SurveyResponse
            {
                SurveyId = request.SurveyId,
                RespondentId = request.RespondentId,
                SessionId = request.SessionId,
                StartedAt = DateTime.UtcNow,
                Status = ResponseStatus.InProgress,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = request.DeviceFingerprint,
                Metadata = request.Metadata
            };

            await _responseRepository.AddAsync(response);

            _logger.LogInformation("开始答题，问卷ID: {SurveyId}, 会话ID: {SessionId}, 回答ID: {ResponseId}", 
                request.SurveyId, request.SessionId, response.Id);

            var result = new
            {
                ResponseId = response.Id,
                SurveyId = response.SurveyId,
                SessionId = response.SessionId,
                StartedAt = response.StartedAt,
                Status = "进行中"
            };

            return SuccessResponse<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始答题失败，问卷ID: {SurveyId}, 会话ID: {SessionId}", 
                request.SurveyId, request.SessionId);
            return ErrorResponse<object>("开始答题失败");
        }
    }
}

/// <summary>
/// 开始答题请求DTO
/// </summary>
[DisplayName("开始答题")]
public class StartSurveyRequestDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    [Required(ErrorMessage = "问卷ID不能为空")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [DisplayName("会话ID")]
    [Required(ErrorMessage = "会话ID不能为空")]
    [StringLength(50, ErrorMessage = "会话ID长度不能超过50个字符")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 答题者ID（可为空）
    /// </summary>
    [DisplayName("答题者ID")]
    [StringLength(50, ErrorMessage = "答题者ID长度不能超过50个字符")]
    public string? RespondentId { get; set; }

    /// <summary>
    /// 设备指纹
    /// </summary>
    [DisplayName("设备指纹")]
    [StringLength(100, ErrorMessage = "设备指纹长度不能超过100个字符")]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 元数据
    /// </summary>
    [DisplayName("元数据")]
    [StringLength(2000, ErrorMessage = "元数据长度不能超过2000个字符")]
    public string? Metadata { get; set; }
}
