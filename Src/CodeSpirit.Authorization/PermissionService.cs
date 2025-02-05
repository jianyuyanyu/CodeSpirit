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
                // 获取模块名称：优先从 ModuleAttribute 获取，其次使用程序集名称
                var moduleAttribute = controller.GetCustomAttribute<ModuleAttribute>() ?? controller.Assembly.GetCustomAttribute<ModuleAttribute>();
                var moduleName = moduleAttribute?.Name;

                // 获取控制器名称并短化（剔除 "Controller" 后缀）
                string controllerName = controller.Name.RemovePostFix("Controller").ToCamelCase();

                // 通过 DisplayNameAttribute 获取控制器描述，若未设置则使用控制器短名
                string controllerDescription = controller.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? controllerName;

                // 获取控制器上定义的路由模板（若有）
                string controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;

                // 合并控制器与动作路由，得到实际请求路径
                string controllerPath = CombineRoutes(controllerRoute, null, controllerName);

                // 创建控制器节点（根节点），控制器节点无需请求路径和请求方法
                var controllerNode = new PermissionNode($"{moduleName}_{controllerName}".TrimStart('_'), controllerDescription, path: controllerPath);
                _permissionTree.Add(controllerNode);

                // 获取控制器中所有公共实例方法，并排除继承自基类的方法
                var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.DeclaringType == controller)
                    .ToList();

                // 遍历控制器中的每个动作方法
                foreach (var action in actions)
                {
                    // 默认的动作短名称与描述
                    string actionShortName = action.Name.ToCamelCase();
                    string actionDescription = action.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? actionShortName;

                    // 检查是否定义了自定义的 PermissionAttribute，若有则使用其中的名称和描述
                    var permissionAttribute = action.GetCustomAttribute<PermissionAttribute>();
                    if (permissionAttribute != null)
                    {
                        actionShortName = permissionAttribute.Name;
                        actionDescription = permissionAttribute.Description;
                    }

                    // 构造权限名称，格式为 "{controllerShortName}_{actionShortName}"
                    string permissionName = $"{moduleName}_{controllerName}_{actionShortName}";

                    // 获取动作上定义的路由模板（优先从 HTTP 方法特性获取，其次从 RouteAttribute 获取）
                    var httpMethodAttribute = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
                    string actionRoute = httpMethodAttribute?.Template ?? action.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;

                    // 合并控制器与动作路由，得到实际请求路径
                    string actionPath = CombineRoutes(controllerRoute, actionRoute, controllerName);

                    // 获取动作支持的 HTTP 请求方法（GET、POST 等）
                    string requestMethod = GetRequestMethod(action);

                    // 创建动作节点，其中 Parent 字段设置为所属控制器短名
                    var actionNode = new PermissionNode(permissionName.TrimStart('_'), actionDescription, controllerName, actionPath, requestMethod);
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
        /// 合并控制器和动作的路由模板，构造实际的请求路径。
        /// 例如：控制器模板为 "api/[controller]"，动作模板为 "getAll"，
        /// 合并后为 "api/getAll"（若控制器模板中存在占位符，则需根据实际情况处理）。
        /// </summary>
        /// <param name="controllerRoute">控制器路由模板</param>
        /// <param name="actionRoute">动作路由模板</param>
        /// <returns>实际请求路径</returns>
        private string CombineRoutes(string controllerRoute, string actionRoute, string controllerName)
        {
            // 如果控制器路由为空，则使用默认的控制器路由模板
            if (string.IsNullOrWhiteSpace(controllerRoute))
            {
                controllerRoute = $"[controller]";
            }

            // 如果动作路由为空，则使用默认的动作路由模板
            if (string.IsNullOrWhiteSpace(actionRoute))
            {
                actionRoute = string.Empty;
            }

            // 替换控制器路由中的 [controller] 为控制器短名
            controllerRoute = controllerRoute.Replace("[controller]", controllerName);

            // 处理路径分隔符，确保路径格式正确
            if (!string.IsNullOrWhiteSpace(actionRoute))
            {
                return $"{controllerRoute.TrimEnd('/')}/{actionRoute.TrimStart('/')}";
            }
            else
            {
                return controllerRoute.TrimEnd('/');
            }
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
            if (action.IsDefined(typeof(HttpGetAttribute), inherit: false))
                return "GET";
            if (action.IsDefined(typeof(HttpPostAttribute), inherit: false))
                return "POST";
            if (action.IsDefined(typeof(HttpPutAttribute), inherit: false))
                return "PUT";
            if (action.IsDefined(typeof(HttpDeleteAttribute), inherit: false))
                return "DELETE";
            if (action.IsDefined(typeof(HttpPatchAttribute), inherit: false))
                return "PATCH";
            // 可根据需要添加更多 HTTP 方法支持

            // 默认返回空字符串，表示未定义请求方法
            return string.Empty;
        }
    }
}
