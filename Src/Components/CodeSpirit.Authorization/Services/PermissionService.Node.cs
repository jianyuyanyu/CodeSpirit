using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    public partial class PermissionService : IPermissionService
    {
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

            return new PermissionNode(
                $"{moduleName}_{name}",
                description,
                moduleName,
                path,
                displayName: displayName);
        }

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

            return new PermissionNode(
                $"{controllerNode.Name}_{name}",
                description,
                controllerNode.Name,
                path,
                requestMethod,
                displayName: displayName);
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
        /// 检查动作方法是否允许匿名访问
        /// </summary>
        /// <param name="action">动作方法信息</param>
        /// <returns>是否允许匿名访问</returns>
        private bool IsAnonymousAction(MethodInfo action) =>
            action.GetCustomAttribute<AllowAnonymousAttribute>() != null;

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
    }
}