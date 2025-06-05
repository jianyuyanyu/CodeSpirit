using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    public partial class PermissionService
    {
        ///// <summary>
        ///// 构建权限树的主要方法
        ///// </summary>
        //private void BuildPermissionTree()
        //{
        //    _logger.LogInformation("Building permission tree");

        //    var controllers = GetControllers()
        //        .Where(c => !IsAnonymousController(c))
        //        .GroupBy(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default");

        //    foreach (var moduleGroup in controllers)
        //    {
        //        var moduleNode = CreateModuleNode(moduleGroup);
        //        ProcessModuleControllers(moduleGroup, moduleNode);
        //        _permissionTree.Add(moduleNode);
        //    }

        //    BuildHierarchicalTree(_permissionTree);
        //    _logger.LogInformation("Permission tree built successfully with {ModuleCount} modules", _permissionTree.Count);
        //}

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

            // 推断模块级别的平台类型：基于模块内控制器的平台类型
            var modulePlatformType = InferModulePlatformType(moduleGroup);

            return new PermissionNode(
                moduleName,
                moduleName,
                path: string.Empty,
                displayName: moduleDisplayName,
                platformType: modulePlatformType);
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
        /// 推断模块的平台类型
        /// </summary>
        /// <param name="moduleGroup">模块分组信息</param>
        /// <returns>推断出的平台类型</returns>
        private PlatformType InferModulePlatformType(IGrouping<string, TypeInfo> moduleGroup)
        {
            // 收集所有控制器的平台类型
            var controllerPlatformTypes = new List<PlatformType>();
            
            foreach (var controller in moduleGroup)
            {
                var platformType = GetControllerPlatformType(controller);
                controllerPlatformTypes.Add(platformType);
            }

            if (!controllerPlatformTypes.Any())
            {
                return PlatformType.Both;
            }

            // 去重后的平台类型
            var distinctPlatformTypes = controllerPlatformTypes.Distinct().ToList();
            
            // 如果所有控制器都是同一个平台类型，使用该平台类型
            if (distinctPlatformTypes.Count == 1)
            {
                return distinctPlatformTypes.First();
            }
            
            // 如果包含多种平台类型，使用 Both 表示支持多平台
            if (distinctPlatformTypes.Contains(PlatformType.System) && distinctPlatformTypes.Contains(PlatformType.Tenant))
            {
                return PlatformType.Both;
            }
            
            // 如果只有 System 或 Tenant 控制器（没有 Both），使用对应的平台类型
            if (distinctPlatformTypes.Contains(PlatformType.System) && !distinctPlatformTypes.Contains(PlatformType.Both))
            {
                return PlatformType.System;
            }
            
            if (distinctPlatformTypes.Contains(PlatformType.Tenant) && !distinctPlatformTypes.Contains(PlatformType.Both))
            {
                return PlatformType.Tenant;
            }

            return PlatformType.Both;
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

            // 推断模块平台类型
            var controllerArray = controllers.ToArray();
            var moduleGroup = controllerArray.GroupBy(c => c.GetCustomAttribute<ModuleAttribute>()?.Name ?? "default").First();
            var modulePlatformType = InferModulePlatformType(moduleGroup);

            var moduleNode = new PermissionNode(
                targetModule,
                targetModule,
                path: string.Empty,
                displayName: moduleDisplayName,
                platformType: modulePlatformType);

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