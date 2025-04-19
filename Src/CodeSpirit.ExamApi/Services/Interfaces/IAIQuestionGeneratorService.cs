using CodeSpirit.ExamApi.Dtos.Question;

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
    /// <returns>生成的题目列表</returns>
    Task<List<CreateQuestionDto>> GenerateQuestionsAsync(AIGenerateQuestionDto request);
} 