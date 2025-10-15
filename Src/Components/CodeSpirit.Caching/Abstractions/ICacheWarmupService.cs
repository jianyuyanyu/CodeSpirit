using CodeSpirit.Caching.Models;

namespace CodeSpirit.Caching.Abstractions;

/// <summary>
/// 缓存预热服务接口
/// </summary>
public interface ICacheWarmupService
{
    /// <summary>
    /// 异步预热单个缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WarmupAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步批量预热缓存项
    /// </summary>
    /// <param name="items">预热项集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WarmupBatchAsync(IEnumerable<CacheWarmupItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步预热缓存项（带参数）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <typeparam name="TParam">参数类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">带参数的值工厂方法</param>
    /// <param name="parameter">工厂方法参数</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WarmupAsync<T, TParam>(string key, Func<TParam, Task<T>> factory, TParam parameter, CacheOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查预热状态
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热状态信息</returns>
    Task<CacheWarmupStatus> GetWarmupStatusAsync(string key, CancellationToken cancellationToken = default);
}
