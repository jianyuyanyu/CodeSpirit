using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Reflection;

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
        /// 构造函数，通过依赖注入的 IServiceProvider 获取应用中所有控制器类型，
        /// 并构造权限树。
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public PermissionService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            BuildPermissionTree();
        }

        /// <summary>
        /// 构建权限树的主要方法
        /// </summary>
        private void BuildPermissionTree()
        {
            IEnumerable<TypeInfo> controllers = GetControllers();
            foreach (TypeInfo controller in controllers.Where(c => !IsAnonymousController(c)))
            {
                PermissionNode controllerNode = CreateControllerNode(controller);
                if (controllerNode != null)
                {
                    _permissionTree.Add(controllerNode);
                    ProcessControllerActions(controller, controllerNode);
                }
            }

            BuildHierarchicalTree(_permissionTree);
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
        /// 创建控制器节点
        /// </summary>
        /// <param name="controller">控制器类型信息</param>
        /// <returns>权限节点</returns>
        private PermissionNode CreateControllerNode(TypeInfo controller)
        {
            // 获取模块特性
            ModuleAttribute moduleAttr = controller.GetCustomAttribute<ModuleAttribute>() ??
                            controller.Assembly.GetCustomAttribute<ModuleAttribute>();
            string moduleName = moduleAttr?.Name;

            // 获取控制器名称
            string controllerName = controller.Name.RemovePostFix("Controller").ToCamelCase();

            // 获取权限和显示名称特性
            PermissionAttribute permissionAttr = controller.GetCustomAttribute<PermissionAttribute>();
            DisplayNameAttribute displayName = controller.GetCustomAttribute<DisplayNameAttribute>();

            // 确定最终的名称和描述
            string name = permissionAttr?.Name ?? controllerName;
            string description = permissionAttr?.Description ??
                             displayName?.DisplayName ??
                             controllerName;

            // 处理路由
            string route = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            string path = RouteHelper.CombineRoutes(route, null, controllerName);

            // 创建节点
            PermissionNode node = new(
                $"{moduleName}_{name}".TrimStart('_'),
                description,
                path: path);

            node.Code = permissionAttr?.Code ?? GeneratePermissionCode(node);
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
            DisplayNameAttribute displayName = action.GetCustomAttribute<DisplayNameAttribute>();

            // 确定动作名称
            string actionName = action.Name.ToCamelCase();
            string name = permissionAttr?.Name ?? actionName;
            string description = permissionAttr?.Description ??
                             displayName?.DisplayName ??
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
                requestMethod);

            node.Code = permissionAttr?.Code ?? GeneratePermissionCode(node);
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
        public List<PermissionNode> GetPermissionTree() => _permissionTree;

        // 权限代码生成方法
        private string GeneratePermissionCode(PermissionNode permissionNode)
        {
            return $"{permissionNode.RequestMethod}:{permissionNode.Name}".GenerateShortCode();
        }

        /// <summary>
        /// 检查权限代码是否存在
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public bool HasPermission(string permissionCode)
        {
            return FindNodeByCode(permissionCode, _permissionTree) != null;
        }

        /// <summary>
        /// 递归查找指定权限代码的节点
        /// </summary>
        /// <param name="code">权限代码</param>
        /// <param name="nodes">要搜索的节点集合</param>
        /// <returns>找到的节点，如果未找到则返回 null</returns>
        private PermissionNode FindNodeByCode(string code, List<PermissionNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Code == code)
                {
                    return node;
                }

                var foundInChildren = FindNodeByCode(code, node.Children);
                if (foundInChildren != null)
                {
                    return foundInChildren;
                }
            }

            return null;
        }
    }
}
