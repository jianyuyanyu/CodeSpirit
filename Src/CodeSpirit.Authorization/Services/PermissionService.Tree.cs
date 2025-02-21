using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    public partial class PermissionService
    {
        /// <summary>
        /// 构建权限树的主要方法
        /// </summary>
        private void BuildPermissionTree()
        {
            _logger.LogInformation("Building permission tree");

            var controllers = GetControllers()
                .Where(c => !IsAnonymousController(c))
                .GroupBy(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default");

            foreach (var moduleGroup in controllers)
            {
                var moduleNode = CreateModuleNode(moduleGroup);
                ProcessModuleControllers(moduleGroup, moduleNode);
                _permissionTree.Add(moduleNode);
            }

            BuildHierarchicalTree(_permissionTree);
            _logger.LogInformation("Permission tree built successfully with {ModuleCount} modules", _permissionTree.Count);
        }

        /// <summary>
        /// 创建模块节点
        /// </summary>
        /// <param name="moduleGroup">模块分组信息</param>
        /// <returns>模块权限节点</returns>
        private PermissionNode CreateModuleNode(IGrouping<string, TypeInfo> moduleGroup)
        {
            var moduleName = moduleGroup.Key;
            var moduleAttr = moduleGroup.First().GetCustomAttribute<ModuleAttribute>();
            var moduleDisplayName = moduleAttr?.DisplayName ?? moduleName;

            return new PermissionNode(
                moduleName,
                moduleName,
                path: string.Empty,
                displayName: moduleDisplayName);
        }

        /// <summary>
        /// 处理模块下的所有控制器
        /// </summary>
        /// <param name="moduleGroup">模块分组信息</param>
        /// <param name="moduleNode">模块节点</param>
        private void ProcessModuleControllers(IGrouping<string, TypeInfo> moduleGroup, PermissionNode moduleNode)
        {
            foreach (var controller in moduleGroup)
            {
                var controllerNode = CreateControllerNode(controller, moduleNode.Name);
                if (controllerNode != null)
                {
                    moduleNode.Children.Add(controllerNode);
                    ProcessControllerActions(controller, controllerNode);
                }
            }
        }

        /// <summary>
        /// 构建层级权限树
        /// </summary>
        /// <param name="nodes">权限节点列表</param>
        private void BuildHierarchicalTree(List<PermissionNode> nodes)
        {
            var nodeDict = nodes.ToDictionary(n => n.Name);

            foreach (var node in nodes.Where(n => !string.IsNullOrEmpty(n.Parent)))
            {
                if (nodeDict.TryGetValue(node.Parent, out var parentNode))
                {
                    if (!parentNode.Children.Contains(node))
                    {
                        parentNode.Children.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// 构建指定模块的权限树
        /// </summary>
        private List<PermissionNode> BuildModulePermissionTree(string targetModule)
        {
            var controllers = GetControllers()
                .Where(c => !IsAnonymousController(c) &&
                       (c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default") == targetModule);

            var moduleAttr = controllers.FirstOrDefault()?.GetCustomAttribute<ModuleAttribute>();
            var moduleDisplayName = moduleAttr?.DisplayName ?? targetModule;

            var moduleNode = new PermissionNode(
                targetModule,
                targetModule,
                path: string.Empty,
                displayName: moduleDisplayName);

            foreach (var controller in controllers)
            {
                var controllerNode = CreateControllerNode(controller, targetModule);
                if (controllerNode != null)
                {
                    moduleNode.Children.Add(controllerNode);
                    ProcessControllerActions(controller, controllerNode);
                }
            }

            return new List<PermissionNode> { moduleNode };
        }
    }
} 