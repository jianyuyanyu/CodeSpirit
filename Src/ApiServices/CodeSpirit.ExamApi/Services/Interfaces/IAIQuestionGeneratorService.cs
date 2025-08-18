using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Interfaces;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// AI题目生成服务接口
/// </summary>
public interface IAIQuestionGeneratorService
{
    /// <summary>
    /// 使用AI生成题目
    /// </summary>
    /// <param name="request">生成题目的参数</param>
    /// <param name="sessionId">会话ID，用于实时推送生成过程</param>
    /// <param name="notificationService">通知服务，用于实时推送生成过程</param>
    /// <returns>生成的题目列表</returns>
    Task<List<CreateQuestionDto>> GenerateQuestionsAsync(AIGenerateQuestionDto request, string sessionId = null, IGeneratorNotificationService notificationService = null);
} 