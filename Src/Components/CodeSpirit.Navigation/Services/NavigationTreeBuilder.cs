using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航树构建器实现
    /// </summary>
    public class NavigationTreeBuilder : INavigationTreeBuilder
    {
        private readonly IActionDescriptorCollectionProvider _actionProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NavigationTreeBuilder> _logger;

        private const string CONFIG_SECTION_KEY = "Navigation";

        /// <summary>
        /// 初始化导航树构建器
        /// </summary>
        /// <param name="actionProvider">Action描述符集合提供者</param>
        /// <param name="configuration">配置</param>
        /// <param name="logger">日志记录器</param>
        public NavigationTreeBuilder(
            IActionDescriptorCollectionProvider actionProvider,
            IConfiguration configuration,
            ILogger<NavigationTreeBuilder> logger)
        {
            _actionProvider = actionProvider;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 构建完整的导航树
        /// </summary>
        public List<NavigationNode> BuildNavigationTree()
        {
            _logger.LogInformation("Building complete navigation tree");

            // 1. 获取所有模块名称
            var moduleNames = GetAllModuleNames();
            _logger.LogInformation("Found {Count} modules: {Modules}", moduleNames.Count, string.Join(", ", moduleNames));

            // 2. 为每个模块构建导航树
            var allModules = new List<NavigationNode>();
            foreach (var moduleName in moduleNames)
            {
                try
                {
                    _logger.LogDebug("Building navigation for module: {ModuleName}", moduleName);
                    var moduleNodes = BuildModuleNavigationTree(moduleName);
                    if (moduleNodes != null && moduleNodes.Any())
                    {
                        _logger.LogInformation("Built navigation for module {ModuleName}: {NodeCount} nodes, PlatformType={PlatformType}", 
                            moduleName, moduleNodes.Count, moduleNodes.First().PlatformType);
                        allModules.AddRange(moduleNodes);
                        _logger.LogDebug("Total modules so far: {Count}", allModules.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Module {ModuleName} returned empty navigation tree (no controllers or all hidden)", moduleName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build navigation for module: {ModuleName}", moduleName);
                }
            }

            _logger.LogInformation("Built navigation tree with {Count} modules: {Modules}", 
                allModules.Count, 
                string.Join(", ", allModules.Select(m => $"{m.Name}({m.PlatformType})")));
            return allModules;
        }

        /// <summary>
        /// 构建指定模块的导航树
        /// </summary>
        public List<NavigationNode> BuildModuleNavigationTree(string moduleName)
        {
            // 首先尝试从代码构建导航树
            var codeNavigation = BuildCodeBasedNavigation(moduleName);
            _logger.LogDebug("Module {ModuleName}: codeNavigation={Count} nodes", moduleName, codeNavigation?.Count ?? 0);

            // 然后加载配置文件中的导航
            var configNavigation = LoadNavigationFromConfig(moduleName);
            _logger.LogDebug("Module {ModuleName}: configNavigation={HasConfig}", moduleName, configNavigation != null);

            // 如果两者都存在且代码导航不为空列表，进行合并
            if (configNavigation != null && codeNavigation.Count > 0)
            {
                MergeNavigationNodes(configNavigation, codeNavigation[0]);
                var result = new List<NavigationNode> { configNavigation };
                ProcessPlatformTypeInheritance(result);
                _logger.LogDebug("Module {ModuleName}: merged result, PlatformType={PlatformType}", moduleName, result.First().PlatformType);
                return result;
            }

            // 返回非空的那个，如果都为空则返回空列表
            var navigationResult = configNavigation != null
                ? new List<NavigationNode> { configNavigation }
                : codeNavigation;

            // 处理平台类型继承
            ProcessPlatformTypeInheritance(navigationResult);

            if (navigationResult != null && navigationResult.Any())
            {
                _logger.LogDebug("Module {ModuleName}: final result, PlatformType={PlatformType}", moduleName, navigationResult.First().PlatformType);
            }

            return navigationResult;
        }

        /// <summary>
        /// 合并代码导航和配置导航
        /// </summary>
        public NavigationNode MergeNavigationNodes(NavigationNode existing, NavigationNode current)
        {
            // 复制所有属性
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
            existing.PlatformType = current.PlatformType;
            existing.OriginalPlatformType = current.OriginalPlatformType;
            existing.Group = current.Group;
            existing.Tags = current.Tags;
            existing.RequireAuth = current.RequireAuth;
            existing.IsExperimental = current.IsExperimental;
            existing.MinVersion = current.MinVersion;
            existing.MaxVersion = current.MaxVersion;
            existing.SupportedDevices = current.SupportedDevices;
            existing.Priority = current.Priority;
            existing.Shortcut = current.Shortcut;
            existing.Badge = current.Badge;
            existing.BadgeType = current.BadgeType;
            existing.Visible = current.Visible;

            // 合并元数据
            foreach (var kvp in current.MetaData)
            {
                existing.MetaData[kvp.Key] = kvp.Value;
            }

            // 递归合并子节点
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

            return existing;
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

            // 推断模块级别的平台类型：优先使用最具体的平台类型
            var inferredPlatformType = PlatformType.Both; // 默认为Both，支持所有平台

            // 收集所有控制器的平台类型，包括从基类继承的平台类型
            var controllerPlatformTypes = new List<PlatformType>();

            foreach (var controller in controllers)
            {
                var navAttr = controller.Key.GetCustomAttribute<NavigationAttribute>();
                var platformType = navAttr?.PlatformType ?? PlatformType.Inherit;

                // 如果控制器设置为继承，则查找基类的平台类型
                if (platformType == PlatformType.Inherit)
                {
                    // 从控制器的基类层次结构中查找平台类型
                    var baseType = controller.Key.BaseType;
                    while (baseType != null && baseType != typeof(object))
                    {
                        var baseNavAttr = baseType.GetCustomAttribute<NavigationAttribute>();
                        if (baseNavAttr != null && baseNavAttr.PlatformType != PlatformType.Inherit)
                        {
                            platformType = baseNavAttr.PlatformType;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }

                    // 如果仍然没有找到，使用默认值Both
                    if (platformType == PlatformType.Inherit)
                    {
                        platformType = PlatformType.Both;
                    }
                }

                controllerPlatformTypes.Add(platformType);
            }

            if (controllerPlatformTypes.Any())
            {
                // 去重后的平台类型
                var distinctPlatformTypes = controllerPlatformTypes.Distinct().ToList();

                // 如果所有控制器都是同一个平台类型，使用该平台类型
                if (distinctPlatformTypes.Count == 1)
                {
                    inferredPlatformType = distinctPlatformTypes.First();
                }
                // 如果包含多种平台类型，使用 Both 表示支持多平台
                else if (distinctPlatformTypes.Contains(PlatformType.System) && distinctPlatformTypes.Contains(PlatformType.Tenant))
                {
                    inferredPlatformType = PlatformType.Both;
                }
                // 如果只有 System 或 Tenant 控制器（没有 Both），使用对应的平台类型
                else if (distinctPlatformTypes.Contains(PlatformType.System) && !distinctPlatformTypes.Contains(PlatformType.Both))
                {
                    inferredPlatformType = PlatformType.System;
                }
                else if (distinctPlatformTypes.Contains(PlatformType.Tenant) && !distinctPlatformTypes.Contains(PlatformType.Both))
                {
                    inferredPlatformType = PlatformType.Tenant;
                }
            }

            var moduleNode = new NavigationNode(moduleName, moduleDisplayName, modulePath)
            {
                ModuleName = moduleName,
                Permission = moduleName.ToCamelCase(),
                Icon = moduleAttr?.Icon,
                PlatformType = inferredPlatformType,
                OriginalPlatformType = inferredPlatformType
            };

            foreach (var controller in controllers)
            {
                var navAttr = controller.Key.GetCustomAttribute<NavigationAttribute>();

                NavigationNode controllerNode = null;

                // 只有明确定义了 NavigationAttribute 且未隐藏的控制器才创建导航节点
                if (navAttr != null && !navAttr.Hidden)
                {
                    var controllerName = controller.Key.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase).ToCamelCase();
                    var controllerPath = $"{modulePath}/{controllerName}";
                    controllerNode = CreateNavigationNode(moduleName, navAttr, controllerName, controller.Key, controllerPath);
                }

                // 如果成功创建了控制器节点，则添加到模块节点中并处理动作方法
                if (controllerNode != null)
                {
                    moduleNode.Children.Add(controllerNode);

                    foreach (var action in controller)
                    {
                        var actionNavAttr = action.MethodInfo.GetCustomAttribute<NavigationAttribute>();
                        if (actionNavAttr != null && !actionNavAttr.Hidden)
                        {
                            var actionName = action.ActionName.ToCamelCase();
                            var actionPath = $"{controllerNode.Path}/{actionName}";
                            var actionNode = CreateNavigationNode(moduleName, actionNavAttr, actionName, action.MethodInfo, actionPath);
                            controllerNode.Children.Add(actionNode);
                        }
                    }
                }
            }

            // 如果模块没有任何子节点（所有控制器都被隐藏），则不返回该模块
            if (moduleNode.Children.Count == 0)
            {
                return [];
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
                ModuleName = moduleName,
                PlatformType = attr.PlatformType,
                OriginalPlatformType = attr.PlatformType,
                Group = attr.Group,
                Tags = attr.Tags ?? [],
                RequireAuth = attr.RequireAuth,
                IsExperimental = attr.IsExperimental,
                MinVersion = attr.MinVersion,
                MaxVersion = attr.MaxVersion,
                SupportedDevices = attr.SupportedDevices ?? ["desktop", "tablet", "mobile"],
                Priority = attr.Priority,
                Shortcut = attr.Shortcut,
                Badge = attr.Badge,
                BadgeType = attr.BadgeType,
                Visible = attr.Visible
            };

            // 解析元数据JSON
            if (!string.IsNullOrEmpty(attr.MetaDataJson))
            {
                try
                {
                    node.MetaData = JsonConvert.DeserializeObject<Dictionary<string, object>>(attr.MetaDataJson) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to parse metadata JSON for navigation node {defaultName}: {attr.MetaDataJson}");
                    node.MetaData = new();
                }
            }

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
                    // 移除Async后缀以与ASP.NET Core运行时行为保持一致
                    var actionName = methodInfo.Name.RemovePostFix("Async");
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
                // 优先使用控制器类本身定义的Route属性，如果没有则使用继承的
                var routeAttr = controllerType2.GetCustomAttribute<RouteAttribute>(inherit: false);
                if (routeAttr == null)
                {
                    routeAttr = controllerType2.GetCustomAttribute<RouteAttribute>(inherit: true);
                }
                var route = routeAttr?.Template?.Replace("[controller]", controllerName.ToCamelCase()) ?? string.Empty;

                if (!string.IsNullOrEmpty(route))
                {
                    node.Route = route;
                }
            }

            return node;
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
                Link = item.Link,
                PlatformType = item.PlatformType,
                OriginalPlatformType = item.PlatformType,
                Group = item.Group,
                Tags = item.Tags ?? [],
                MetaData = item.MetaData ?? new(),
                RequireAuth = item.RequireAuth,
                IsExperimental = item.IsExperimental,
                MinVersion = item.MinVersion,
                MaxVersion = item.MaxVersion,
                SupportedDevices = item.SupportedDevices ?? ["desktop", "tablet", "mobile"],
                Priority = item.Priority,
                Shortcut = item.Shortcut,
                Badge = item.Badge,
                BadgeType = item.BadgeType
            };

            if (item.Children?.Any() == true)
            {
                node.Children.AddRange(item.Children.Select(child =>
                    ConvertToNavigationNode(child, moduleName)));
            }

            return node;
        }

        /// <summary>
        /// 获取所有模块名称
        /// </summary>
        private List<string> GetAllModuleNames()
        {
            var codeModules = GetCurrentModules();
            var configModules = GetConfigModules();

            _logger.LogDebug("Code modules: {Modules}", string.Join(", ", codeModules));
            _logger.LogDebug("Config modules: {Modules}", string.Join(", ", configModules));

            var allModules = codeModules.Union(configModules)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();

            _logger.LogDebug("All modules: {Modules}", string.Join(", ", allModules));
            return allModules;
        }

        /// <summary>
        /// 获取代码中定义的模块
        /// </summary>
        private List<string> GetCurrentModules()
        {
            return _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(x => x.ControllerTypeInfo)
                .Distinct()
                .Select(c => c.GetCustomAttribute<ModuleAttribute>()?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 获取配置文件中定义的模块
        /// </summary>
        private List<string> GetConfigModules()
        {
            var configSection = _configuration.GetSection(CONFIG_SECTION_KEY);
            if (!configSection.Exists())
            {
                return [];
            }

            // 获取配置节下的所有子节点名称，这些就是模块名
            return configSection.GetChildren()
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        /// <summary>
        /// 解析平台类型，处理继承逻辑
        /// </summary>
        private PlatformType ResolvePlatformType(PlatformType currentPlatformType, PlatformType? parentPlatformType = null)
        {
            // 如果当前节点设置为继承，则使用父级的配置
            if (currentPlatformType == PlatformType.Inherit)
            {
                // 如果父级存在且不是继承类型，则使用父级配置
                if (parentPlatformType.HasValue && parentPlatformType != PlatformType.Inherit)
                {
                    return parentPlatformType.Value;
                }

                // 如果父级也是继承或者没有父级，则默认为Both
                return PlatformType.Both;
            }

            return currentPlatformType;
        }

        /// <summary>
        /// 递归处理导航树的平台类型继承
        /// </summary>
        private void ProcessPlatformTypeInheritance(List<NavigationNode> nodes, PlatformType? parentPlatformType = null)
        {
            foreach (var node in nodes)
            {
                // 如果节点的原始平台类型是继承，则更新为解析后的类型
                if (node.OriginalPlatformType == PlatformType.Inherit)
                {
                    node.PlatformType = ResolvePlatformType(PlatformType.Inherit, parentPlatformType);
                }
                // 如果节点的平台类型本身是继承（可能是在序列化/反序列化过程中丢失了OriginalPlatformType），也需要处理
                else if (node.PlatformType == PlatformType.Inherit)
                {
                    node.PlatformType = ResolvePlatformType(PlatformType.Inherit, parentPlatformType);
                    // 如果OriginalPlatformType也是Inherit，更新它以便后续处理
                    if (node.OriginalPlatformType == PlatformType.Inherit)
                    {
                        node.OriginalPlatformType = node.PlatformType;
                    }
                }
                // 如果节点没有设置平台类型（默认值），确保设置为Both
                else if (node.PlatformType == PlatformType.None)
                {
                    node.PlatformType = PlatformType.Both;
                }

                // 递归处理子节点，传递当前节点的解析后平台类型
                if (node.Children.Any())
                {
                    ProcessPlatformTypeInheritance(node.Children, node.PlatformType);
                }
            }
        }
    }
}
