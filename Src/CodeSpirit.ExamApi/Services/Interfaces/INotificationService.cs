using CodeSpirit.ExamApi.Dtos.Question;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送题目生成开始通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="request">生成请求</param>
    Task NotifyGenerationStartedAsync(string sessionId, AIGenerateQuestionDto request);

    /// <summary>
    /// 发送题目生成进度通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="stage">当前阶段</param>
    /// <param name="message">消息</param>
    /// <param name="percentage">完成百分比</param>
    Task NotifyGenerationProgressAsync(string sessionId, string stage, string message, int percentage);

    /// <summary>
    /// 发送题目生成完成通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="questions">生成的题目</param>
    /// <param name="duration">耗时(毫秒)</param>
    Task NotifyGenerationCompletedAsync(string sessionId, List<CreateQuestionDto> questions, long duration);

    /// <summary>
    /// 发送题目生成错误通知
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="error">错误信息</param>
    Task NotifyGenerationErrorAsync(string sessionId, string error);
} 