using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航缓存管理器实现
    /// </summary>
    public class NavigationCacheManager : INavigationCacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<NavigationCacheManager> _logger;

        // 简化后的缓存键：单一缓存键，不再按平台类型分离
        private const string NAVIGATION_CACHE_KEY = "CodeSpirit:Navigation:All";

        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
            SlidingExpiration = TimeSpan.FromDays(90)
        };

        /// <summary>
        /// 初始化导航缓存管理器
        /// </summary>
        /// <param name="cache">分布式缓存</param>
        /// <param name="logger">日志记录器</param>
        public NavigationCacheManager(
            IDistributedCache cache,
            ILogger<NavigationCacheManager> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取缓存的导航树
        /// </summary>
        public async Task<List<NavigationNode>> GetCachedNavigationAsync()
        {
            try
            {
                var cached = await _cache.GetAsync<List<NavigationNode>>(NAVIGATION_CACHE_KEY);

                if (cached == null)
                {
                    _logger.LogDebug("Navigation cache miss");
                }
                else
                {
                    _logger.LogDebug("Navigation cache hit, {Count} modules", cached.Count);
                }

                return cached;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached navigation");
                return null;
            }
        }

        /// <summary>
        /// 设置导航树缓存
        /// </summary>
        public async Task SetCachedNavigationAsync(List<NavigationNode> nodes)
        {
            try
            {
                await _cache.SetAsync(NAVIGATION_CACHE_KEY, nodes, _cacheOptions);
                _logger.LogInformation("Navigation cache set, {Count} modules", nodes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set navigation cache");
                throw;
            }
        }

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        public async Task ClearAllCacheAsync()
        {
            try
            {
                await _cache.RemoveAsync(NAVIGATION_CACHE_KEY);
                _logger.LogInformation("Navigation cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear navigation cache");
                throw;
            }
        }

        /// <summary>
        /// 清除指定模块的缓存（简化后：清除整个缓存，下次访问时会自动重建）
        /// </summary>
        public async Task ClearModuleCacheAsync(string moduleName)
        {
            // 简化后的缓存策略：清除整个缓存
            // 下次访问时会自动重建
            await ClearAllCacheAsync();
            _logger.LogInformation("Cleared cache for module: {ModuleName}", moduleName);
        }
    }
}
