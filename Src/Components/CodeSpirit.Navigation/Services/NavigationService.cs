using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.Navigation
{
    /// <summary>
    /// 站点导航服务实现（重构后）
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly INavigationTreeBuilder _treeBuilder;
        private readonly INavigationCacheManager _cacheManager;
        private readonly INavigationFilterService _filterService;
        private readonly ILogger<NavigationService> _logger;

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        /// <param name="treeBuilder">导航树构建器</param>
        /// <param name="cacheManager">缓存管理器</param>
        /// <param name="filterService">过滤服务</param>
        /// <param name="logger">日志记录器</param>
        public NavigationService(
            INavigationTreeBuilder treeBuilder,
            INavigationCacheManager cacheManager,
            INavigationFilterService filterService,
            ILogger<NavigationService> logger)
        {
            _treeBuilder = treeBuilder;
            _cacheManager = cacheManager;
            _filterService = filterService;
            _logger = logger;
        }

        /// <summary>
        /// 获取导航树（简化后）
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>导航节点列表</returns>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both)
        {
            try
            {
                // 1. 尝试从缓存获取完整导航树
                var cachedNodes = await _cacheManager.GetCachedNavigationAsync();

                if (cachedNodes == null)
                {
                    // 2. 构建导航树
                    cachedNodes = _treeBuilder.BuildNavigationTree();

                    // 3. 写入缓存
                    await _cacheManager.SetCachedNavigationAsync(cachedNodes);
                }

                // 4. 根据平台类型在内存中过滤
                var context = new NavigationFilterContext
                {
                    PlatformType = platformType
                };

                return _filterService.FilterNodes(cachedNodes, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation tree");
                return new List<NavigationNode>();
            }
        }

        /// <summary>
        /// 根据权限过滤导航节点（保持向后兼容）
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="hasPermissionService">权限服务</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPermission(List<NavigationNode> nodes, IHasPermissionService hasPermissionService)
        {
            var context = new NavigationFilterContext
            {
                PermissionService = hasPermissionService
            };

            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 根据平台类型过滤导航节点（保持向后兼容）
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="platformType">平台类型</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPlatform(List<NavigationNode> nodes, PlatformType platformType)
        {
            var context = new NavigationFilterContext
            {
                PlatformType = platformType
            };

            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 根据上下文过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByContext(List<NavigationNode> nodes, NavigationFilterContext context)
        {
            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 初始化导航树（简化后）
        /// </summary>
        public async Task InitializeNavigationTree()
        {
            _logger.LogInformation("Starting navigation tree initialization");

            try
            {
                // 1. 构建导航树
                var navigationTree = _treeBuilder.BuildNavigationTree();

                // 2. 写入缓存
                await _cacheManager.SetCachedNavigationAsync(navigationTree);

                _logger.LogInformation(
                    "Navigation tree initialization completed, {Count} modules",
                    navigationTree.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize navigation tree");
                throw;
            }
        }

        /// <summary>
        /// 清除指定模块的导航缓存（简化后）
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型，保留以保持API兼容性，但不再使用</param>
        public async Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null)
        {
            // platformType 参数保留以保持 API 兼容性，但不再使用
            await _cacheManager.ClearModuleCacheAsync(moduleName);
        }

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        public async Task ClearAllNavigationCacheAsync()
        {
            await _cacheManager.ClearAllCacheAsync();
        }
    }
}
