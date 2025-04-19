using CodeSpirit.ExamApi.Dtos.Question;

namespace CodeSpirit.ExamApi.Services.Helpers;

/// <summary>
/// 提示词构建器接口
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// 构建生成题目的提示词
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>提示词</returns>
    string BuildGenerationPrompt(AIGenerateQuestionDto request);
    
    /// <summary>
    /// 构建格式修正的提示词
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>提示词</returns>
    string BuildCorrectionPrompt(AIGenerateQuestionDto request);
} 