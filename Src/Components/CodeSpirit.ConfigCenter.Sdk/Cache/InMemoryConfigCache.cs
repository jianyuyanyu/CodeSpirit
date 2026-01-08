using CodeSpirit.ConfigCenter.Sdk.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Sdk.Cache;

/// <summary>
/// 内存配置缓存服务
/// </summary>
public class InMemoryConfigCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConfigCenterOptions _options;
    private readonly ILogger<InMemoryConfigCache> _logger;
    private const string CacheKeyPrefix = "configcenter:memory:";

    /// <summary>
    /// 构造函数
    /// </summary>
    public InMemoryConfigCache(
        IMemoryCache memoryCache,
        IOptions<ConfigCenterOptions> options,
        ILogger<InMemoryConfigCache> logger)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
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
            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation("从内存缓存获取配置: AppId={AppId}, Version={Version}, ConfigCount={Count}", 
                    appId, cached?.Version, cached?.Configs?.Count ?? 0);
            }
            else
            {
                _logger.LogDebug("从内存缓存获取配置: {AppId}", appId);
            }
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
        
        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation("已保存配置到内存缓存: AppId={AppId}, Version={Version}, ConfigCount={Count}, Expiration={Expiration}分钟", 
                appId, configs.Version, configs.Configs?.Count ?? 0, (expiration ?? TimeSpan.FromMinutes(60)).TotalMinutes);
        }
        else
        {
            _logger.LogDebug("已保存配置到内存缓存: {AppId}", appId);
        }
    }

    /// <summary>
    /// 清除内存缓存
    /// </summary>
    public void ClearCache(string appId)
    {
        var cacheKey = GetCacheKey(appId);
        _memoryCache.Remove(cacheKey);
        
        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation("已清除内存缓存: AppId={AppId}", appId);
        }
        else
        {
            _logger.LogDebug("已清除内存缓存: {AppId}", appId);
        }
    }
}

