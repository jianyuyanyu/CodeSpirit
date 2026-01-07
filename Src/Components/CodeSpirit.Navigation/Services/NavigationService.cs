using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.DistributedLock;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Navigation
{
    /// <summary>
    /// 站点导航服务实现（重构后）
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly INavigationTreeBuilder _treeBuilder;
        private readonly INavigationCacheManager _cacheManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationFilterService _filterService;
        private readonly ILogger<NavigationService> _logger;

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        /// <param name="treeBuilder">导航树构建器</param>
        /// <param name="cacheManager">缓存管理器</param>
        /// <param name="serviceProvider">服务提供程序（用于动态解析 Scoped 服务）</param>
        /// <param name="filterService">过滤服务</param>
        /// <param name="logger">日志记录器</param>
        public NavigationService(
            INavigationTreeBuilder treeBuilder,
            INavigationCacheManager cacheManager,
            IServiceProvider serviceProvider,
            INavigationFilterService filterService,
            ILogger<NavigationService> logger)
        {
            _treeBuilder = treeBuilder ?? throw new ArgumentNullException(nameof(treeBuilder));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取导航树（简化后）
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>导航节点列表</returns>
        public async Task<List<NavigationNode>> GetNavigationTreeAsync(PlatformType platformType = PlatformType.Both)
        {
            try
            {
                // 1. 从缓存获取完整导航树（由 InitializeNavigationTree 初始化）
                var cachedNodes = await _cacheManager.GetCachedNavigationAsync();

                if (cachedNodes == null || !cachedNodes.Any())
                {
                    _logger.LogWarning("Navigation cache is empty. Please ensure InitializeNavigationTree is called during startup.");
                    return new List<NavigationNode>();  // 返回空列表而不是覆盖缓存
                }

                // 2. 根据平台类型在内存中过滤
                // 注意：这里只应用平台过滤，认证和权限过滤应该在 Controller 层通过 FilterNodesByContext 进行
                var context = new NavigationFilterContext
                {
                    PlatformType = platformType,
                    // 设置 IsAuthenticated = true，避免 AuthenticationFilter 过滤掉所有节点
                    // 实际的认证过滤应该在 Controller 层根据用户实际状态进行
                    IsAuthenticated = true
                };

                // 调试日志：记录缓存中的模块总数和平台类型
                var platformTypes = cachedNodes.Select(n => n.PlatformType).Distinct().ToList();
                _logger.LogDebug(
                    "Retrieved {Count} modules from cache for platform {PlatformType}. Node platform types: {NodePlatformTypes}",
                    cachedNodes.Count,
                    platformType,
                    string.Join(", ", platformTypes));

                var filtered = _filterService.FilterNodes(cachedNodes, context);
                
                _logger.LogDebug(
                    "Filtered result: {ResultCount} nodes for platform {PlatformType}",
                    filtered?.Count ?? 0,
                    platformType);

                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation tree");
                return new List<NavigationNode>();
            }
        }

        /// <summary>
        /// 根据权限过滤导航节点（保持向后兼容）
        /// </summary>
        /// <param name="nodes">导航节点列表</param>
        /// <param name="hasPermissionService">权限服务</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPermission(List<NavigationNode> nodes, IHasPermissionService hasPermissionService)
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
        /// <param name="nodes">导航节点列表</param>
        /// <param name="platformType">平台类型</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByPlatform(List<NavigationNode> nodes, PlatformType platformType)
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
        /// <param name="nodes">导航节点列表</param>
        /// <param name="context">过滤上下文</param>
        /// <returns>过滤后的导航节点列表</returns>
        public virtual List<NavigationNode> FilterNodesByContext(List<NavigationNode> nodes, NavigationFilterContext context)
        {
            return _filterService.FilterNodes(nodes, context);
        }

        /// <summary>
        /// 初始化导航树（简化后）
        /// </summary>
        public async Task InitializeNavigationTree()
        {
            _logger.LogInformation("Starting navigation tree initialization");

            const string lockKey = "Navigation:InitLock";

            try
            {
                // 1. 构建当前服务的导航树
                var navigationTree = _treeBuilder.BuildNavigationTree();

                _logger.LogInformation(
                    "Built navigation tree with {Count} modules for current service: {Modules}",
                    navigationTree.Count,
                    string.Join(", ", navigationTree.Select(m => $"{m.Name}({m.PlatformType})")));

                // 2. 使用分布式锁保护并发合并（直接使用 IDistributedLockProvider）
                using var scope = _serviceProvider.CreateScope();
                var lockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
                
                _logger.LogDebug("Acquiring distributed lock: {LockKey}", lockKey);
                
                using var lockHandle = await lockProvider.AcquireLockAsync(
                    lockKey,
                    TimeSpan.FromSeconds(30),  // 获取锁的超时时间
                    TimeSpan.FromSeconds(60)   // 锁的最大持有时间
                );
                
                _logger.LogDebug("Acquired distributed lock for navigation tree merge");
                
                // 3. 在锁保护下，通过 NavigationCacheManager 读取和合并
                var existingCacheData = await _cacheManager.GetCachedNavigationDataAsync();
                
                if (existingCacheData != null && existingCacheData.Nodes != null && existingCacheData.Nodes.Any())
                {
                    // 4. 合并策略：合并当前服务的模块到现有缓存中
                    var existingModuleNames = existingCacheData.Nodes
                        .Select(m => m.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var newModules = navigationTree
                        .Where(m => !existingModuleNames.Contains(m.Name))
                        .ToList();
                    
                    if (newModules.Any())
                    {
                        _logger.LogInformation(
                            "Merging {NewCount} new modules into existing cache with {ExistingCount} modules. New modules: {NewModules}",
                            newModules.Count,
                            existingCacheData.Nodes.Count,
                            string.Join(", ", newModules.Select(m => m.Name)));
                        
                        // 记录旧版本
                        var oldVersion = existingCacheData.Version;
                        
                        // 合并到现有缓存
                        var merged = existingCacheData.Nodes.ToList();
                        merged.AddRange(newModules);
                        
                        // 写入合并后的缓存（通过 NavigationCacheManager）
                        await _cacheManager.SetCachedNavigationAsync(merged);
                        
                        // 获取新版本并比较
                        var newVersion = await _cacheManager.GetCurrentVersionAsync();
                        
                        if (oldVersion != newVersion)
                        {
                            _logger.LogWarning(
                                "Navigation content changed! Old version: {OldVersion}, New version: {NewVersion}, Added {Count} modules", 
                                oldVersion, newVersion, newModules.Count);
                        }
                        
                        _logger.LogInformation(
                            "Navigation tree merged successfully. Total modules: {TotalCount}",
                            merged.Count);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No new modules to merge. Existing cache already contains all modules from current service. Version: {Version}",
                            existingCacheData.Version);
                    }
                }
                else
                {
                    // 5. 如果缓存不存在，直接写入
                    _logger.LogInformation(
                        "No existing cache found. Writing {Count} modules to cache: {Modules}",
                        navigationTree.Count,
                        string.Join(", ", navigationTree.Select(m => $"{m.Name}({m.PlatformType})")));
                    
                    await _cacheManager.SetCachedNavigationAsync(navigationTree);
                    var version = await _cacheManager.GetCurrentVersionAsync();
                    _logger.LogInformation(
                        "Navigation tree initialized, version: {Version}, {Count} modules", 
                        version, navigationTree.Count);
                }

                // 6. 验证缓存写入
                var cached = await _cacheManager.GetCachedNavigationAsync();
                _logger.LogInformation(
                    "Navigation tree initialization completed. Cached {CachedCount} modules, verified {VerifiedCount} modules",
                    navigationTree.Count,
                    cached?.Count ?? 0);
                
                _logger.LogDebug("Released distributed lock: {LockKey}", lockKey);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Failed to acquire distributed lock for navigation merge within timeout: {LockKey}", lockKey);
                throw new InvalidOperationException("导航树初始化失败：无法获取分布式锁，可能存在其他服务正在初始化", ex);
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
        /// <param name="moduleName">模块名称</param>
        /// <param name="platformType">平台类型，保留以保持API兼容性，但不再使用</param>
        public async Task ClearModuleNavigationCacheAsync(string moduleName, PlatformType? platformType = null)
        {
            // platformType 参数保留以保持 API 兼容性，但不再使用
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
        /// 获取当前导航版本号
        /// </summary>
        public async Task<string> GetNavigationVersionAsync()
        {
            return await _cacheManager.GetCurrentVersionAsync();
        }
    }
}
