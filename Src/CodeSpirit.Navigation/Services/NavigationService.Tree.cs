using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation
{
    public partial class NavigationService
    {
        /// <summary>
        /// 更新模块导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        private async Task UpdateModuleNavigationCache(string moduleName)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
            var moduleNavigation = BuildModuleNavigationTree(moduleName);
            var existingNavigation = await _cache.GetAsync<List<NavigationNode>>(cacheKey);

            if (existingNavigation != null)
            {
                MergeNavigationNodes(existingNavigation[0], moduleNavigation[0]);
                moduleNavigation = existingNavigation;
            }

            await _cache.SetAsync(cacheKey, moduleNavigation, _cacheOptions);
        }

        /// <summary>
        /// 构建模块导航树
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        private List<NavigationNode> BuildModuleNavigationTree(string moduleName)
        {
            var controllers = _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(x => x.ControllerTypeInfo.GetCustomAttribute<ModuleAttribute>()?.Name == moduleName)
                .GroupBy(x => x.ControllerTypeInfo);

            // Get module display name from ModuleAttribute
            var moduleType = controllers.FirstOrDefault()?.Key.Assembly.GetTypes()
                .FirstOrDefault(t => t.GetCustomAttribute<ModuleAttribute>()?.Name == moduleName);
            var moduleAttr = moduleType?.GetCustomAttribute<ModuleAttribute>();
            var moduleDisplayName = moduleAttr?.DisplayName;
            var modulePath = $"/{moduleName.ToCamelCase()}";

            var moduleNode = new NavigationNode(moduleName, moduleDisplayName, modulePath)
            {
                ModuleName = moduleName,
                Permission = moduleName.ToCamelCase()
            };

            foreach (var controller in controllers)
            {
                var navAttr = controller.Key.GetCustomAttribute<NavigationAttribute>();
                if (navAttr != null && !navAttr.Hidden)
                {
                    var controllerName = controller.Key.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase).ToCamelCase();
                    var controllerPath = $"{modulePath}/{controllerName}";
                    var controllerNode = CreateNavigationNode(moduleName, navAttr, controllerName, controller.Key, controllerPath);
                    moduleNode.Children.Add(controllerNode);

                    foreach (var action in controller)
                    {
                        var actionNavAttr = action.MethodInfo.GetCustomAttribute<NavigationAttribute>();
                        if (actionNavAttr != null && !actionNavAttr.Hidden)
                        {
                            var actionName = action.ActionName.ToCamelCase();
                            var actionPath = $"{controllerPath}/{actionName}";
                            var actionNode = CreateNavigationNode(moduleName, actionNavAttr, actionName, action.MethodInfo, actionPath);
                            controllerNode.Children.Add(actionNode);
                        }
                    }
                }
            }

            return [moduleNode];
        }

        /// <summary>
        /// 创建导航节点
        /// </summary>
        private NavigationNode CreateNavigationNode(string moduleName, NavigationAttribute attr, string defaultName, MemberInfo memberInfo, string defaultPath)
        {
            var displayAttr = memberInfo.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
            var descriptionAttr = memberInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

            var node = new NavigationNode(defaultName, attr.Title ?? displayAttr?.DisplayName ?? defaultName, attr.Path ?? defaultPath)
            {
                Icon = attr.Icon,
                Order = attr.Order,
                ParentPath = attr.ParentPath,
                Hidden = attr.Hidden,
                Description = attr.Description ?? descriptionAttr?.Description,
                IsExternal = attr.IsExternal,
                Target = attr.Target,
                ModuleName = moduleName
            };

            // Generate permission code if not explicitly set
            if (string.IsNullOrEmpty(attr.Permission))
            {
                if (memberInfo is Type controllerType)
                {
                    // For controller: moduleName_controllerName
                    var controllerName = controllerType.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                    node.Permission = $"{moduleName.ToCamelCase()}_{controllerName.ToCamelCase()}";
                }
                else if (memberInfo is MethodInfo methodInfo)
                {
                    // For action: moduleName_controllerName_actionName
                    controllerType = methodInfo.DeclaringType;
                    var controllerName = controllerType.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                    var actionName = methodInfo.Name;
                    node.Permission = $"{moduleName.ToCamelCase()}_{controllerName.ToCamelCase()}_{actionName.ToCamelCase()}";
                }
            }
            else
            {
                node.Permission = attr.Permission;
            }

            // Handle route generation
            if (string.IsNullOrEmpty(node.Route) && memberInfo is Type controllerType2)
            {
                var controllerName = controllerType2.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                var route = controllerType2.GetCustomAttribute<RouteAttribute>()?.Template?.Replace("[controller]", controllerName.ToCamelCase()) ?? string.Empty;

                if (!string.IsNullOrEmpty(route))
                {
                    node.Route = route;
                }
            }

            return node;
        }

        /// <summary>
        /// 合并导航节点
        /// </summary>
        private void MergeNavigationNodes(NavigationNode existing, NavigationNode current)
        {
            existing.Title = current.Title;
            existing.Path = current.Path;
            existing.Icon = current.Icon;
            existing.Order = current.Order;
            existing.ParentPath = current.ParentPath;
            existing.Hidden = current.Hidden;
            existing.Permission = current.Permission;
            existing.Description = current.Description;
            existing.IsExternal = current.IsExternal;
            existing.Target = current.Target;
            existing.ModuleName = current.ModuleName;
            existing.Route = current.Route;

            foreach (var currentChild in current.Children)
            {
                var existingChild = existing.Children.FirstOrDefault(c => c.Name == currentChild.Name);
                if (existingChild != null)
                {
                    MergeNavigationNodes(existingChild, currentChild);
                }
                else
                {
                    existing.Children.Add(currentChild);
                }
            }
        }
    }
}
