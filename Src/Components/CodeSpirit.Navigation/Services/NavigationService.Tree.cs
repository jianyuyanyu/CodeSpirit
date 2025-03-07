using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation
{
    public partial class NavigationService
    {
        private readonly string CONFIG_SECTION_KEY = "Navigation";

        /// <summary>
        /// 构建模块导航树
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <summary>
        /// 构建指定模块的导航树。
        /// 首先从代码中构建导航，如果成功则加载配置文件中的导航，并进行合并。
        /// 如果两者都存在且代码导航不为空，则返回合并后的导航列表；
        /// 否则返回非空的导航列表，如果都为空则返回空列表。
        /// </summary>
        /// <param name="moduleName">模块名称。</param>
        /// <returns>导航节点列表。</returns>
        protected virtual List<NavigationNode> BuildModuleNavigationTree(string moduleName)
        {
            // 首先尝试从代码构建导航树
            var codeNavigation = BuildCodeBasedNavigation(moduleName);

            // 然后加载配置文件中的导航
            var configNavigation = LoadNavigationFromConfig(moduleName);

            // 如果两者都存在且代码导航不为空列表，进行合并
            if (configNavigation != null && codeNavigation.Count > 0)
            {
                MergeNavigationNodes(configNavigation, codeNavigation[0]);
                return [configNavigation];
            }

            // 返回非空的那个，如果都为空则返回空列表
            return configNavigation != null ? [configNavigation] : codeNavigation;
        }

        /// <summary>
        /// 从代码构建导航树
        /// </summary>
        private List<NavigationNode> BuildCodeBasedNavigation(string moduleName)
        {
            var controllers = _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(x => x.ControllerTypeInfo.GetCustomAttribute<ModuleAttribute>()?.Name == moduleName)
                .GroupBy(x => x.ControllerTypeInfo);

            if (!controllers.Any())
            {
                return [];
            }

            // Get module display name from ModuleAttribute
            var moduleType = controllers.FirstOrDefault()?.Key.Assembly.GetTypes()
                .FirstOrDefault(t => t.GetCustomAttribute<ModuleAttribute>()?.Name == moduleName);
            var moduleAttr = moduleType?.GetCustomAttribute<ModuleAttribute>();
            var moduleDisplayName = moduleAttr?.DisplayName;
            var modulePath = $"/{moduleName.ToCamelCase()}";

            var moduleNode = new NavigationNode(moduleName, moduleDisplayName, modulePath)
            {
                ModuleName = moduleName,
                Permission = moduleName.ToCamelCase(),
                Icon = moduleAttr?.Icon
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
            existing.Link = current.Link;

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

        /// <summary>
        /// 从配置文件加载导航配置
        /// </summary>
        private NavigationNode LoadNavigationFromConfig(string moduleName)
        {
            var config = _configuration.GetSection($"{CONFIG_SECTION_KEY}:{moduleName}")
                .Get<NavigationConfigItem>();

            if (config == null)
            {
                return null;
            }

            return ConvertToNavigationNode(config, moduleName);
        }

        /// <summary>
        /// 将配置项转换为导航节点
        /// </summary>
        private NavigationNode ConvertToNavigationNode(NavigationConfigItem item, string moduleName)
        {
            var node = new NavigationNode(
                item.Name ?? item.Path?.Split('/').Last() ?? "unknown",
                item.Title,
                item.Path)
            {
                Icon = item.Icon,
                Order = item.Order,
                ParentPath = item.ParentPath,
                Hidden = item.Hidden,
                Permission = item.Permission,
                Description = item.Description,
                IsExternal = item.IsExternal,
                Target = item.Target,
                ModuleName = item.ModuleName ?? moduleName,
                Route = item.Route,
                Link = item.Link
            };

            if (item.Children?.Any() == true)
            {
                node.Children.AddRange(item.Children.Select(child =>
                    ConvertToNavigationNode(child, moduleName)));
            }

            return node;
        }
    }
}
