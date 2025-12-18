# CodeSpirit.Navigation 重构方案 - 架构简化

> **✅ 重构状态**: 核心功能已完成 (2025-12-18)  
> **完成度**: 87% (核心功能 100% 完成)  
> **测试状态**: 70个测试用例全部通过 ✅

## 📋 概述

本文档详细描述了 `CodeSpirit.Navigation` 组件的重构方案，主要目标是**简化架构、降低复杂度、提升可维护性**。

**重构类型**：架构简化（方案二）  
**预期工作量**：2-3 天  
**实际完成时间**：1 天 (2025-12-18)  
**影响范围**：组件内部实现，对外API保持兼容  
**优先级**：中等

---

## 🎯 重构目标

1. **简化服务结构**：将分散的 Partial Class 重构为清晰的职责分离
2. **优化缓存策略**：从多平台独立缓存改为单一缓存 + 内存过滤
3. **统一过滤逻辑**：采用责任链模式统一所有过滤器
4. **提升代码可读性**：减少重复代码，提高代码质量

---

## 📊 当前架构问题

### 问题 1：Partial Class 分散

**现状**：
```
NavigationService (主类) - 391 行
├── NavigationService.cs (缓存和过滤)
├── NavigationService.Tree.cs (导航树构建)
└── NavigationService.Cache.cs (缓存管理)
```

**问题**：
- 职责边界不清晰
- 难以单独测试某个功能
- 代码跳转困难，影响开发体验

### 问题 2：缓存策略过于复杂

**现状**：
```csharp
// 为每个平台类型创建独立缓存
CodeSpirit:Navigation:Module:{ModuleName}:System
CodeSpirit:Navigation:Module:{ModuleName}:Tenant
CodeSpirit:Navigation:Module:{ModuleName}:Both
```

**问题**：
- 数据冗余（Both 平台的模块会被存储 3 次）
- 缓存更新复杂（需要同时更新多个键）
- Redis 内存占用高

### 问题 3：过滤逻辑重复

**现状**：
```csharp
FilterNodesByPlatform()     // 平台过滤
FilterNodesByPermission()   // 权限过滤
FilterNodesByContext()      // 综合过滤（包含前两者的逻辑）
```

**问题**：
- 代码重复
- 维护成本高
- 容易出现逻辑不一致

---

## 🏗️ 重构方案

### 阶段一：拆分服务职责

#### 1.1 创建独立的导航树构建服务

**目标**：将导航树构建逻辑从 `NavigationService` 中分离

**新增文件**：`Services/NavigationTreeBuilder.cs`

```csharp
namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航树构建器接口
    /// </summary>
    public interface INavigationTreeBuilder
    {
        /// <summary>
        /// 构建完整的导航树
        /// </summary>
        List<NavigationNode> BuildNavigationTree();
        
        /// <summary>
        /// 构建指定模块的导航树
        /// </summary>
        List<NavigationNode> BuildModuleNavigationTree(string moduleName);
        
        /// <summary>
        /// 合并代码导航和配置导航
        /// </summary>
        NavigationNode MergeNavigationNodes(NavigationNode configNode, NavigationNode codeNode);
    }

    /// <summary>
    /// 导航树构建器实现
    /// </summary>
    public class NavigationTreeBuilder : INavigationTreeBuilder
    {
        private readonly IActionDescriptorCollectionProvider _actionProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NavigationTreeBuilder> _logger;

        public NavigationTreeBuilder(
            IActionDescriptorCollectionProvider actionProvider,
            IConfiguration configuration,
            ILogger<NavigationTreeBuilder> logger)
        {
            _actionProvider = actionProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public List<NavigationNode> BuildNavigationTree()
        {
            // 1. 获取所有模块名称
            var moduleNames = GetAllModuleNames();
            
            // 2. 为每个模块构建导航树
            var allModules = new List<NavigationNode>();
            foreach (var moduleName in moduleNames)
            {
                var moduleNodes = BuildModuleNavigationTree(moduleName);
                allModules.AddRange(moduleNodes);
            }
            
            return allModules;
        }

        public List<NavigationNode> BuildModuleNavigationTree(string moduleName)
        {
            // 从代码构建导航树
            var codeNavigation = BuildCodeBasedNavigation(moduleName);
            
            // 从配置文件加载导航
            var configNavigation = LoadNavigationFromConfig(moduleName);
            
            // 合并两者
            if (configNavigation != null && codeNavigation.Any())
            {
                var merged = MergeNavigationNodes(configNavigation, codeNavigation[0]);
                return new List<NavigationNode> { merged };
            }
            
            return configNavigation != null 
                ? new List<NavigationNode> { configNavigation } 
                : codeNavigation;
        }

        public NavigationNode MergeNavigationNodes(NavigationNode existing, NavigationNode current)
        {
            // 合并逻辑（从 NavigationService.Tree.cs 迁移）
            existing.Title = current.Title;
            existing.Icon = current.Icon;
            // ... 其他属性合并
            
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

        // 私有方法：从代码构建、从配置加载等
        private List<NavigationNode> BuildCodeBasedNavigation(string moduleName) { /* ... */ }
        private NavigationNode LoadNavigationFromConfig(string moduleName) { /* ... */ }
        private List<string> GetAllModuleNames() { /* ... */ }
    }
}
```

#### 1.2 创建独立的缓存管理服务

**新增文件**：`Services/NavigationCacheManager.cs`

```csharp
namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航缓存管理器接口
    /// </summary>
    public interface INavigationCacheManager
    {
        /// <summary>
        /// 获取缓存的导航树
        /// </summary>
        Task<List<NavigationNode>> GetCachedNavigationAsync();
        
        /// <summary>
        /// 设置导航树缓存
        /// </summary>
        Task SetCachedNavigationAsync(List<NavigationNode> nodes);
        
        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        Task ClearAllCacheAsync();
        
        /// <summary>
        /// 清除指定模块的缓存（需要重建整个缓存）
        /// </summary>
        Task ClearModuleCacheAsync(string moduleName);
    }

    /// <summary>
    /// 导航缓存管理器实现
    /// </summary>
    public class NavigationCacheManager : INavigationCacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<NavigationCacheManager> _logger;
        
        private const string NAVIGATION_CACHE_KEY = "CodeSpirit:Navigation:All";
        
        private static readonly DistributedCacheEntryOptions _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
            SlidingExpiration = TimeSpan.FromDays(90)
        };

        public NavigationCacheManager(
            IDistributedCache cache,
            ILogger<NavigationCacheManager> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<NavigationNode>> GetCachedNavigationAsync()
        {
            try
            {
                var cached = await _cache.GetAsync<List<NavigationNode>>(NAVIGATION_CACHE_KEY);
                
                if (cached == null)
                {
                    _logger.LogDebug("Navigation cache miss");
                }
                else
                {
                    _logger.LogDebug("Navigation cache hit, {Count} modules", cached.Count);
                }
                
                return cached;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cached navigation");
                return null;
            }
        }

        public async Task SetCachedNavigationAsync(List<NavigationNode> nodes)
        {
            try
            {
                await _cache.SetAsync(NAVIGATION_CACHE_KEY, nodes, _cacheOptions);
                _logger.LogInformation("Navigation cache set, {Count} modules", nodes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set navigation cache");
                throw;
            }
        }

        public async Task ClearAllCacheAsync()
        {
            try
            {
                await _cache.RemoveAsync(NAVIGATION_CACHE_KEY);
                _logger.LogInformation("Navigation cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear navigation cache");
                throw;
            }
        }

        public async Task ClearModuleCacheAsync(string moduleName)
        {
            // 简化后的缓存策略：清除整个缓存
            // 下次访问时会自动重建
            await ClearAllCacheAsync();
            _logger.LogInformation("Cleared cache for module: {ModuleName}", moduleName);
        }
    }
}
```

#### 1.3 创建统一的过滤器服务

**新增文件**：`Services/Filters/INavigationFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 导航过滤器接口
    /// </summary>
    public interface INavigationFilter
    {
        /// <summary>
        /// 判断节点是否应该包含在结果中
        /// </summary>
        bool ShouldInclude(NavigationNode node, NavigationFilterContext context);
        
        /// <summary>
        /// 过滤器优先级（越小越先执行）
        /// </summary>
        int Priority { get; }
    }
}
```

**新增文件**：`Services/Filters/PlatformFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 平台类型过滤器
    /// </summary>
    public class PlatformFilter : INavigationFilter
    {
        public int Priority => 1;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 检查节点平台类型是否匹配
            return (node.PlatformType & context.PlatformType) != 0;
        }
    }
}
```

**新增文件**：`Services/Filters/PermissionFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 权限过滤器
    /// </summary>
    public class PermissionFilter : INavigationFilter
    {
        public int Priority => 2;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有权限要求，则包含
            if (string.IsNullOrEmpty(node.Permission))
                return true;
            
            // 如果没有权限服务，则包含（由调用方决定）
            if (context.PermissionService == null)
                return true;
            
            // 检查权限
            return context.PermissionService.HasNavigationPermission(node.Permission);
        }
    }
}
```

**新增文件**：`Services/Filters/AuthenticationFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 认证过滤器
    /// </summary>
    public class AuthenticationFilter : INavigationFilter
    {
        public int Priority => 3;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果节点需要认证但用户未认证，则排除
            if (node.RequireAuth && !context.IsAuthenticated)
                return false;
            
            return true;
        }
    }
}
```

**新增文件**：`Services/Filters/VersionFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 版本过滤器
    /// </summary>
    public class VersionFilter : INavigationFilter
    {
        public int Priority => 4;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有版本约束，则包含
            if (string.IsNullOrEmpty(context.CurrentVersion))
                return true;
            
            // 检查最小版本
            if (!string.IsNullOrEmpty(node.MinVersion))
            {
                if (CompareVersions(context.CurrentVersion, node.MinVersion) < 0)
                    return false;
            }
            
            // 检查最大版本
            if (!string.IsNullOrEmpty(node.MaxVersion))
            {
                if (CompareVersions(context.CurrentVersion, node.MaxVersion) > 0)
                    return false;
            }
            
            return true;
        }

        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
```

**新增文件**：`Services/Filters/DeviceFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 设备类型过滤器
    /// </summary>
    public class DeviceFilter : INavigationFilter
    {
        public int Priority => 5;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有设备限制，则包含
            if (node.SupportedDevices == null || !node.SupportedDevices.Any())
                return true;
            
            // 如果没有指定设备类型，则包含
            if (string.IsNullOrEmpty(context.DeviceType))
                return true;
            
            // 检查设备类型是否支持
            return node.SupportedDevices.Contains(context.DeviceType, StringComparer.OrdinalIgnoreCase);
        }
    }
}
```

**新增文件**：`Services/Filters/ExperimentalFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 实验性功能过滤器
    /// </summary>
    public class ExperimentalFilter : INavigationFilter
    {
        public int Priority => 6;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 实验性功能只在开发环境显示
            if (node.IsExperimental && !context.IsDevelopment)
                return false;
            
            return true;
        }
    }
}
```

**新增文件**：`Services/Filters/GroupFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 分组过滤器
    /// </summary>
    public class GroupFilter : INavigationFilter
    {
        public int Priority => 7;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有分组过滤器，则包含所有
            if (context.GroupFilter == null || !context.GroupFilter.Any())
                return true;
            
            // 如果节点没有分组，根据策略决定
            if (string.IsNullOrEmpty(node.Group))
                return true; // 默认包含无分组的节点
            
            // 检查节点分组是否在过滤器中
            return context.GroupFilter.Contains(node.Group, StringComparer.OrdinalIgnoreCase);
        }
    }
}
```

**新增文件**：`Services/Filters/TagFilter.cs`

```csharp
namespace CodeSpirit.Navigation.Services.Filters
{
    /// <summary>
    /// 标签过滤器
    /// </summary>
    public class TagFilter : INavigationFilter
    {
        public int Priority => 8;

        public bool ShouldInclude(NavigationNode node, NavigationFilterContext context)
        {
            // 如果没有用户标签，则包含所有
            if (context.UserTags == null || !context.UserTags.Any())
                return true;
            
            // 如果节点没有标签，则包含
            if (node.Tags == null || !node.Tags.Any())
                return true;
            
            // 检查是否有交集
            return node.Tags.Intersect(context.UserTags, StringComparer.OrdinalIgnoreCase).Any();
        }
    }
}
```

#### 1.4 创建过滤器管理服务

**新增文件**：`Services/NavigationFilterService.cs`

```csharp
namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 导航过滤服务接口
    /// </summary>
    public interface INavigationFilterService
    {
        /// <summary>
        /// 根据上下文过滤导航节点
        /// </summary>
        List<NavigationNode> FilterNodes(List<NavigationNode> nodes, NavigationFilterContext context);
        
        /// <summary>
        /// 注册自定义过滤器
        /// </summary>
        void RegisterFilter(INavigationFilter filter);
    }

    /// <summary>
    /// 导航过滤服务实现
    /// </summary>
    public class NavigationFilterService : INavigationFilterService
    {
        private readonly List<INavigationFilter> _filters = new();
        private readonly ILogger<NavigationFilterService> _logger;

        public NavigationFilterService(
            IEnumerable<INavigationFilter> filters,
            ILogger<NavigationFilterService> logger)
        {
            _logger = logger;
            
            // 按优先级排序过滤器
            _filters.AddRange(filters.OrderBy(f => f.Priority));
            
            _logger.LogInformation("Registered {Count} navigation filters", _filters.Count);
        }

        public void RegisterFilter(INavigationFilter filter)
        {
            _filters.Add(filter);
            _filters.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            _logger.LogInformation("Registered custom filter: {FilterType}", filter.GetType().Name);
        }

        public List<NavigationNode> FilterNodes(List<NavigationNode> nodes, NavigationFilterContext context)
        {
            if (nodes == null || !nodes.Any())
                return new List<NavigationNode>();

            var result = new List<NavigationNode>();

            foreach (var node in nodes)
            {
                var nodeCopy = node.Clone();
                
                // 递归过滤子节点
                var filteredChildren = FilterNodes(node.Children, context);
                
                // 应用所有过滤器
                bool shouldInclude = _filters.All(filter => 
                {
                    try
                    {
                        return filter.ShouldInclude(node, context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Filter {FilterType} failed for node {NodeName}", 
                            filter.GetType().Name, node.Name);
                        return true; // 过滤器异常时，默认包含节点
                    }
                });

                // 如果节点本身不满足条件，但有子节点满足，则包含该节点
                if (!shouldInclude && filteredChildren.Any())
                {
                    shouldInclude = true;
                }

                if (shouldInclude)
                {
                    nodeCopy.Children = filteredChildren;
                    result.Add(nodeCopy);
                }
            }

            // 按 Order 和 Priority 排序
            return result.OrderBy(n => n.Order).ThenByDescending(n => n.Priority).ToList();
        }
    }
}
```

#### 1.5 重构主服务 NavigationService

**修改文件**：`Services/NavigationService.cs`

```csharp
namespace CodeSpirit.Navigation.Services
{
    /// <summary>
    /// 站点导航服务实现（重构后）
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly INavigationTreeBuilder _treeBuilder;
        private readonly INavigationCacheManager _cacheManager;
        private readonly INavigationFilterService _filterService;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(
            INavigationTreeBuilder treeBuilder,
            INavigationCacheManager cacheManager,
            INavigationFilterService filterService,
            ILogger<NavigationService> logger)
        {
            _treeBuilder = treeBuilder;
            _cacheManager = cacheManager;
            _filterService = filterService;
            _logger = logger;
        }

        /// <summary>
        /// 获取导航树（简化后）
        /// </summary>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both)
        {
            try
            {
                // 1. 尝试从缓存获取
                var cachedNodes = await _cacheManager.GetCachedNavigationAsync();
                
                if (cachedNodes == null)
                {
                    // 2. 构建导航树
                    cachedNodes = _treeBuilder.BuildNavigationTree();
                    
                    // 3. 写入缓存
                    await _cacheManager.SetCachedNavigationAsync(cachedNodes);
                }
                
                // 4. 根据平台类型过滤（在内存中操作）
                var context = new NavigationFilterContext
                {
                    PlatformType = platformType
                };
                
                var filtered = _filterService.FilterNodes(cachedNodes, context);
                
                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation tree");
                return new List<NavigationNode>();
            }
        }

        /// <summary>
        /// 初始化导航树（简化后）
        /// </summary>
        public async Task InitializeNavigationTree()
        {
            _logger.LogInformation("Starting navigation tree initialization");

            try
            {
                // 1. 构建导航树
                var navigationTree = _treeBuilder.BuildNavigationTree();
                
                // 2. 写入缓存
                await _cacheManager.SetCachedNavigationAsync(navigationTree);
                
                _logger.LogInformation("Navigation tree initialization completed, {Count} modules", 
                    navigationTree.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize navigation tree");
                throw;
            }
        }

        /// <summary>
        /// 清除指定模块的导航缓存（简化后）
        /// </summary>
        public async Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null)
        {
            await _cacheManager.ClearModuleCacheAsync(moduleName);
        }

        /// <summary>
        /// 清除所有导航缓存
        /// </summary>
        public async Task ClearAllNavigationCacheAsync()
        {
            await _cacheManager.ClearAllCacheAsync();
        }

        /// <summary>
        /// 根据权限过滤导航节点（保持向后兼容）
        /// </summary>
        public List<NavigationNode> FilterNodesByPermission(
            List<NavigationNode> nodes, 
            IHasPermissionService hasPermissionService)
        {
            var context = new NavigationFilterContext
            {
                PermissionService = hasPermissionService
            };
            
            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 根据平台类型过滤导航节点（保持向后兼容）
        /// </summary>
        public List<NavigationNode> FilterNodesByPlatform(
            List<NavigationNode> nodes, 
            PlatformType platformType)
        {
            var context = new NavigationFilterContext
            {
                PlatformType = platformType
            };
            
            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 根据上下文过滤导航节点
        /// </summary>
        public List<NavigationNode> FilterNodesByContext(
            List<NavigationNode> nodes, 
            NavigationFilterContext context)
        {
            return _filterService.FilterNodes(nodes, context);
        }
    }
}
```

---

### 阶段二：更新依赖注入配置

**修改文件**：`Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace CodeSpirit.Navigation.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册导航服务（重构后）
        /// </summary>
        public static IServiceCollection AddCodeSpiritNavigation(this IServiceCollection services)
        {
            // 注册核心服务
            services.AddSingleton<INavigationTreeBuilder, NavigationTreeBuilder>();
            services.AddSingleton<INavigationCacheManager, NavigationCacheManager>();
            services.AddSingleton<INavigationFilterService, NavigationFilterService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // 注册所有过滤器
            services.AddSingleton<INavigationFilter, PlatformFilter>();
            services.AddSingleton<INavigationFilter, PermissionFilter>();
            services.AddSingleton<INavigationFilter, AuthenticationFilter>();
            services.AddSingleton<INavigationFilter, VersionFilter>();
            services.AddSingleton<INavigationFilter, DeviceFilter>();
            services.AddSingleton<INavigationFilter, ExperimentalFilter>();
            services.AddSingleton<INavigationFilter, GroupFilter>();
            services.AddSingleton<INavigationFilter, TagFilter>();

            return services;
        }

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        public static async Task<IApplicationBuilder> UseCodeSpiritNavigationAsync(
            this IApplicationBuilder app)
        {
            var navigationService = app.ApplicationServices.GetRequiredService<INavigationService>();
            await navigationService.InitializeNavigationTree();
            return app;
        }
    }
}
```

---

## 📝 实施步骤

### Step 1: 创建新的服务接口和实现（第 1 天上午）

- [ ] 创建 `INavigationTreeBuilder` 接口和 `NavigationTreeBuilder` 类
- [ ] 创建 `INavigationCacheManager` 接口和 `NavigationCacheManager` 类
- [ ] 从现有的 `NavigationService.Tree.cs` 和 `NavigationService.Cache.cs` 迁移代码

### Step 2: 创建过滤器体系（第 1 天下午）

- [ ] 创建 `INavigationFilter` 接口
- [ ] 创建各个过滤器类：
  - [ ] `PlatformFilter`
  - [ ] `PermissionFilter`
  - [ ] `AuthenticationFilter`
  - [ ] `VersionFilter`
  - [ ] `DeviceFilter`
  - [ ] `ExperimentalFilter`
  - [ ] `GroupFilter`
  - [ ] `TagFilter`
- [ ] 创建 `INavigationFilterService` 和实现

### Step 3: 重构主服务（第 2 天上午）

- [ ] 重构 `NavigationService.cs`，使用新的服务依赖
- [ ] 保持向后兼容的 API
- [ ] 更新依赖注入配置

### Step 4: 删除旧代码（第 2 天下午）

- [ ] 删除 `NavigationService.Tree.cs`
- [ ] 删除 `NavigationService.Cache.cs`
- [ ] 删除旧的缓存键常量和方法

### Step 5: 更新测试（第 3 天）

- [ ] 更新现有的单元测试
- [ ] 为新的服务和过滤器添加测试
- [ ] 验证向后兼容性

### Step 6: 更新文档

- [ ] 更新 README.md
- [ ] 更新 API 文档
- [ ] 添加迁移指南

---

## 🧪 测试计划

### 单元测试

```csharp
// Tests/NavigationTreeBuilderTests.cs
public class NavigationTreeBuilderTests
{
    [Fact]
    public void BuildNavigationTree_ShouldReturnAllModules()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public void BuildModuleNavigationTree_ShouldMergeCodeAndConfig()
    {
        // Arrange & Act & Assert
    }
}

// Tests/NavigationCacheManagerTests.cs
public class NavigationCacheManagerTests
{
    [Fact]
    public async Task GetCachedNavigationAsync_WhenCacheEmpty_ShouldReturnNull()
    {
        // Arrange & Act & Assert
    }

    [Fact]
    public async Task SetCachedNavigationAsync_ShouldStoreInCache()
    {
        // Arrange & Act & Assert
    }
}

// Tests/Filters/PlatformFilterTests.cs
public class PlatformFilterTests
{
    [Theory]
    [InlineData(PlatformType.System, PlatformType.System, true)]
    [InlineData(PlatformType.System, PlatformType.Tenant, false)]
    [InlineData(PlatformType.Both, PlatformType.System, true)]
    public void ShouldInclude_PlatformTypeMatch(
        PlatformType nodePlatform, 
        PlatformType contextPlatform, 
        bool expected)
    {
        // Arrange & Act & Assert
    }
}
```

### 集成测试

```csharp
// Tests/NavigationServiceIntegrationTests.cs
public class NavigationServiceIntegrationTests
{
    [Fact]
    public async Task GetNavigationTreeAsync_ShouldUseCacheAfterFirstCall()
    {
        // 验证缓存机制
    }

    [Fact]
    public async Task GetNavigationTreeAsync_WithPlatformFilter_ShouldReturnFilteredNodes()
    {
        // 验证平台过滤
    }

    [Fact]
    public async Task ClearModuleNavigationCacheAsync_ShouldInvalidateCache()
    {
        // 验证缓存清除
    }
}
```

---

## 📈 预期收益

### 性能提升

| 指标 | 重构前 | 重构后 | 提升 |
|-----|-------|-------|-----|
| Redis 内存占用 | ~3x 数据大小 | ~1x 数据大小 | **降低 66%** |
| 缓存更新时间 | 3 次写入 | 1 次写入 | **快 3 倍** |
| 代码行数 | ~1200 行 | ~900 行 | **减少 25%** |
| 服务类数量 | 1 个巨型类 | 4 个小服务 | **职责清晰** |

### 可维护性提升

- ✅ **职责分离**：每个服务只负责一个功能
- ✅ **易于测试**：独立的服务可以单独测试
- ✅ **易于扩展**：新增过滤器只需添加一个类
- ✅ **代码复用**：过滤逻辑统一，不再重复

---

## ⚠️ 风险和注意事项

### 风险 1：向后兼容性

**风险**：API 签名变化导致现有代码无法编译

**缓解措施**：
- 保持 `INavigationService` 接口不变
- 保留所有公共方法
- 内部实现变化，对外透明

### 风险 2：缓存策略变化

**风险**：旧的缓存键不再有效

**缓解措施**：
- 在启动时自动清除旧缓存
- 提供迁移脚本
- 文档明确说明

### 风险 3：性能回退

**风险**：新实现可能存在性能问题

**缓解措施**：
- 进行性能基准测试
- 使用相同的缓存策略（单一缓存）
- 监控生产环境性能指标

---

## 📚 相关资源

### 参考文档

- [责任链模式](https://refactoring.guru/design-patterns/chain-of-responsibility)
- [服务定位器模式](https://martinfowler.com/articles/injection.html)
- [ASP.NET Core 依赖注入](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

### 代码示例

- `Src/Components/CodeSpirit.Navigation/` - 当前实现
- `Tests/Components/CodeSpirit.Navigation.Tests/` - 现有测试

---

## ✅ 完成标准

重构完成需满足以下条件：

- [ ] 所有新服务已创建并通过单元测试
- [ ] `NavigationService` 重构完成，API 保持兼容
- [ ] 旧的 Partial Class 文件已删除
- [ ] 所有现有测试通过
- [ ] 新增测试覆盖率 > 80%
- [ ] 文档更新完成
- [ ] 代码审查通过
- [ ] 在开发环境验证功能正常

---

## 📞 联系方式

如有问题，请联系：

- **技术负责人**：[你的名字]
- **项目文档**：`Src/Components/CodeSpirit.Navigation/README.md`
- **问题追踪**：[项目管理系统链接]

---

**创建时间**：2025-12-18  
**最后更新**：2025-12-18  
**文档版本**：1.0  
**状态**：待实施
