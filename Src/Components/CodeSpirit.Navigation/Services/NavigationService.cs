using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSpirit.Core.Authorization;

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
        private readonly IConfiguration _configuration;

        private const string CACHE_KEY_PREFIX = "CodeSpirit:Navigation:Module:";
        private const string MODULE_NAMES_CACHE_KEY = "CodeSpirit:Navigation:ModuleNames";

        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
            SlidingExpiration = TimeSpan.FromDays(90)
        };

        public NavigationService(
            IActionDescriptorCollectionProvider actionProvider,
            IDistributedCache cache,
            ILogger<NavigationService> logger,
            IConfiguration configuration)
        {
            _actionProvider = actionProvider;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 获取导航树
        /// </summary>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync()
        {
            var allModuleNodes = new List<NavigationNode>();

            try
            {
                var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);

                if (moduleNames == null)
                {
                    _logger.LogWarning("No navigation modules found in cache");
                    return allModuleNodes;
                }

                foreach (var moduleName in moduleNames)
                {
                    try
                    {
                        var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
                        var moduleNodes = await _cache.GetAsync<List<NavigationNode>>(cacheKey);
                        if (moduleNodes != null)
                        {
                            allModuleNodes.AddRange(moduleNodes);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 优雅处理单个模块缓存异常，记录错误并继续处理下一个模块
                        _logger.LogError(ex, $"Failed to retrieve navigation for module '{moduleName}'. This module will be skipped.");
                    }
                }
            }
            catch (Exception ex)
            {
                // 优雅处理模块列表缓存异常
                _logger.LogError(ex, "Failed to retrieve navigation module list. Navigation will be empty.");
                return allModuleNodes;
            }

            return allModuleNodes;
        }

        /// <summary>
        /// 根据用户权限过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPermission(List<NavigationNode> nodes, IHasPermissionService hasPermissionService)
        {
            if (nodes == null || !nodes.Any())
            {
                return [];
            }

            if (hasPermissionService == null)
            {
                _logger.LogWarning("Permission service not available. Skipping permission filtering.");
                return nodes;
            }

            var result = new List<NavigationNode>();

            foreach (var node in nodes)
            {
                // 首先递归处理子节点
                var filteredChildren = FilterNodesByPermission(node.Children, hasPermissionService);
                
                // 检查节点自身权限或子节点是否有权限
                bool hasPermission = string.IsNullOrEmpty(node.Permission) || 
                                     hasPermissionService.HasPermission(node.Permission) || 
                                     filteredChildren.Any();

                if (hasPermission)
                {
                    // 创建节点副本，保留原始引用
                    var nodeCopy = node;
                    nodeCopy.Children = filteredChildren;
                    result.Add(nodeCopy);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns>服务提供者</returns>
        protected virtual IServiceProvider GetServiceProvider()
        {
            var accessor = new HttpContextAccessor();
            var httpContext = accessor.HttpContext;
            return httpContext?.RequestServices ?? null;
        }
    }
}
