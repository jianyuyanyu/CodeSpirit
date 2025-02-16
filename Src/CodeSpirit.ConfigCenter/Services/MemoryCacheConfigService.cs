using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 内存缓存配置服务实现
/// </summary>
public class MemoryCacheConfigService : IConfigCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheConfigService> _logger;

    public MemoryCacheConfigService(
        IMemoryCache cache,
        ILogger<MemoryCacheConfigService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<string> GetAsync(string key)
    {
        return Task.FromResult(_cache.Get<string>(key));
    }

    public Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        _cache.Set(key, value, options);
        _logger.LogDebug("Set cache: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Remove cache: {Key}", key);
        return Task.CompletedTask;
    }

    public Task ClearAppConfigsAsync(string appId, string environment)
    {
        // 由于是内存缓存，这里简化处理
        _logger.LogInformation("Clear app configs: {AppId}/{Environment}", appId, environment);
        return Task.CompletedTask;
    }
} 