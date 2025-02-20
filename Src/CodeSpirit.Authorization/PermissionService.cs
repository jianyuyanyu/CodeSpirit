using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 权限服务：用于从应用中的所有控制器及其动作中构建权限树，
    /// 可用于后续权限管理或动态生成菜单等场景。
    /// </summary>
    public class PermissionService : IPermissionService
    {
        /// <summary>
        /// 权限树根节点集合（一般每个控制器为一个根节点，其下挂载动作节点）
        /// </summary>
        private readonly List<PermissionNode> _permissionTree = [];

        /// <summary>
        /// 服务提供者
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 分布式缓存
        /// </summary>
        private readonly IDistributedCache _cache;

        /// <summary>
        /// 缓存键前缀
        /// </summary>
        private const string CACHE_KEY_PREFIX = "CodeSpirit:PermissionTree:Module:";

        /// <summary>
        /// 缓存键前缀
        /// </summary>
        private const string MODULE_NAMES_CACHE_KEY = "CodeSpirit:PermissionTree:ModuleNames";

        /// <summary>
        /// 缓存选项
        /// </summary>
        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        private readonly ILogger<PermissionService> _logger;

        /// <summary>
        /// 构造函数，通过依赖注入的 IServiceProvider 获取应用中所有控制器类型，
        /// 并构造权限树。
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="cache">分布式缓存</param>
        /// <param name="logger">日志记录器</param>
        public PermissionService(
            IServiceProvider serviceProvider,
            IDistributedCache cache,
            ILogger<PermissionService> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _logger = logger;
        }

        public async Task InitializePermissionTree()
        {
            _logger.LogInformation("Starting permission tree initialization");
            
            // 获取当前服务的所有模块
            var currentModules = GetControllers()
                .Where(c => !IsAnonymousController(c))
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default")
                .Distinct()
                .ToList();

            _logger.LogInformation("Found {ModuleCount} modules: {Modules}", 
                currentModules.Count, 
                string.Join(", ", currentModules));

            // 保存模块名称列表到缓存
            await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, currentModules, _cacheOptions);

            // 清除并重建每个模块的缓存
            foreach (var moduleName in currentModules)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
                await _cache.RemoveAsync(cacheKey);
            }

            // 构建新的权限树
            BuildPermissionTree();

            // 按模块分组保存到缓存
            foreach (var moduleNode in _permissionTree)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{moduleNode.Name}";
                await _cache.SetAsync(cacheKey, new List<PermissionNode> { moduleNode }, _cacheOptions);
            }

            _logger.LogInformation("Permission tree initialization completed");
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
        /// 构建权限树的主要方法
        /// </summary>
        private void BuildPermissionTree()
        {
            _logger.LogInformation("Building permission tree");
            
            IEnumerable<TypeInfo> controllers = GetControllers();

            // 按模块分组处理控制器
            var moduleGroups = controllers
                .Where(c => !IsAnonymousController(c))
                .GroupBy(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default");

            foreach (var moduleGroup in moduleGroups)
            {
                var moduleName = moduleGroup.Key;
                _logger.LogDebug("Processing module: {ModuleName}", moduleName);
                
                // 获取第一个控制器上的 ModuleAttribute，用于提取显示名称
                var moduleAttr = moduleGroup.First().GetCustomAttribute<ModuleAttribute>();
                var moduleDisplayName = moduleAttr?.DisplayName ?? moduleName;

                var moduleNode = new PermissionNode(
                    moduleName,
                    moduleName,
                    path: string.Empty,
                    displayName: moduleDisplayName);

                foreach (TypeInfo controller in moduleGroup)
                {
                    PermissionNode controllerNode = CreateControllerNode(controller, moduleName);
                    if (controllerNode != null)
                    {
                        moduleNode.Children.Add(controllerNode);
                        ProcessControllerActions(controller, controllerNode);
                    }
                }

                _permissionTree.Add(moduleNode);
            }

            BuildHierarchicalTree(_permissionTree);

            _logger.LogInformation("Permission tree built successfully with {ModuleCount} modules", 
                _permissionTree.Count);
        }

        /// <summary>
        /// 创建控制器节点
        /// </summary>
        /// <param name="controller">控制器类型信息</param>
        /// <param name="moduleName">模块名称</param>
        /// <returns>权限节点</returns>
        private PermissionNode CreateControllerNode(TypeInfo controller, string moduleName)
        {
            // 获取控制器名称
            string controllerName = controller.Name.RemovePostFix("Controller").ToCamelCase();

            // 获取权限和显示名称特性
            PermissionAttribute permissionAttr = controller.GetCustomAttribute<PermissionAttribute>();
            DisplayNameAttribute displayNameAttr = controller.GetCustomAttribute<DisplayNameAttribute>();

            // 确定最终的名称和描述
            string name = permissionAttr?.Name ?? controllerName;
            string description = permissionAttr?.Description ??
                              displayNameAttr?.DisplayName ??
                              controllerName;
            string displayName = permissionAttr?.DisplayName ?? displayNameAttr?.DisplayName ?? name;

            // 处理路由
            string route = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            string path = RouteHelper.CombineRoutes(route, null, controllerName);

            // 创建节点，设置父节点为模块名
            PermissionNode node = new(
                $"{moduleName}_{name}",
                description,
                moduleName, // 设置父节点为模块名
                path,
                displayName: displayName);

            return node;
        }

        /// <summary>
        /// 处理控制器的所有动作方法
        /// </summary>
        /// <param name="controller">控制器类型信息</param>
        /// <param name="controllerNode">控制器节点</param>
        private void ProcessControllerActions(TypeInfo controller, PermissionNode controllerNode)
        {
            IEnumerable<MethodInfo> actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controller && !IsAnonymousAction(m));

            foreach (MethodInfo action in actions)
            {
                PermissionNode actionNode = CreateActionNode(action, controller, controllerNode);
                if (actionNode != null)
                {
                    controllerNode.Children.Add(actionNode);
                }
            }
        }

        /// <summary>
        /// 检查动作方法是否允许匿名访问
        /// </summary>
        /// <param name="action">动作方法信息</param>
        /// <returns>是否允许匿名访问</returns>
        private bool IsAnonymousAction(MethodInfo action) =>
            action.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        /// <summary>
        /// 创建动作节点
        /// </summary>
        /// <param name="action">动作方法信息</param>
        /// <param name="controller">控制器类型信息</param>
        /// <param name="controllerNode">控制器节点</param>
        /// <returns>权限节点</returns>
        private PermissionNode CreateActionNode(MethodInfo action, TypeInfo controller, PermissionNode controllerNode)
        {
            // 获取权限和显示名称特性
            PermissionAttribute permissionAttr = action.GetCustomAttribute<PermissionAttribute>();
            DisplayNameAttribute displayNameAttr = action.GetCustomAttribute<DisplayNameAttribute>();

            // 确定动作名称
            string actionName = action.Name.ToCamelCase();
            string name = permissionAttr?.Name ?? actionName;
            string description = permissionAttr?.Description ?? actionName;
            string displayName = permissionAttr?.DisplayName ??
                            displayNameAttr?.DisplayName ??
                            actionName;

            // 处理路由
            string controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            string actionRoute = GetActionRoute(action);
            string path = RouteHelper.CombineRoutes(controllerRoute, actionRoute, controllerNode.Name);
            string requestMethod = HttpMethodHelper.GetRequestMethod(action);

            // 创建节点
            PermissionNode node = new(
                $"{controllerNode.Name}_{name}",
                description,
                controllerNode.Name,
                path,
                requestMethod,
                displayName: displayName);

            return node;
        }

        /// <summary>
        /// 获取动作方法的路由
        /// </summary>
        /// <param name="action">动作方法信息</param>
        /// <returns>路由模板</returns>
        private string GetActionRoute(MethodInfo action)
        {
            HttpMethodAttribute httpMethodAttr = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
            return httpMethodAttr?.Template ??
                   action.GetCustomAttribute<RouteAttribute>()?.Template ??
                   string.Empty;
        }

        /// <summary>
        /// 构建层级权限树
        /// </summary>
        /// <param name="nodes">权限节点列表</param>
        private void BuildHierarchicalTree(List<PermissionNode> nodes)
        {
            Dictionary<string, PermissionNode> nodeDict = nodes.ToDictionary(n => n.Name);

            foreach (PermissionNode node in nodes.Where(n => !string.IsNullOrEmpty(n.Parent)))
            {
                if (nodeDict.TryGetValue(node.Parent, out PermissionNode parentNode))
                {
                    if (!parentNode.Children.Contains(node))
                    {
                        parentNode.Children.Add(node);
                    }
                }
            }
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
                _logger.LogWarning("No modules found in cache");
                return allModuleNodes;
            }

            _logger.LogDebug("Found {ModuleCount} modules in cache", moduleNames.Count);
            
            // 获取每个模块的权限树
            foreach (var moduleName in moduleNames)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
                var moduleNodes = await _cache.GetAsync<List<PermissionNode>>(cacheKey);
                if (moduleNodes != null)
                {
                    allModuleNodes.AddRange(moduleNodes);
                }
            }

            return allModuleNodes;
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

            // 更新模块名称列表
            var moduleNames = await _cache.GetAsync<List<string>>(MODULE_NAMES_CACHE_KEY);
            if (moduleNames != null)
            {
                moduleNames.Remove(moduleName);
                await _cache.SetAsync(MODULE_NAMES_CACHE_KEY, moduleNames, _cacheOptions);
            }

            _logger.LogInformation("Permission cache cleared for module: {ModuleName}", moduleName);
        }

        // 权限代码生成方法
        private string GeneratePermissionCode(PermissionNode permissionNode)
        {
            return $"{permissionNode.RequestMethod}:{permissionNode.Name}".GenerateShortCode();
        }

        /// <summary>
        /// 检查权限代码是否存在
        /// </summary>
        /// <param name="name">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public bool HasPermission(string name)
        {
            return FindNodeByName(name, _permissionTree) != null;
        }

        /// <summary>
        /// 递归查找指定权限代码的节点
        /// </summary>
        /// <param name="name">权限代码</param>
        /// <param name="nodes">要搜索的节点集合</param>
        /// <returns>找到的节点，如果未找到则返回 null</returns>
        private PermissionNode FindNodeByName(string name, List<PermissionNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Name == name)
                {
                    return node;
                }

                var foundInChildren = FindNodeByName(name, node.Children);
                if (foundInChildren != null)
                {
                    return foundInChildren;
                }
            }

            return null;
        }
    }
}
