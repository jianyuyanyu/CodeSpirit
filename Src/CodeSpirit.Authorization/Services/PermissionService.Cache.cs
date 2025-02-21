using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    public partial class PermissionService
    {
        /// <summary>
        /// 初始化权限树
        /// </summary>
        public async Task InitializePermissionTree()
        {
            _logger.LogInformation("Starting permission tree initialization");

            var currentModules = GetCurrentModules();
            var existingModules = await GetExistingModules();
            var allModules = MergeModules(currentModules, existingModules);

            await UpdateModuleCache(currentModules, allModules);

            _logger.LogInformation("Permission tree initialization completed");
        }

        /// <summary>
        /// 获取当前服务中的所有模块
        /// </summary>
        /// <returns>模块名称列表</returns>
        private List<string> GetCurrentModules()
        {
            return GetControllers()
                .Where(c => !IsAnonymousController(c))
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default")
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 从缓存中获取已存在的模块列表
        /// </summary>
        /// <returns>模块名称列表</returns>
        private async Task<List<string>> GetExistingModules()
        {
            return await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY) ?? [];
        }

        /// <summary>
        /// 合并现有模块和新模块列表
        /// </summary>
        /// <param name="currentModules">当前模块列表</param>
        /// <param name="existingModules">已存在的模块列表</param>
        /// <returns>合并后的模块列表</returns>
        private List<string> MergeModules(List<string> currentModules, List<string> existingModules)
        {
            return existingModules.Union(currentModules).Distinct().ToList();
        }

        /// <summary>
        /// 更新模块缓存
        /// </summary>
        /// <param name="currentModules">当前模块列表</param>
        /// <param name="allModules">所有模块列表</param>
        private async Task UpdateModuleCache(List<string> currentModules, List<string> allModules)
        {
            await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, allModules, _cacheOptions);

            foreach (var moduleName in currentModules)
            {
                await UpdateModulePermissionCache(moduleName);
            }
        }

        /// <summary>
        /// 更新指定模块的权限缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        private async Task UpdateModulePermissionCache(string moduleName)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
            var modulePermissions = BuildModulePermissionTree(moduleName);
            var existingPermissions = await _cache.GetAsync<List<PermissionNode>>(cacheKey);

            if (existingPermissions != null)
            {
                MergePermissionNodes(existingPermissions[0], modulePermissions[0]);
                modulePermissions = existingPermissions;
            }

            await _cache.SetAsync(cacheKey, modulePermissions, _cacheOptions);
            _logger.LogInformation("Updated permission tree for module: {ModuleName}", moduleName);
        }

        /// <summary>
        /// 合并权限节点，保留现有节点的信息
        /// </summary>
        /// <param name="existing">现有节点</param>
        /// <param name="current">当前节点</param>
        private void MergePermissionNodes(PermissionNode existing, PermissionNode current)
        {
            // 更新现有节点的基本信息，保留原有的扩展属性
            existing.Path = current.Path;
            existing.RequestMethod = current.RequestMethod;

            // 处理子节点
            foreach (var currentChild in current.Children)
            {
                var existingChild = existing.Children.FirstOrDefault(c => c.Name == currentChild.Name);
                if (existingChild != null)
                {
                    // 递归合并子节点
                    MergePermissionNodes(existingChild, currentChild);
                }
                else
                {
                    // 添加新的子节点
                    existing.Children.Add(currentChild);
                }
            }
        }

        /// <summary>
        /// 清除指定模块的权限树缓存
        /// </summary>
        public async Task ClearModulePermissionCacheAsync(string moduleName)
        {
            _logger.LogInformation("Clearing permission cache for module: {ModuleName}", moduleName);

            // 清除指定模块的缓存
            var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation("Removed cache for key: {CacheKey}", cacheKey);

            // 更新模块名称列表
            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);
            if (moduleNames != null)
            {
                moduleNames.Remove(moduleName);
                await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, moduleNames, _cacheOptions);
                _logger.LogInformation("Updated module names cache, remaining modules: {ModuleCount}", moduleNames.Count);
            }
            else
            {
                _logger.LogWarning("Module names cache not found when clearing module: {ModuleName}", moduleName);
            }

            _logger.LogInformation("Permission cache cleared for module: {ModuleName}", moduleName);
        }
    }
} 