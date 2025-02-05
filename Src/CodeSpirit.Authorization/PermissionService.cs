using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

/// <summary>
/// 权限服务：用于从应用中的所有控制器及其动作中构建权限树，
/// 可用于后续权限管理或动态生成菜单等场景。
/// </summary>
public class PermissionService
{
    /// <summary>
    /// 权限树根节点集合（一般每个控制器为一个根节点，其下挂载动作节点）
    /// </summary>
    private readonly List<PermissionNode> _permissionTree = [];
    /// <summary>
    /// 构造函数，通过依赖注入的 IServiceProvider 获取应用中所有控制器类型，
    /// 并构造权限树。
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public PermissionService(IServiceProvider serviceProvider)
    {
        // 从 DI 容器中获取 ApplicationPartManager
        var partManager = serviceProvider.GetRequiredService<ApplicationPartManager>();
        ControllerFeature controllerFeature = new();
        partManager.PopulateFeature(controllerFeature);

        // 遍历每个控制器，构造控制器节点和对应的动作节点
        foreach (var controller in controllerFeature.Controllers)
        {
            // 获取控制器名称
            string controllerName = controller.Name;
            // 通过 DisplayNameAttribute 获取控制器描述，若未设置则使用控制器名称
            string controllerDescription = controller.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? controllerName;

            // 创建控制器节点（根节点），控制器节点无需请求路径和请求方法
            var controllerNode = new PermissionNode(controllerName, controllerDescription);
            _permissionTree.Add(controllerNode);

            // 获取控制器中所有公共实例方法，并排除继承自基类的方法
            var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controller)
                .ToList();

            // 遍历控制器中的每个动作方法
            foreach (var action in actions)
            {
                // 默认的动作名称和描述
                string actionName = action.Name;
                string actionDescription = action.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? actionName;
                // 获取动作请求路径（例如通过 RouteAttribute 指定的模板）
                string actionPath = GetActionPath(action);
                // 获取动作支持的 HTTP 请求方法（GET、POST 等）
                string requestMethod = GetRequestMethod(action);

                // 如果动作上定义了自定义的 PermissionAttribute，则使用该特性中的信息覆盖默认值
                var permissionAttribute = action.GetCustomAttribute<PermissionAttribute>();
                if (permissionAttribute != null)
                {
                    actionName = permissionAttribute.Name;
                    actionDescription = permissionAttribute.Description;
                }

                // 创建动作节点，其中 Parent 字段设置为所属控制器名称
                var actionNode = new PermissionNode(actionName, actionDescription, controllerName, actionPath, requestMethod);
                controllerNode.Children.Add(actionNode);
            }
        }

        // 根据父节点关系进一步构建多级权限结构（若有需要扩展更深层级）
        BuildPermissionTree(_permissionTree);
    }

    /// <summary>
    /// 根据每个节点的 Parent 属性，构建完整的权限树结构。
    /// 目前主要用于保证节点间的层级关系，后续若需要支持更多层级可在此扩展逻辑。
    /// </summary>
    /// <param name="permissionNodes">权限节点列表</param>
    private void BuildPermissionTree(List<PermissionNode> permissionNodes)
    {
        // 遍历所有节点，如果节点存在父节点标识，则将其添加到对应父节点的 Children 集合中
        foreach (var node in permissionNodes)
        {
            if (!string.IsNullOrEmpty(node.Parent))
            {
                // 查找与节点 Parent 名称匹配的父节点
                var parentNode = permissionNodes.FirstOrDefault(n => n.Name == node.Parent);
                if (parentNode != null && !parentNode.Children.Contains(node))
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

    /// <summary>
    /// 获取指定动作方法的请求路径，优先使用 RouteAttribute 指定的模板
    /// </summary>
    /// <param name="action">动作方法的反射信息</param>
    /// <returns>请求路径模板字符串，若未定义则返回空字符串</returns>
    private string GetActionPath(MethodInfo action)
    {
        var routeAttribute = action.GetCustomAttribute<RouteAttribute>();
        return routeAttribute?.Template ?? string.Empty;
    }

    /// <summary>
    /// 获取指定动作方法的 HTTP 请求方法（如 GET、POST、PUT、DELETE 等）
    /// 根据常用的 HTTP 特性进行判断，若未定义则返回空字符串。
    /// </summary>
    /// <param name="action">动作方法的反射信息</param>
    /// <returns>HTTP 请求方法字符串</returns>
    private string GetRequestMethod(MethodInfo action)
    {
        // 判断动作方法是否标识了各类 HTTP 请求特性
        if (action.IsDefined(typeof(HttpGetAttribute), false))
            return "GET";
        if (action.IsDefined(typeof(HttpPostAttribute), false))
            return "POST";
        if (action.IsDefined(typeof(HttpPutAttribute), false))
            return "PUT";
        if (action.IsDefined(typeof(HttpDeleteAttribute), false))
            return "DELETE";
        if (action.IsDefined(typeof(HttpPatchAttribute), false))
            return "PATCH";
        // 可根据需要添加更多 HTTP 方法支持

        // 默认返回空字符串，表示未定义请求方法
        return string.Empty;
    }
}