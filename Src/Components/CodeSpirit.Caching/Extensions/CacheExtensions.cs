using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Keys;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;

namespace CodeSpirit.Caching.Extensions;

/// <summary>
/// 缓存操作扩展方法
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 获取或设置缓存值（同步版本）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <returns>缓存值</returns>
    public static T GetOrSet<T>(this ICacheService cacheService, string key, Func<T> factory, CacheOptions? options = null)
    {
        return cacheService.GetOrSetAsync(key, () => Task.FromResult(factory()), options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取缓存值（同步版本）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值</returns>
    public static T? Get<T>(this ICacheService cacheService, string key)
    {
        return cacheService.GetAsync<T>(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 设置缓存值（同步版本）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存选项</param>
    public static void Set<T>(this ICacheService cacheService, string key, T value, CacheOptions? options = null)
    {
        cacheService.SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 移除缓存项（同步版本）
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    public static void Remove(this ICacheService cacheService, string key)
    {
        cacheService.RemoveAsync(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 检查缓存项是否存在（同步版本）
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    public static bool Exists(this ICacheService cacheService, string key)
    {
        return cacheService.ExistsAsync(key).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取或设置缓存值，带过期时间
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="level">缓存级别</param>
    /// <returns>缓存值</returns>
    public static async Task<T> GetOrSetAsync<T>(this ICacheService cacheService, string key, Func<Task<T>> factory, TimeSpan expiration, CacheLevel level = CacheLevel.Both)
    {
        var options = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Level = level
        };

        return await cacheService.GetOrSetAsync(key, factory, options);
    }

    /// <summary>
    /// 设置缓存值，带过期时间
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="level">缓存级别</param>
    public static async Task SetAsync<T>(this ICacheService cacheService, string key, T value, TimeSpan expiration, CacheLevel level = CacheLevel.Both)
    {
        var options = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Level = level
        };

        await cacheService.SetAsync(key, value, options);
    }

    /// <summary>
    /// 获取或设置缓存值，仅使用L1缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>缓存值</returns>
    public static async Task<T> GetOrSetL1Async<T>(this ICacheService cacheService, string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var options = CacheOptions.L1Only(expiration);
        return await cacheService.GetOrSetAsync(key, factory, options);
    }

    /// <summary>
    /// 获取或设置缓存值，仅使用L2缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>缓存值</returns>
    public static async Task<T> GetOrSetL2Async<T>(this ICacheService cacheService, string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var options = CacheOptions.L2Only(expiration);
        return await cacheService.GetOrSetAsync(key, factory, options);
    }

    /// <summary>
    /// 批量获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="keys">缓存键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键值对字典</returns>
    public static async Task<Dictionary<string, T?>> GetManyAsync<T>(this ICacheService cacheService, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return await GetManyAsync<T>(cacheService, keys, CacheOptions.Default, cancellationToken);
    }

    /// <summary>
    /// 批量获取缓存值（带缓存选项）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="keys">缓存键集合</param>
    /// <param name="options">缓存选项，用于控制缓存级别（如 L2Only 时不回填 L1 缓存）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>键值对字典</returns>
    public static async Task<Dictionary<string, T?>> GetManyAsync<T>(this ICacheService cacheService, IEnumerable<string> keys, CacheOptions options, CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        var tasks = keyList.Select(key => cacheService.GetAsync<T>(key, options, cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks);

        var dictionary = new Dictionary<string, T?>();
        for (int i = 0; i < keyList.Count; i++)
        {
            dictionary[keyList[i]] = results[i];
        }

        return dictionary;
    }

    /// <summary>
    /// 批量设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="items">键值对字典</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task SetManyAsync<T>(this ICacheService cacheService, Dictionary<string, T> items, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        var tasks = items.Select(kvp => cacheService.SetAsync(kvp.Key, kvp.Value, options, cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 批量移除缓存项
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="keys">缓存键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task RemoveManyAsync(this ICacheService cacheService, IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Select(key => cacheService.RemoveAsync(key, cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 获取或设置缓存值，带标签
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="tags">缓存标签</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>缓存值</returns>
    public static async Task<T> GetOrSetWithTagsAsync<T>(this ICacheService cacheService, string key, Func<Task<T>> factory, string[] tags, TimeSpan? expiration = null)
    {
        var options = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Tags = new HashSet<string>(tags)
        };

        return await cacheService.GetOrSetAsync(key, factory, options);
    }

    /// <summary>
    /// 创建带前缀的缓存键
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="prefix">前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>完整的缓存键</returns>
    public static string CreateKey(this ICacheService cacheService, string prefix, params object[] parts)
    {
        if (cacheService is MultiLevelCacheService multiLevelService)
        {
            // 通过反射获取键生成器（这不是最佳实践，但为了扩展方法的便利性）
            var keyGeneratorField = typeof(MultiLevelCacheService).GetField("_keyGenerator", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (keyGeneratorField?.GetValue(multiLevelService) is ICacheKeyGenerator keyGenerator)
            {
                return keyGenerator.GenerateKey(prefix, parts);
            }
        }

        // 回退到简单的键生成
        var keyParts = new List<string> { prefix };
        keyParts.AddRange(parts.Select(p => p?.ToString() ?? string.Empty));
        return string.Join(":", keyParts);
    }

    /// <summary>
    /// 创建用户特定的缓存键
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="userId">用户ID</param>
    /// <param name="prefix">前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>用户特定的缓存键</returns>
    public static string CreateUserKey(this ICacheService cacheService, long userId, string prefix, params object[] parts)
    {
        var allParts = new List<object> { $"user:{userId}" };
        allParts.AddRange(parts);
        return cacheService.CreateKey(prefix, allParts.ToArray());
    }

    /// <summary>
    /// 创建租户特定的缓存键
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="prefix">前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>租户特定的缓存键</returns>
    public static string CreateTenantKey(this ICacheService cacheService, string tenantId, string prefix, params object[] parts)
    {
        var allParts = new List<object> { $"tenant:{tenantId}" };
        allParts.AddRange(parts);
        return cacheService.CreateKey(prefix, allParts.ToArray());
    }

    #region 强类型缓存键扩展方法

    /// <summary>
    /// 使用强类型键获取缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">强类型缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值</returns>
    public static Task<T?> GetAsync<T>(
        this ICacheService cacheService,
        ICacheKey<T> key,
        CancellationToken cancellationToken = default)
    {
        return cacheService.GetAsync<T>(key.Key, cancellationToken);
    }

    /// <summary>
    /// 使用强类型键获取或设置缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">强类型缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值</returns>
    public static Task<T> GetOrSetAsync<T>(
        this ICacheService cacheService,
        ICacheKey<T> key,
        Func<Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var options = key.Options;
        if (key.Tags.Any())
        {
            options.Tags.UnionWith(key.Tags);
        }
        return cacheService.GetOrSetAsync(key.Key, factory, options, cancellationToken);
    }

    /// <summary>
    /// 使用强类型键设置缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">强类型缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task SetAsync<T>(
        this ICacheService cacheService,
        ICacheKey<T> key,
        T value,
        CancellationToken cancellationToken = default)
    {
        var options = key.Options;
        if (key.Tags.Any())
        {
            options.Tags.UnionWith(key.Tags);
        }
        return cacheService.SetAsync(key.Key, value, options, cancellationToken);
    }

    /// <summary>
    /// 使用强类型键删除缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="key">强类型缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task RemoveAsync<T>(
        this ICacheService cacheService,
        ICacheKey<T> key,
        CancellationToken cancellationToken = default)
    {
        return cacheService.RemoveAsync(key.Key, cancellationToken);
    }

    #endregion
}
