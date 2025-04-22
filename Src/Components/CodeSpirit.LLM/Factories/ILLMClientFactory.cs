using CodeSpirit.LLM.Clients;

namespace CodeSpirit.LLM.Factories;

/// <summary>
/// LLM客户端工厂接口
/// </summary>
public interface ILLMClientFactory
{
    /// <summary>
    /// 创建LLM客户端
    /// </summary>
    /// <returns>LLM客户端</returns>
    Task<ILLMClient?> CreateClientAsync();

    /// <summary>
    /// 创建LLM客户端
    /// </summary>
    /// <param name="forceRefreshSettings">是否强制刷新设置</param>
    /// <returns>LLM客户端</returns>
    Task<ILLMClient?> CreateClientAsync(bool forceRefreshSettings);
}
