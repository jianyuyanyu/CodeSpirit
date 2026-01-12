using CodeSpirit.Caching.Models;

namespace CodeSpirit.Caching.Abstractions;

/// <summary>
/// 统一缓存服务接口
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，如果不存在则返回默认值</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取缓存值（带缓存选项）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存选项，用于控制缓存级别（如 L2Only 时不回填 L1 缓存）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，如果不存在则返回默认值</returns>
    Task<T?> GetAsync<T>(string key, CacheOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取缓存值，如果不存在则通过工厂方法创建并缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetAsync<T>(string key, T value, CacheOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步按模式移除缓存项
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步刷新缓存项的过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}
