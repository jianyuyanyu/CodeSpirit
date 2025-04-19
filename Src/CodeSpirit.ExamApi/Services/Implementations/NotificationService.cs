using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Hubs;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<QuestionGenerationHub> _questionGenerationHubContext;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="questionGenerationHubContext">题目生成Hub上下文</param>
    /// <param name="logger">日志记录器</param>
    public NotificationService(
        IHubContext<QuestionGenerationHub> questionGenerationHubContext,
        ILogger<NotificationService> logger)
    {
        _questionGenerationHubContext = questionGenerationHubContext;
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
            await _questionGenerationHubContext.Clients.Group(sessionId).SendAsync(
                "GenerationStarted",
                new
                {
                    sessionId,
                    request.Topic,
                    request.Count,
                    request.Type,
                    request.Difficulty,
                    request.CategoryId,
                    request.Requirements,
                    timestamp = DateTime.UtcNow
                });
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
            await _questionGenerationHubContext.Clients.Group(sessionId).SendAsync(
                "GenerationProgress",
                new
                {
                    sessionId,
                    stage,
                    message,
                    percentage,
                    timestamp = DateTime.UtcNow
                });
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
            await _questionGenerationHubContext.Clients.Group(sessionId).SendAsync(
                "GenerationCompleted",
                new
                {
                    sessionId,
                    questionCount = questions.Count,
                    duration,
                    timestamp = DateTime.UtcNow
                });
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
            await _questionGenerationHubContext.Clients.Group(sessionId).SendAsync(
                "GenerationError",
                new
                {
                    sessionId,
                    error,
                    timestamp = DateTime.UtcNow
                });
            _logger.LogWarning("已发送题目生成错误通知: {SessionId}, {Error}", sessionId, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送题目生成错误通知失败: {SessionId}", sessionId);
        }
    }
} 