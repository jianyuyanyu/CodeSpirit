using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation
{
    /// <summary>
    /// 站点导航服务实现
    /// </summary>
    public partial class NavigationService : INavigationService
    {
        private readonly IActionDescriptorCollectionProvider _actionProvider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<NavigationService> _logger;

        private const string CACHE_KEY_PREFIX = "CodeSpirit:Navigation:Module:";
        private const string MODULE_NAMES_CACHE_KEY = "CodeSpirit:Navigation:ModuleNames";

        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
            SlidingExpiration = TimeSpan.FromDays(1)
        };

        public NavigationService(
            IActionDescriptorCollectionProvider actionProvider,
            IDistributedCache cache,
            ILogger<NavigationService> logger)
        {
            _actionProvider = actionProvider;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取导航树
        /// </summary>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync()
        {
            var allModuleNodes = new List<NavigationNode>();
            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);

            if (moduleNames == null)
            {
                _logger.LogWarning("No navigation modules found in cache");
                return allModuleNodes;
            }

            foreach (var moduleName in moduleNames)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
                var moduleNodes = await _cache.GetAsync<List<NavigationNode>>(cacheKey);
                if (moduleNodes != null)
                {
                    allModuleNodes.AddRange(moduleNodes);
                }
            }

            return allModuleNodes;
        }
    }  
}
