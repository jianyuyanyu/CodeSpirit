using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 应用健康状态服务接口
/// </summary>
public interface IAppHealthService : IScopedDependency
{
    /// <summary>
    /// 更新应用健康状态
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="isHealthy">是否健康</param>
    Task UpdateHealthStatusAsync(string appId, bool isHealthy = true);

    /// <summary>
    /// 获取应用健康状态
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>健康状态，null表示未知</returns>
    Task<bool?> GetHealthStatusAsync(string appId);

    /// <summary>
    /// 获取健康状态缓存键
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>缓存键</returns>
    string GetHealthStatusCacheKey(string appId);
}
