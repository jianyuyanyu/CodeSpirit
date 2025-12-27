using CodeSpirit.Amis.Helpers;
using CodeSpirit.Navigation.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航本地化服务实现
    /// </summary>
    public class NavigationLocalizationService : INavigationLocalizationService
    {
        private readonly CultureResolver _cultureResolver;
        private readonly ILogger<NavigationLocalizationService> _logger;

        /// <summary>
        /// 初始化导航本地化服务
        /// </summary>
        /// <param name="cultureResolver">文化信息解析器</param>
        /// <param name="logger">日志记录器</param>
        public NavigationLocalizationService(
            CultureResolver cultureResolver,
            ILogger<NavigationLocalizationService> logger)
        {
            _cultureResolver = cultureResolver;
            _logger = logger;
        }

        /// <summary>
        /// 本地化导航树，将导航树中的文本字段根据当前语言进行转换
        /// </summary>
        /// <param name="nodes">原始导航节点列表</param>
        /// <returns>本地化后的导航节点列表（深拷贝，不修改原始数据）</returns>
        public List<NavigationNode> LocalizeNavigationTree(List<NavigationNode> nodes)
        {
            if (nodes == null || !nodes.Any())
            {
                return nodes ?? new List<NavigationNode>();
            }

            var currentCulture = _cultureResolver.GetCurrentCulture();
            _logger.LogInformation("Localizing navigation tree for culture: {Culture} (Name: {CultureName}, IsNeutral: {IsNeutral})", 
                currentCulture.Name, currentCulture.DisplayName, currentCulture.IsNeutralCulture);

            return nodes.Select(node => LocalizeNode(node, currentCulture)).ToList();
        }

        /// <summary>
        /// 本地化单个导航节点（递归处理子节点）
        /// </summary>
        private NavigationNode LocalizeNode(NavigationNode node, CultureInfo culture)
        {
            // 深拷贝节点
            var localizedNode = node.Clone();

            // 记录节点信息用于调试
            _logger.LogDebug("Localizing node: Name={Name}, Title={Title}, TitleResourceKey={TitleResourceKey}, TitleResourceType={TitleResourceType}", 
                localizedNode.Name, localizedNode.Title, localizedNode.TitleResourceKey ?? "null", localizedNode.TitleResourceType ?? "null");

            // 本地化标题
            if (!string.IsNullOrEmpty(localizedNode.TitleResourceKey) && !string.IsNullOrEmpty(localizedNode.TitleResourceType))
            {
                _logger.LogInformation("Processing localization for node: {NodeName}, ResourceKey: {ResourceKey}, ResourceType: {ResourceType}", 
                    localizedNode.Name, localizedNode.TitleResourceKey, localizedNode.TitleResourceType);
                
                var localizedTitle = GetLocalizedText(
                    localizedNode.TitleResourceType,
                    localizedNode.TitleResourceKey,
                    culture);
                
                if (!string.IsNullOrEmpty(localizedTitle))
                {
                    _logger.LogInformation("Localized title for node {NodeName}: {OriginalTitle} -> {LocalizedTitle}", 
                        localizedNode.Name, localizedNode.Title, localizedTitle);
                    localizedNode.Title = localizedTitle;
                }
                else
                {
                    _logger.LogWarning("Failed to localize title for node {NodeName}, keeping original: {OriginalTitle}", 
                        localizedNode.Name, localizedNode.Title);
                }
            }
            else
            {
                _logger.LogDebug("Node {NodeName} has no resource key information (TitleResourceKey={TitleResourceKey}, TitleResourceType={TitleResourceType}), skipping localization", 
                    localizedNode.Name, localizedNode.TitleResourceKey ?? "null", localizedNode.TitleResourceType ?? "null");
            }

            // 本地化描述
            if (!string.IsNullOrEmpty(localizedNode.DescriptionResourceKey) && !string.IsNullOrEmpty(localizedNode.DescriptionResourceType))
            {
                var localizedDescription = GetLocalizedText(
                    localizedNode.DescriptionResourceType,
                    localizedNode.DescriptionResourceKey,
                    culture);
                
                if (!string.IsNullOrEmpty(localizedDescription))
                {
                    localizedNode.Description = localizedDescription;
                }
            }

            // 递归处理子节点
            if (node.Children != null && node.Children.Any())
            {
                localizedNode.Children = node.Children
                    .Select(child => LocalizeNode(child, culture))
                    .ToList();
            }
            else
            {
                // 如果没有子节点，确保 Children 列表被正确初始化
                localizedNode.Children = node.Children ?? new List<NavigationNode>();
            }

            return localizedNode;
        }

        /// <summary>
        /// 从资源文件中获取本地化文本
        /// </summary>
        /// <param name="resourceTypeName">资源类型的完整名称</param>
        /// <param name="resourceKey">资源键</param>
        /// <param name="culture">目标文化信息</param>
        /// <returns>本地化文本，如果获取失败则返回 null</returns>
        private string GetLocalizedText(string resourceTypeName, string resourceKey, CultureInfo culture)
        {
            try
            {
                // 根据类型名称加载类型
                // 首先尝试直接获取类型
                var resourceType = Type.GetType(resourceTypeName);
                
                // 如果直接获取失败，尝试在所有已加载的程序集中搜索
                if (resourceType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            resourceType = assembly.GetType(resourceTypeName);
                            if (resourceType != null)
                            {
                                _logger.LogDebug("Found resource type {ResourceType} in assembly {AssemblyName}", 
                                    resourceTypeName, assembly.GetName().Name);
                                break;
                            }
                        }
                        catch
                        {
                            // 忽略无法访问的程序集，继续搜索
                        }
                    }
                }
                
                if (resourceType == null)
                {
                    _logger.LogWarning("Resource type not found: {ResourceType}", resourceTypeName);
                    return null;
                }

                // 获取 ResourceManager 属性
                var resourceManagerProp = resourceType.GetProperty("ResourceManager", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (resourceManagerProp == null)
                {
                    _logger.LogWarning("ResourceManager property not found in type: {ResourceType}", resourceTypeName);
                    return null;
                }

                // 获取 ResourceManager 实例
                var resourceManager = resourceManagerProp.GetValue(null) as ResourceManager;
                if (resourceManager == null)
                {
                    _logger.LogWarning("ResourceManager is null for type: {ResourceType}", resourceTypeName);
                    return null;
                }
                
                // 记录 ResourceManager 的基名称，用于调试
                _logger.LogInformation("ResourceManager base name: {BaseName}, Assembly: {AssemblyName}, Culture: {Culture}", 
                    resourceManager.BaseName, resourceType.Assembly.GetName().Name, culture.Name);

                // 获取本地化文本
                // 首先尝试使用指定的文化获取
                _logger.LogInformation("Attempting to get localized text for key: {ResourceKey}, culture: {Culture} (Name: {CultureName}, IsNeutral: {IsNeutral})", 
                    resourceKey, culture.Name, culture.DisplayName, culture.IsNeutralCulture);
                
                // 尝试获取资源集以验证卫星程序集是否可用
                try
                {
                    var resourceSet = resourceManager.GetResourceSet(culture, true, false);
                    if (resourceSet != null)
                    {
                        _logger.LogInformation("ResourceSet found for culture: {Culture}", culture.Name);
                    }
                    else
                    {
                        _logger.LogWarning("ResourceSet not found for culture: {Culture}, trying to load satellite assembly", culture.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get ResourceSet for culture: {Culture}", culture.Name);
                }
                
                // 先获取默认资源文件中的值（用于比较，判断是否回退到默认资源）
                var zhCnCulture = new CultureInfo("zh-CN");
                var defaultText = resourceManager.GetString(resourceKey, zhCnCulture);
                _logger.LogDebug("Default text (zh-CN) for key: {ResourceKey} is: {DefaultText}", resourceKey, defaultText);
                
                var localizedText = resourceManager.GetString(resourceKey, culture);
                
                if (!string.IsNullOrEmpty(localizedText))
                {
                    // 如果当前文化是英文，且返回的文本与默认文本相同，说明可能回退到了默认资源文件
                    if (culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) && localizedText == defaultText)
                    {
                        _logger.LogWarning("Retrieved text for key: {ResourceKey} matches default text, may have fallen back to default resource. Culture: {Culture}, Value: {Value}", 
                            resourceKey, culture.Name, localizedText);
                        // 继续尝试其他方式获取英文资源
                    }
                    else
                    {
                        _logger.LogInformation("Successfully retrieved localized text for key: {ResourceKey}, culture: {Culture}, value: {Value}", 
                            resourceKey, culture.Name, localizedText);
                        return localizedText;
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve localized text for key: {ResourceKey}, culture: {Culture}", 
                        resourceKey, culture.Name);
                }
                
                // 如果获取失败，尝试使用父文化（例如 en-US -> en）
                if (!culture.IsNeutralCulture)
                {
                    try
                    {
                        var parentCulture = culture.Parent;
                        if (parentCulture != null && !parentCulture.Equals(CultureInfo.InvariantCulture))
                        {
                            _logger.LogDebug("Trying parent culture {ParentCulture} for key: {ResourceKey}", 
                                parentCulture.Name, resourceKey);
                            localizedText = resourceManager.GetString(resourceKey, parentCulture);
                            if (!string.IsNullOrEmpty(localizedText))
                            {
                                _logger.LogInformation("Found localized text using parent culture {ParentCulture} for key: {ResourceKey}, value: {Value}", 
                                    parentCulture.Name, resourceKey, localizedText);
                                return localizedText;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get text from parent culture for key: {ResourceKey}", resourceKey);
                    }
                }
                
                // 特殊处理：如果请求的是英文相关文化（en-US, en-GB等），尝试使用中性文化 "en"
                if (culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) && culture.Name != "en")
                {
                    try
                    {
                        var neutralEnCulture = new CultureInfo("en");
                        _logger.LogDebug("Trying neutral culture 'en' for key: {ResourceKey} (original culture: {Culture})", 
                            resourceKey, culture.Name);
                        localizedText = resourceManager.GetString(resourceKey, neutralEnCulture);
                        if (!string.IsNullOrEmpty(localizedText))
                        {
                            _logger.LogInformation("Found localized text using neutral culture 'en' for key: {ResourceKey}, value: {Value}", 
                                resourceKey, localizedText);
                            return localizedText;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to get text from neutral culture 'en' for key: {ResourceKey}", resourceKey);
                    }
                }
                
                // 如果仍然失败，尝试使用默认文化（zh-CN）
                try
                {
                    _logger.LogDebug("Trying default culture (zh-CN) for key: {ResourceKey}", resourceKey);
                    localizedText = resourceManager.GetString(resourceKey, zhCnCulture);
                    if (!string.IsNullOrEmpty(localizedText))
                    {
                        _logger.LogInformation("Found localized text using default culture (zh-CN) for key: {ResourceKey}, value: {Value}", 
                            resourceKey, localizedText);
                        return localizedText;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get text from default culture for key: {ResourceKey}", resourceKey);
                }
                
                _logger.LogWarning("Localized text not found for key: {ResourceKey} in type: {ResourceType} for culture: {Culture}", 
                    resourceKey, resourceTypeName, culture.Name);

                return localizedText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get localized text for key: {ResourceKey} in type: {ResourceType} for culture: {Culture}", 
                    resourceKey, resourceTypeName, culture.Name);
                return null;
            }
        }
    }
}

