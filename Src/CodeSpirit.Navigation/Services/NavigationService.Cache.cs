using CodeSpirit.Core.Attributes;
using CodeSpirit.Navigation.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
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

            var currentModules = GetCurrentModules();
            var existingModules = await GetExistingModules();
            var allModules = MergeModules(currentModules, existingModules);

            await UpdateModuleCache(currentModules, allModules);

            _logger.LogInformation("Navigation tree initialization completed");
        }

        private List<string> GetCurrentModules()
        {
            return _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(x => x.ControllerTypeInfo)
                .Distinct()
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default")
                .Distinct()
                .ToList();
        }

        private async Task<List<string>> GetExistingModules()
        {
            return await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY) ?? [];
        }

        private List<string> MergeModules(List<string> currentModules, List<string> existingModules)
        {
            return existingModules.Union(currentModules).Distinct().ToList();
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
