using CodeSpirit.SurveyApi.Dtos.Survey;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷LLM生成服务接口
/// </summary>
public interface ISurveyLLMGeneratorService
{
    /// <summary>
    /// 根据主题生成问卷字段建议
    /// </summary>
    /// <param name="topic">问卷主题</param>
    /// <returns>字段建议</returns>
    Task<SurveyFieldSuggestions> GenerateFieldSuggestionsAsync(string topic);

    /// <summary>
    /// 根据主题生成问卷
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成的问卷</returns>
    Task<GeneratedSurveyDto> GenerateSurveyAsync(GenerateSurveyRequest request);

    /// <summary>
    /// 优化现有问卷
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="optimizationGoals">优化目标</param>
    /// <returns>优化建议</returns>
    Task<SurveyOptimizationResult> OptimizeSurveyAsync(int surveyId, string optimizationGoals);

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="surveyDto">问卷信息</param>
    /// <returns>洞察分析结果</returns>
    Task<SurveyInsightResult> GenerateInsightsAsync(SurveyDto surveyDto);

    /// <summary>
    /// 验证提示词长度
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>验证结果</returns>
    Task<PromptValidationResult> ValidatePromptAsync(string prompt);

    /// <summary>
    /// 压缩提示词
    /// </summary>
    /// <param name="prompt">原始提示词</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>压缩后的提示词</returns>
    Task<string> CompressPromptAsync(string prompt, int maxLength);
}


