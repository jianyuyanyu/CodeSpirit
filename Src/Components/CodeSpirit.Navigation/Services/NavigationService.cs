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
using CodeSpirit.Core.Enums;

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
        /// <param name="platformType">平台类型</param>
        /// <returns>导航节点列表</returns>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both)
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
                        var cacheKey = GetModuleCacheKey(moduleName, platformType);
                        var moduleNodes = await _cache.GetAsync<List<NavigationNode>>(cacheKey);
                        if (moduleNodes != null)
                        {
                            allModuleNodes.AddRange(moduleNodes);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 优雅处理单个模块缓存异常，记录错误并继续处理下一个模块
                        _logger.LogError(ex, $"Failed to retrieve navigation for module '{moduleName}' on platform '{platformType}'. This module will be skipped.");
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
        /// <param name="hasPermissionService">权限服务</param>
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
                // 深拷贝节点，避免引用问题
                var nodeCopy = node.Clone();
                
                // 首先递归处理子节点
                var filteredChildren = FilterNodesByPermission(node.Children, hasPermissionService);
                
                // 检查节点自身权限或子节点是否有权限
                bool hasPermission = string.IsNullOrEmpty(node.Permission) || 
                                     hasPermissionService.HasNavigationPermission(node.Permission) || 
                                     filteredChildren.Any();

                if (hasPermission)
                {
                    // 设置过滤后的子节点集合
                    nodeCopy.Children = filteredChildren;
                    result.Add(nodeCopy);
                }
            }

            return result;
        }

        /// <summary>
        /// 根据平台类型过滤导航节点
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="platformType">平台类型</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPlatform(List<NavigationNode> nodes, PlatformType platformType)
        {
            if (nodes == null || !nodes.Any())
            {
                return [];
            }

            var result = new List<NavigationNode>();

            foreach (var node in nodes)
            {
                // 检查节点是否支持当前平台类型
                if ((node.PlatformType & platformType) != 0)
                {
                    var nodeCopy = node.Clone();
                    // 递归处理子节点
                    nodeCopy.Children = FilterNodesByPlatform(node.Children, platformType);
                    result.Add(nodeCopy);
                }
            }

            return result;
        }

        /// <summary>
        /// 根据上下文过滤导航节点（包含平台、权限、版本、设备等）
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByContext(List<NavigationNode> nodes, NavigationFilterContext context)
        {
            if (nodes == null || !nodes.Any())
            {
                return [];
            }

            var result = new List<NavigationNode>();

            foreach (var node in nodes)
            {
                var nodeCopy = node.Clone();
                
                // 递归处理子节点
                var filteredChildren = FilterNodesByContext(node.Children, context);
                
                // 检查各种过滤条件
                bool shouldInclude = true;

                // 平台类型过滤
                if ((node.PlatformType & context.PlatformType) == 0)
                {
                    shouldInclude = false;
                }

                // 认证过滤
                if (shouldInclude && node.RequireAuth && !context.IsAuthenticated)
                {
                    shouldInclude = false;
                }

                // 实验性功能过滤
                if (shouldInclude && node.IsExperimental && !context.IsDevelopment)
                {
                    shouldInclude = false;
                }

                // 版本过滤
                if (shouldInclude && !string.IsNullOrEmpty(context.CurrentVersion))
                {
                    if (!string.IsNullOrEmpty(node.MinVersion) && 
                        CompareVersions(context.CurrentVersion, node.MinVersion) < 0)
                    {
                        shouldInclude = false;
                    }

                    if (!string.IsNullOrEmpty(node.MaxVersion) && 
                        CompareVersions(context.CurrentVersion, node.MaxVersion) > 0)
                    {
                        shouldInclude = false;
                    }
                }

                // 设备类型过滤
                if (shouldInclude && !string.IsNullOrEmpty(context.DeviceType) && 
                    node.SupportedDevices?.Contains(context.DeviceType) == false)
                {
                    shouldInclude = false;
                }

                // 分组过滤
                if (shouldInclude && context.GroupFilter?.Length > 0)
                {
                    // 如果设置了分组过滤器，只包含匹配分组的节点
                    if (string.IsNullOrEmpty(node.Group) || !context.GroupFilter.Contains(node.Group))
                    {
                        shouldInclude = false;
                    }
                }

                // 标签过滤
                if (shouldInclude && context.UserTags?.Length > 0 && node.Tags?.Length > 0)
                {
                    // 检查是否有交集
                    if (!node.Tags.Intersect(context.UserTags).Any())
                    {
                        shouldInclude = false;
                    }
                }

                // 权限过滤
                if (shouldInclude && context.PermissionService != null && 
                    !string.IsNullOrEmpty(node.Permission) && 
                    !context.PermissionService.HasNavigationPermission(node.Permission))
                {
                    shouldInclude = false;
                }

                // 如果节点本身不满足条件，但有子节点满足条件，则包含该节点
                if (!shouldInclude && filteredChildren.Any())
                {
                    shouldInclude = true;
                }

                if (shouldInclude)
                {
                    nodeCopy.Children = filteredChildren;
                    result.Add(nodeCopy);
                }
            }

            // 按 Order 和 Priority 排序
            return result.OrderBy(n => n.Order).ThenByDescending(n => n.Priority).ToList();
        }

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        public async Task ClearAllNavigationCacheAsync()
        {
            try
            {
                var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);
                if (moduleNames != null)
                {
                    foreach (var moduleName in moduleNames)
                    {
                        await ClearModuleNavigationCacheAsync(moduleName);
                    }
                }

                await _cache.RemoveAsync(MODULE_NAMES_CACHE_KEY);
                _logger.LogInformation("Cleared all navigation cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all navigation cache");
                throw;
            }
        }

        /// <summary>
        /// 获取模块缓存键
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型</param>
        /// <returns>缓存键</returns>
        private string GetModuleCacheKey(string moduleName, PlatformType platformType)
        {
            return $"{CACHE_KEY_PREFIX}{moduleName}:{platformType}";
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        /// <param name="version1">版本1</param>
        /// <param name="version2">版本2</param>
        /// <returns>比较结果</returns>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
