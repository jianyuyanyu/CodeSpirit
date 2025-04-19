using CodeSpirit.ExamApi.Settings;

namespace CodeSpirit.ExamApi.Services.LLM;

/// <summary>
/// LLM客户端接口
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// 获取当前LLM设置
    /// </summary>
    LLMSettings Settings { get; }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>生成的内容</returns>
    Task<string> GenerateContentAsync(string prompt);

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <returns>生成的内容</returns>
    Task<string> GenerateContentAsync(string prompt, int maxTokens);

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <returns>生成的内容</returns>
    Task<string> GenerateContentAsync(string systemPrompt, string userPrompt);
} 