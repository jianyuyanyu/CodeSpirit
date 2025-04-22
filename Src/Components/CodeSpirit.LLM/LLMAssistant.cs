using CodeSpirit.LLM.Factories;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM;

/// <summary>
/// LLM助手，简化LLM服务的使用
/// </summary>
public class LLMAssistant
{
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly ILogger<LLMAssistant> _logger;

    /// <summary>
    /// 初始化LLM助手
    /// </summary>
    /// <param name="llmClientFactory">LLM客户端工厂</param>
    /// <param name="logger">日志记录器</param>
    public LLMAssistant(
        ILLMClientFactory llmClientFactory,
        ILogger<LLMAssistant> logger)
    {
        _llmClientFactory = llmClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(prompt);
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string prompt, int maxTokens)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(prompt, maxTokens);
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(systemPrompt, userPrompt);
    }

    /// <summary>
    /// 获取LLM客户端
    /// </summary>
    /// <returns>LLM客户端</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    private async Task<Clients.ILLMClient> GetClientAsync()
    {
        try
        {
            var client = await _llmClientFactory.CreateClientAsync();
            if (client == null)
            {
                _logger.LogError("无法创建LLM客户端，请检查LLM设置是否正确");
                throw new InvalidOperationException("无法创建LLM客户端，请检查LLM设置是否正确");
            }
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM客户端时发生错误: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("获取LLM客户端时发生错误", ex);
        }
    }
}
