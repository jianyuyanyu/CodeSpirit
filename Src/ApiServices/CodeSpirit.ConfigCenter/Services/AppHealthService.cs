using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 应用健康状态服务实现
/// </summary>
public class AppHealthService : IAppHealthService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<AppHealthService> _logger;

    /// <summary>
    /// 健康状态缓存键前缀
    /// </summary>
    private const string HealthStatusCacheKeyPrefix = "configcenter:health:";

    /// <summary>
    /// 健康状态缓存选项：缓存2分钟，使用分布式缓存
    /// </summary>
    private static readonly CacheOptions HealthStatusCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        Level = CacheLevel.L2Only // 使用分布式缓存，多实例共享
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    public AppHealthService(
        ICacheService cacheService,
        ILogger<AppHealthService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// 更新应用健康状态
    /// </summary>
    public async Task UpdateHealthStatusAsync(string appId, bool isHealthy = true)
    {
        try
        {
            var cacheKey = GetHealthStatusCacheKey(appId);
            await _cacheService.SetAsync(cacheKey, isHealthy, HealthStatusCacheOptions);

            _logger.LogDebug("已更新应用 {AppId} 健康状态: {IsHealthy}", appId, isHealthy ? "健康" : "不健康");
        }
        catch (Exception ex)
        {
            // 健康状态更新失败不应该影响主流程
            _logger.LogWarning(ex, "更新应用 {AppId} 健康状态失败", appId);
        }
    }

    /// <summary>
    /// 获取应用健康状态
    /// </summary>
    public async Task<bool?> GetHealthStatusAsync(string appId)
    {
        try
        {
            var cacheKey = GetHealthStatusCacheKey(appId);
            return await _cacheService.GetAsync<bool?>(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取应用 {AppId} 健康状态失败", appId);
            return null;
        }
    }

    /// <summary>
    /// 获取健康状态缓存键
    /// </summary>
    public string GetHealthStatusCacheKey(string appId)
    {
        return $"{HealthStatusCacheKeyPrefix}{appId}";
    }
}
