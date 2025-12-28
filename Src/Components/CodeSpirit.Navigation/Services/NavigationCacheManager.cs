using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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
        /// 计算导航树内容的SHA256哈希值
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <returns>16字符的Base64编码哈希值</returns>
        private string ComputeContentHash(List<NavigationNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return "empty";
            }

            try
            {
                var json = JsonConvert.SerializeObject(nodes, new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                var base64Hash = Convert.ToBase64String(hashBytes);
                // 取前16字符作为ETag（去除Base64填充字符）
                return base64Hash.Substring(0, Math.Min(16, base64Hash.Length)).Replace("=", "").Replace("+", "-").Replace("/", "_");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute content hash");
                // 如果计算失败，返回时间戳作为fallback
                return DateTime.UtcNow.Ticks.ToString("X").Substring(0, Math.Min(16, DateTime.UtcNow.Ticks.ToString("X").Length));
            }
        }

        /// <summary>
        /// 获取缓存的导航数据（包含版本）
        /// </summary>
        public async Task<NavigationCacheData> GetCachedNavigationDataAsync()
        {
            try
            {
                var cached = await _cache.GetAsync<NavigationCacheData>(NAVIGATION_CACHE_KEY);

                if (cached == null)
                {
                    _logger.LogDebug("Navigation cache data miss");
                }
                else
                {
                    _logger.LogDebug("Navigation cache data hit, version: {Version}, {Count} modules", 
                        cached.Version, cached.Nodes?.Count ?? 0);
                }

                return cached;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached navigation data");
                return null;
            }
        }

        /// <summary>
        /// 获取缓存的导航树（保持向后兼容）
        /// </summary>
        public async Task<List<NavigationNode>> GetCachedNavigationAsync()
        {
            try
            {
                var cacheData = await GetCachedNavigationDataAsync();
                
                // 如果新格式的缓存数据存在，返回其中的节点
                if (cacheData != null && cacheData.Nodes != null)
                {
                    _logger.LogDebug("Navigation cache hit (new format), {Count} modules", cacheData.Nodes.Count);
                    return cacheData.Nodes;
                }

                // 尝试读取旧格式的缓存（向后兼容）
                var oldCached = await _cache.GetAsync<List<NavigationNode>>(NAVIGATION_CACHE_KEY);
                if (oldCached != null)
                {
                    _logger.LogDebug("Navigation cache hit (old format), {Count} modules", oldCached.Count);
                    // 如果存在旧格式缓存，自动迁移到新格式
                    await SetCachedNavigationAsync(oldCached);
                    return oldCached;
                }

                _logger.LogDebug("Navigation cache miss");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached navigation");
                return null;
            }
        }

        /// <summary>
        /// 设置导航树缓存（自动计算版本号）
        /// </summary>
        public async Task SetCachedNavigationAsync(List<NavigationNode> nodes)
        {
            try
            {
                var version = ComputeContentHash(nodes);
                var cacheData = new NavigationCacheData
                {
                    Version = version,
                    UpdatedAt = DateTime.UtcNow,
                    Nodes = nodes
                };

                await _cache.SetAsync(NAVIGATION_CACHE_KEY, cacheData, _cacheOptions);
                _logger.LogInformation("Navigation cache updated, version: {Version}, {Count} modules", 
                    version, nodes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set navigation cache");
                throw;
            }
        }

        /// <summary>
        /// 获取当前缓存版本号
        /// </summary>
        public async Task<string> GetCurrentVersionAsync()
        {
            try
            {
                var cacheData = await GetCachedNavigationDataAsync();
                return cacheData?.Version;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current version");
                return null;
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
