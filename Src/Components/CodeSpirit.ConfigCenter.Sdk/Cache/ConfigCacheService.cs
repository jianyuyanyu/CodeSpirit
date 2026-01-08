using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ConfigCenter.Sdk.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Sdk.Cache;

/// <summary>
/// 配置缓存服务（Redis 封装）
/// </summary>
public class ConfigCacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConfigCenterOptions _options;
    private readonly ILogger<ConfigCacheService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCacheService(
        IServiceScopeFactory scopeFactory,
        IOptions<ConfigCenterOptions> options,
        ILogger<ConfigCacheService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存键
    /// </summary>
    private static string GetCacheKey(string appId) => $"configcenter:config:{appId}";

    /// <summary>
    /// 从缓存获取配置
    /// </summary>
    public async Task<ConfigItemsExportDto?> GetFromCacheAsync(string appId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            
            if (cacheService == null)
            {
                _logger.LogWarning("ICacheService 未注册，跳过缓存");
                return null;
            }

            var cacheKey = GetCacheKey(appId);
            var cached = await cacheService.GetAsync<ConfigItemsExportDto>(cacheKey, cancellationToken);
            
            if (cached != null)
            {
                _logger.LogDebug("从 Redis 缓存获取配置: {AppId}", appId);
            }
            
            return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从缓存获取配置失败: {AppId}", appId);
            return null;
        }
    }

    /// <summary>
    /// 保存配置到缓存
    /// </summary>
    public async Task SaveToCacheAsync(string appId, ConfigItemsExportDto configs, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            
            if (cacheService == null)
            {
                _logger.LogWarning("ICacheService 未注册，跳过缓存");
                return;
            }

            var cacheKey = GetCacheKey(appId);
            await cacheService.SetAsync(
                cacheKey,
                configs,
                CodeSpirit.Caching.Models.CacheOptions.L2Only(TimeSpan.FromMinutes(_options.CacheExpirationMinutes)),
                cancellationToken);
            
            _logger.LogDebug("已保存配置到 Redis 缓存: {AppId}", appId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存配置到缓存失败: {AppId}", appId);
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public async Task ClearCacheAsync(string appId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();
            
            if (cacheService == null)
            {
                _logger.LogWarning("ICacheService 未注册，跳过缓存清除");
                return;
            }

            var cacheKey = GetCacheKey(appId);
            await cacheService.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogDebug("已清除缓存: {AppId}", appId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清除缓存失败: {AppId}", appId);
        }
    }
}

