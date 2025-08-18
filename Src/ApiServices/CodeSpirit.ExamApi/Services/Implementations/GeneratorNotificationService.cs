using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.Shared.Notifications;
using CodeSpirit.Shared.Notifications.Events;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 通知服务实现
/// </summary>
public class GeneratorNotificationService : IGeneratorNotificationService
{
    private readonly ISessionNotificationService _notificationService;
    private readonly ILogger<GeneratorNotificationService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="notificationService">通知服务</param>
    /// <param name="logger">日志记录器</param>
    public GeneratorNotificationService(
        ISessionNotificationService notificationService,
        ILogger<GeneratorNotificationService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 发送题目生成开始通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="request">生成请求</param>
    public async Task NotifyGenerationStartedAsync(string sessionId, AIGenerateQuestionDto request)
    {
        try
        {
            var message = new SessionNotificationEvent
            {
                Topic = "question-generation",
                Type = "started",
                SessionId = sessionId,
                Data = new Dictionary<string, object>
                {
                    { "request", request }
                }
            };

            await _notificationService.SendNotificationAsync(message);
            _logger.LogInformation("已发送题目生成开始通知: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送题目生成开始通知失败: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// 发送题目生成进度通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="stage">当前阶段</param>
    /// <param name="message">消息</param>
    /// <param name="percentage">完成百分比</param>
    public async Task NotifyGenerationProgressAsync(string sessionId, string stage, string message, int percentage)
    {
        try
        {
            var notification = new SessionNotificationEvent
            {
                Topic = "question-generation",
                Type = "progress",
                SessionId = sessionId,
                Data = new Dictionary<string, object>
                {
                    { "stage", stage },
                    { "message", message },
                    { "percentage", percentage }
                }
            };

            await _notificationService.SendNotificationAsync(notification);
            _logger.LogDebug("已发送题目生成进度通知: {SessionId}, {Stage}, {Percentage}%", sessionId, stage, percentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送题目生成进度通知失败: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// 发送题目生成完成通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="questions">生成的题目</param>
    /// <param name="duration">耗时(毫秒)</param>
    public async Task NotifyGenerationCompletedAsync(string sessionId, List<CreateQuestionDto> questions, long duration)
    {
        try
        {
            var notification = new SessionNotificationEvent
            {
                Topic = "question-generation",
                Type = "completed",
                SessionId = sessionId,
                Data = new Dictionary<string, object>
                {
                    { "questionCount", questions.Count },
                    { "duration", duration }
                }
            };

            await _notificationService.SendNotificationAsync(notification);
            _logger.LogInformation("已发送题目生成完成通知: {SessionId}, 生成了{Count}道题目, 耗时{Duration}ms", 
                sessionId, questions.Count, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送题目生成完成通知失败: {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// 发送题目生成错误通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="error">错误信息</param>
    public async Task NotifyGenerationErrorAsync(string sessionId, string error)
    {
        try
        {
            var notification = new SessionNotificationEvent
            {
                Topic = "question-generation",
                Type = "error",
                SessionId = sessionId,
                Data = new Dictionary<string, object>
                {
                    { "error", error }
                }
            };

            await _notificationService.SendNotificationAsync(notification);
            _logger.LogWarning("已发送题目生成错误通知: {SessionId}, {Error}", sessionId, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送题目生成错误通知失败: {SessionId}", sessionId);
        }
    }
} 