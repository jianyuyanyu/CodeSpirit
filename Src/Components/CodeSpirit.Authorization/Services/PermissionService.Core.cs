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
            _logger.LogDebug("[PermissionService] 开始权限检查: 权限名称={PermissionName}", permissionName);
            
            // 检查权限名称是否为 null 或空
            if (string.IsNullOrEmpty(permissionName))
            {
                _logger.LogDebug("[PermissionService] 权限名称为空，返回false");
                return false;
            }

            // 默认放通所有 default_ 开头的权限
            if (permissionName.StartsWith("default_", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("[PermissionService] default_开头的权限，直接放通");
                return true;
            }

            // 检查用户权限集合是否为 null
            if (userPermissions == null || userPermissions.Count == 0)
            {
                _logger.LogDebug("[PermissionService] 用户权限集合为空，返回false");
                return false;
            }

            _logger.LogDebug("[PermissionService] 用户权限集合大小: {PermissionCount}", userPermissions.Count);

            // 新的权限匹配逻辑：
            // 1. 直接精确匹配（不区分大小写）
            // 2. 检查通配符权限
            //    - 对于权限 "identity_users_create"，检查：
            //      - identity_* (一级通配)
            //      - identity_users_* (二级通配)
            // 3. 只有以 _* 结尾的权限才被视为通配权限

            // 1. 直接精确匹配
            if (userPermissions.Any(p => string.Equals(p, permissionName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("[PermissionService] 直接匹配成功，返回true");
                return true;
            }

            // 2. 检查通配符权限
            var permissionParts = permissionName.Split('_');
            
            // 从一级开始逐级检查通配符权限
            for (int i = 0; i < permissionParts.Length - 1; i++)
            {
                // 构建通配符权限：module_* 或 module_controller_*
                var wildcardPermission = string.Join("_", permissionParts.Take(i + 1)) + "_*";
                
                _logger.LogDebug("[PermissionService] 检查通配符权限: {WildcardPermission}", wildcardPermission);
                
                if (userPermissions.Any(p => string.Equals(p, wildcardPermission, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogDebug("[PermissionService] 通配符权限匹配成功: {WildcardPermission}，返回true", wildcardPermission);
                    return true;
                }
            }

            _logger.LogDebug("[PermissionService] 所有权限检查都失败，返回false");
            return false;
        }
    }
}