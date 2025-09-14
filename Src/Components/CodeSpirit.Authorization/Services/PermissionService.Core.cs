using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;
using CodeSpirit.Authorization.Extensions;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 权限服务：用于从应用中的所有控制器及其动作中构建权限树，
    /// 可用于后续权限管理或动态生成菜单等场景。
    /// </summary>
    public partial class PermissionService : IPermissionService
    {
        private readonly List<PermissionNode> _permissionTree = [];
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PermissionService> _logger;

        private const string CACHE_KEY_PREFIX = "CodeSpirit:PermissionTree:Module:";
        private const string MODULE_NAMES_CACHE_KEY = "CodeSpirit:PermissionTree:ModuleNames";

        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
            SlidingExpiration = TimeSpan.FromDays(90)
        };

        public PermissionService(
            IServiceProvider serviceProvider,
            IDistributedCache cache,
            ILogger<PermissionService> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取权限树，即所有控制器及其下属动作组成的节点集合
        /// </summary>
        /// <returns>权限树根节点列表</returns>
        public List<PermissionNode> GetPermissionTree()
        {
            return GetPermissionTreeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 获取所有控制器
        /// </summary>
        /// <returns>控制器类型信息集合</returns>
        private IEnumerable<TypeInfo> GetControllers()
        {
            ApplicationPartManager partManager = _serviceProvider.GetRequiredService<ApplicationPartManager>();
            ControllerFeature controllerFeature = new();
            partManager.PopulateFeature(controllerFeature);
            return controllerFeature.Controllers;
        }

        /// <summary>
        /// 检查控制器是否允许匿名访问
        /// </summary>
        /// <param name="controller">控制器类型信息</param>
        /// <returns>是否允许匿名访问</returns>
        private bool IsAnonymousController(TypeInfo controller) =>
            controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        /// <summary>
        /// 获取权限树，即所有控制器及其下属动作组成的节点集合
        /// </summary>
        /// <returns>权限树根节点列表</returns>
        public async Task<List<PermissionNode>> GetPermissionTreeAsync()
        {
            _logger.LogDebug("Retrieving permission tree from cache");

            var allModuleNodes = new List<PermissionNode>();
            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);

            if (moduleNames == null)
            {
                _logger.LogWarning("No modules found in cache with key: {CacheKey}", MODULE_NAMES_CACHE_KEY);
                return allModuleNodes;
            }

            _logger.LogDebug("Found {ModuleCount} modules in cache with key: {CacheKey}",
                moduleNames.Count,
                MODULE_NAMES_CACHE_KEY);

            // 获取每个模块的权限树
            foreach (var moduleName in moduleNames)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
                var moduleNodes = await _cache.GetAsync<List<PermissionNode>>(cacheKey);
                if (moduleNodes != null)
                {
                    _logger.LogDebug("Retrieved permission tree for module: {ModuleName} with key: {CacheKey}, nodes count: {NodesCount}",
                        moduleName,
                        cacheKey,
                        moduleNodes.Count);
                    allModuleNodes.AddRange(moduleNodes);
                }
                else
                {
                    _logger.LogWarning("Cache miss for module: {ModuleName} with key: {CacheKey}",
                        moduleName,
                        cacheKey);
                }
            }

            return allModuleNodes;
        }

        /// <summary>
        /// 检查用户是否拥有指定权限
        /// </summary>
        /// <param name="permissionName">权限名称</param>
        /// <param name="userPermissions">用户拥有的权限集合</param>
        /// <returns>true 表示有权限，false 表示无权限</returns>
        public bool HasPermission(string permissionName, ISet<string> userPermissions)
        {
            _logger.LogWarning("[PermissionService] 开始具体权限检查: 权限名称={PermissionName}", permissionName);
            
            // 检查权限名称是否为 null 或空
            if (string.IsNullOrEmpty(permissionName))
            {
                _logger.LogWarning("[PermissionService] 权限名称为空，返回false");
                return false;
            }

            // 默认放通所有 default_ 开头的权限
            if (permissionName.StartsWith("default_", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[PermissionService] default_开头的权限，直接放通");
                return true;
            }

            // 检查用户权限集合是否为 null
            if (userPermissions == null)
            {
                _logger.LogWarning("[PermissionService] 用户权限集合为null，返回false");
                return false;
            }

            _logger.LogWarning("[PermissionService] 用户权限集合大小: {PermissionCount}", userPermissions.Count);
            if (userPermissions.Count > 0)
            {
                _logger.LogWarning("[PermissionService] 用户权限详情: [{Permissions}]", string.Join(",", userPermissions));
            }

            //权限继承逻辑：
            //基于权限名称的层级结构（使用下划线分隔）
            //例如对于权限 "module_controller_action"：
            //如果用户有 "module" 权限，则拥有该模块下所有权限
            //如果用户有 "module_controller" 权限，则拥有该控制器下所有权限
            //如果用户有具体的 "module_controller_action" 权限，则只有该具体权限

            // 直接匹配权限（不区分大小写）
            _logger.LogWarning("[PermissionService] 检查直接匹配权限: {PermissionName}", permissionName);
            if (userPermissions.Any(p => string.Equals(p, permissionName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("[PermissionService] 直接匹配成功，返回true");
                return true;
            }

            // 查找权限节点
            var permissionParts = permissionName.Split('_');
            _logger.LogWarning("[PermissionService] 权限分段: [{Parts}], 段数: {PartCount}", 
                string.Join(",", permissionParts), permissionParts.Length);
            
            if (permissionParts.Length < 2)
            {
                _logger.LogWarning("[PermissionService] 权限段数小于2，返回false");
                return false;
            }
            //对于二级权限，如果用户存在三级及以下权限，则放通二级权限（控制器权限）
            else if (permissionParts.Length == 2)
            {
                _logger.LogWarning("[PermissionService] 检查二级权限的子权限匹配");
                if (userPermissions.Any(p => p.StartsWith(permissionName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("[PermissionService] 二级权限子权限匹配成功，返回true");
                    return true;
                }
            }

            // 先检查模块级权限（不区分大小写）
            _logger.LogWarning("[PermissionService] 检查模块级权限: {ModulePermission}", permissionParts[0]);
            if (userPermissions.Any(p => string.Equals(p, permissionParts[0], StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("[PermissionService] 模块级权限匹配成功，返回true");
                return true;
            }

            // 从模块开始逐级查找父权限（不区分大小写）
            var currentPermission = permissionParts[0]; // 模块
            _logger.LogWarning("[PermissionService] 开始逐级检查父权限");
            for (int i = 1; i < permissionParts.Length - 1; i++)
            {
                currentPermission = $"{currentPermission}_{permissionParts[i]}";
                _logger.LogWarning("[PermissionService] 检查父权限: {ParentPermission}", currentPermission);
                // 如果用户拥有任意父级权限，则认为有权限
                if (userPermissions.Any(p => string.Equals(p, currentPermission, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("[PermissionService] 父权限匹配成功，返回true");
                    return true;
                }
            }

            _logger.LogWarning("[PermissionService] 所有权限检查都失败，返回false");
            return false;
        }
    }
}