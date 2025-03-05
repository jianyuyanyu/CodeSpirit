using CodeSpirit.Core.Attributes;
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

            // 更新每个模块的导航缓存
            foreach (var moduleName in allModules)
            {
                await UpdateModuleNavigationCache(moduleName);
            }

            _logger.LogInformation("Navigation tree initialization completed");
        }

        private List<string> GetCurrentModules()
        {
            return _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(x => x.ControllerTypeInfo)
                .Distinct()
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 获取配置文件中定义的模块
        /// </summary>
        private List<string> GetConfigModules()
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

        private async Task UpdateModuleCache(List<string> currentModules, List<string> allModules)
        {
            await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, allModules, _cacheOptions);

            foreach (var moduleName in currentModules)
            {
                await UpdateModuleNavigationCache(moduleName);
            }
        }

        /// <summary>
        /// 更新模块导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        private async Task UpdateModuleNavigationCache(string moduleName)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";

            // 构建模块导航树（包含代码定义和配置文件的合并逻辑）
            var moduleNavigation = BuildModuleNavigationTree(moduleName);

            // 如果没有任何导航信息，则返回
            if (!moduleNavigation.Any())
            {
                return;
            }

            var existingNavigation = await _cache.GetAsync<List<NavigationNode>>(cacheKey);
            if (existingNavigation != null)
            {
                MergeNavigationNodes(existingNavigation[0], moduleNavigation[0]);
                moduleNavigation = existingNavigation;
            }

            await _cache.SetAsync(cacheKey, moduleNavigation, _cacheOptions);
        }

        /// <summary>
        /// 清除指定模块的导航缓存
        /// </summary>
        public async Task ClearModuleNavigationCacheAsync(string moduleName)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
            await _cache.RemoveAsync(cacheKey);

            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);
            if (moduleNames != null)
            {
                moduleNames.Remove(moduleName);
                await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, moduleNames, _cacheOptions);
            }
        }
    }
}
