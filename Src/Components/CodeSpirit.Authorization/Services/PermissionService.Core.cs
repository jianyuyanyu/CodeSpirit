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
            SlidingExpiration = TimeSpan.FromDays(1)
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
        /// 检查用户是否拥有指定权限
        /// </summary>
        /// <param name="name">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public bool HasPermission(string name)
        {
            var httpContext = _serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            HashSet<string> userPermissions = httpContext.User.FindAll("permissions").Select(c => c.Value).ToHashSet();
            return HasPermission(name, userPermissions);
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
            // 检查权限名称是否为 null 或空
            if (string.IsNullOrEmpty(permissionName))
            {
                return false;
            }

            // 检查用户权限集合是否为 null
            if (userPermissions == null)
            {
                return false;
            }

            // 默认放通所有 default_ 开头的权限
            if (permissionName.StartsWith("default_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            //权限继承逻辑：
            //基于权限名称的层级结构（使用下划线分隔）
            //例如对于权限 "module_controller_action"：
            //如果用户有 "module" 权限，则拥有该模块下所有权限
            //如果用户有 "module_controller" 权限，则拥有该控制器下所有权限
            //如果用户有具体的 "module_controller_action" 权限，则只有该具体权限

            // 直接匹配权限
            if (userPermissions.Contains(permissionName))
            {
                return true;
            }

            // 查找权限节点
            var permissionParts = permissionName.Split('_');
            if (permissionParts.Length < 2)
            {
                return false;
            }

            // 先检查模块级权限
            if (userPermissions.Contains(permissionParts[0]))
            {
                return true;
            }

            // 从模块开始逐级查找父权限
            var currentPermission = permissionParts[0]; // 模块
            for (int i = 1; i < permissionParts.Length - 1; i++)
            {
                currentPermission = $"{currentPermission}_{permissionParts[i]}";
                // 如果用户拥有任意父级权限，则认为有权限
                if (userPermissions.Contains(currentPermission))
                {
                    return true;
                }
            }

            return false;
        }
    }
}