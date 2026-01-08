using CodeSpirit.ConfigCenter.Sdk.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Sdk.Cache;

/// <summary>
/// 内存配置缓存服务
/// </summary>
public class InMemoryConfigCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryConfigCache> _logger;
    private const string CacheKeyPrefix = "configcenter:memory:";

    /// <summary>
    /// 构造函数
    /// </summary>
    public InMemoryConfigCache(
        IMemoryCache memoryCache,
        ILogger<InMemoryConfigCache> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存键
    /// </summary>
    private static string GetCacheKey(string appId) => $"{CacheKeyPrefix}{appId}";

    /// <summary>
    /// 从内存缓存获取配置
    /// </summary>
    public ConfigItemsExportDto? GetFromCache(string appId)
    {
        var cacheKey = GetCacheKey(appId);
        if (_memoryCache.TryGetValue(cacheKey, out ConfigItemsExportDto? cached))
        {
            _logger.LogDebug("从内存缓存获取配置: {AppId}", appId);
            return cached;
        }
        return null;
    }

    /// <summary>
    /// 保存配置到内存缓存
    /// </summary>
    public void SaveToCache(string appId, ConfigItemsExportDto configs, TimeSpan? expiration = null)
    {
        var cacheKey = GetCacheKey(appId);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(60),
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(cacheKey, configs, options);
        _logger.LogDebug("已保存配置到内存缓存: {AppId}", appId);
    }

    /// <summary>
    /// 清除内存缓存
    /// </summary>
    public void ClearCache(string appId)
    {
        var cacheKey = GetCacheKey(appId);
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("已清除内存缓存: {AppId}", appId);
    }
}

