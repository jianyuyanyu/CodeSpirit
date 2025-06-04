using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation
{
    public partial class NavigationService
    {
        /// <summary>
        /// 初始化导航树
        /// </summary>
        public async Task InitializeNavigationTree()
        {
            _logger.LogInformation("Starting navigation tree initialization");

            // 获取代码中定义的模块
            var currentModules = GetCurrentModules();

            var existingModules = await GetExistingModules();

            // 获取配置文件中定义的模块
            var configModules = GetConfigModules();

            // 合并模块列表
            var allModules = currentModules.Union(existingModules).Union(configModules).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

            // 更新模块列表缓存
            await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, allModules, _cacheOptions);

            // 为每个平台类型构建导航树缓存
            var systemModules = new List<NavigationNode>();
            var tenantModules = new List<NavigationNode>();
            var bothModules = new List<NavigationNode>();

            // 构建每个模块的导航树并根据平台类型分类
            foreach (var moduleName in allModules)
            {
                var moduleNavigation = BuildModuleNavigationTree(moduleName);
                if (!moduleNavigation.Any())
                {
                    continue;
                }

                var moduleNode = moduleNavigation[0];
                var modulePlatformType = moduleNode.PlatformType;
                _logger.LogInformation($"模块 {moduleName} 的平台类型: {modulePlatformType}");

                // 确保模块节点的ModuleName属性被正确设置
                if (string.IsNullOrEmpty(moduleNode.ModuleName))
                {
                    moduleNode.ModuleName = moduleName;
                }

                // 根据模块的实际平台类型将其分配到对应的缓存中
                switch (modulePlatformType)
                {
                    case PlatformType.System:
                        systemModules.Add(moduleNode);
                        break;
                    case PlatformType.Tenant:
                        tenantModules.Add(moduleNode);
                        break;
                    case PlatformType.Both:
                        systemModules.Add(moduleNode);
                        tenantModules.Add(moduleNode);
                        bothModules.Add(moduleNode);
                        break;
                    default:
                        _logger.LogWarning($"模块 {moduleName} 的平台类型为 {modulePlatformType}，跳过缓存创建");
                        continue;
                }
            }

            // 创建各平台的导航缓存
            if (systemModules.Any())
            {
                await SetPlatformNavigationCache(PlatformType.System, systemModules);
                _logger.LogInformation($"System平台缓存包含 {systemModules.Count} 个模块: [{string.Join(", ", systemModules.Select(m => m.Name))}]");
            }

            if (tenantModules.Any())
            {
                await SetPlatformNavigationCache(PlatformType.Tenant, tenantModules);
                _logger.LogInformation($"Tenant平台缓存包含 {tenantModules.Count} 个模块: [{string.Join(", ", tenantModules.Select(m => m.Name))}]");
            }

            if (bothModules.Any())
            {
                await SetPlatformNavigationCache(PlatformType.Both, bothModules);
                _logger.LogInformation($"Both平台缓存包含 {bothModules.Count} 个模块: [{string.Join(", ", bothModules.Select(m => m.Name))}]");
            }

            _logger.LogInformation("Navigation tree initialization completed");
        }

        /// <summary>
        /// 设置平台导航缓存
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <param name="modules">模块列表</param>
        private async Task SetPlatformNavigationCache(PlatformType platformType, List<NavigationNode> modules)
        {
            var tasks = modules.Select(async module =>
            {
                var cacheKey = GetModuleCacheKey(module.ModuleName, platformType);
                // 直接存储模块，不需要再次过滤，因为我们在分类时已经确保了平台类型的正确性
                await _cache.SetAsync(cacheKey, new List<NavigationNode> { module }, _cacheOptions);
                _logger.LogDebug($"设置缓存 {cacheKey}: 包含模块 {module.ModuleName}");
            });

            await Task.WhenAll(tasks);
        }

        protected virtual List<string> GetCurrentModules()
        {
            return _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(x => x.ControllerTypeInfo)
                .Distinct()
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 获取配置文件中定义的模块
        /// </summary>
        protected virtual List<string> GetConfigModules()
        {
            var configSection = _configuration.GetSection(CONFIG_SECTION_KEY);
            if (!configSection.Exists())
            {
                return [];
            }

            // 获取配置节下的所有子节点名称，这些就是模块名
            return configSection.GetChildren()
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        private async Task<List<string>> GetExistingModules()
        {
            return await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY) ?? [];
        }

        /// <summary>
        /// 更新模块导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型</param>
        private async Task UpdateModuleNavigationCache(string moduleName, PlatformType platformType)
        {
            var cacheKey = GetModuleCacheKey(moduleName, platformType);

            // 构建模块导航树（包含代码定义和配置文件的合并逻辑）
            var moduleNavigation = BuildModuleNavigationTree(moduleName);

            // 如果没有任何导航信息，则返回
            if (!moduleNavigation.Any())
            {
                return;
            }

            // 根据平台类型过滤导航
            var filteredNavigation = FilterNodesByPlatform(moduleNavigation, platformType);

            if (!filteredNavigation.Any())
            {
                return;
            }

            var existingNavigation = await _cache.GetAsync<List<NavigationNode>>(cacheKey);
            if (existingNavigation != null)
            {
                MergeNavigationNodes(existingNavigation[0], filteredNavigation[0]);
                filteredNavigation = existingNavigation;
            }

            await _cache.SetAsync(cacheKey, filteredNavigation, _cacheOptions);
        }

        /// <summary>
        /// 清除指定模块的导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型，null表示清除所有平台缓存</param>
        public async Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null)
        {
            if (platformType.HasValue)
            {
                // 清除指定平台的缓存
                var cacheKey = GetModuleCacheKey(moduleName, platformType.Value);
                await _cache.RemoveAsync(cacheKey);
                _logger.LogInformation($"Cleared navigation cache for module: {moduleName}, platform: {platformType.Value}");
            }
            else
            {
                // 清除所有平台的缓存
                var systemCacheKey = GetModuleCacheKey(moduleName, PlatformType.System);
                var tenantCacheKey = GetModuleCacheKey(moduleName, PlatformType.Tenant);
                var bothCacheKey = GetModuleCacheKey(moduleName, PlatformType.Both);

                await Task.WhenAll(
                    _cache.RemoveAsync(systemCacheKey),
                    _cache.RemoveAsync(tenantCacheKey),
                    _cache.RemoveAsync(bothCacheKey)
                );

                _logger.LogInformation($"Cleared navigation cache for module: {moduleName} (all platforms)");
            }

            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);
            if (moduleNames != null)
            {
                moduleNames.Remove(moduleName);
                await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, moduleNames, _cacheOptions);
            }
        }
    }
}
