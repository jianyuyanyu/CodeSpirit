using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Caching.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 统一租户存储实现
/// 按照内存→分布式缓存→API的固定优先级获取租户信息
/// </summary>
public class UnifiedTenantStore : ITenantStore
{
    private readonly MemoryTenantStore _memoryStore;
    private readonly DistributedTenantStore _distributedStore;
    private readonly ApiTenantStore _apiStore;
    private readonly ILogger<UnifiedTenantStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheService">统一缓存服务</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="tenantOptions">租户配置选项</param>
    /// <param name="apiStoreOptions">API存储配置选项</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <param name="logger">日志记录器</param>
    public UnifiedTenantStore(
        ICacheService cacheService,
        IHttpClientFactory httpClientFactory,
        IOptions<TenantOptions> tenantOptions,
        IOptions<ApiTenantStoreOptions> apiStoreOptions,
        ILoggerFactory loggerFactory,
        ILogger<UnifiedTenantStore> logger)
    {
        _logger = logger;
        
        // 初始化三个存储实现，使用注入的日志工厂
        _memoryStore = new MemoryTenantStore(loggerFactory.CreateLogger<MemoryTenantStore>());
        _distributedStore = new DistributedTenantStore(
            cacheService, 
            loggerFactory.CreateLogger<DistributedTenantStore>(),
            tenantOptions);
        _apiStore = new ApiTenantStore(
            httpClientFactory,
            loggerFactory.CreateLogger<ApiTenantStore>(),
            apiStoreOptions);
    }

    /// <summary>
    /// 获取租户信息
    /// 按照内存→分布式缓存→API的优先级获取
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo> GetTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        _logger.LogDebug("开始获取租户信息: {TenantId}", tenantId);

        // 1. 优先从内存获取
        try
        {
            _logger.LogDebug("尝试从内存存储获取租户信息: {TenantId}", tenantId);
            var tenant = await _memoryStore.GetTenantAsync(tenantId);
            if (tenant != null)
            {
                _logger.LogDebug("成功从内存存储获取租户信息: {TenantId}", tenantId);
                return tenant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从内存存储获取租户信息失败: {TenantId}", tenantId);
        }

        // 2. 从分布式缓存获取
        try
        {
            _logger.LogDebug("尝试从分布式缓存获取租户信息: {TenantId}", tenantId);
            var tenant = await _distributedStore.GetTenantAsync(tenantId);
            if (tenant != null)
            {
                _logger.LogDebug("成功从分布式缓存获取租户信息: {TenantId}", tenantId);
                
                // 同步到内存存储
                try
                {
                    await _memoryStore.CreateTenantAsync(tenant);
                    _logger.LogDebug("成功将租户信息同步到内存存储: {TenantId}", tenantId);
                }
                catch (Exception syncEx)
                {
                    _logger.LogWarning(syncEx, "同步租户信息到内存存储失败: {TenantId}", tenantId);
                }
                
                return tenant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从分布式缓存获取租户信息失败: {TenantId}", tenantId);
        }

        // 3. 最后从API获取
        try
        {
            _logger.LogDebug("尝试从API获取租户信息: {TenantId}", tenantId);
            var tenant = await _apiStore.GetTenantAsync(tenantId);
            if (tenant != null)
            {
                _logger.LogDebug("成功从API获取租户信息: {TenantId}", tenantId);
                
                // 同步到分布式缓存和内存存储
                try
                {
                    await _distributedStore.CreateTenantAsync(tenant);
                    await _memoryStore.CreateTenantAsync(tenant);
                    _logger.LogDebug("成功将租户信息同步到缓存层: {TenantId}", tenantId);
                }
                catch (Exception syncEx)
                {
                    _logger.LogWarning(syncEx, "同步租户信息到缓存层失败: {TenantId}", tenantId);
                }
                
                return tenant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从API获取租户信息失败: {TenantId}", tenantId);
        }

        _logger.LogDebug("未在任何存储中找到租户: {TenantId}", tenantId);
        return null;
    }

    /// <summary>
    /// 获取所有活跃租户
    /// 从API获取最新的活跃租户列表
    /// </summary>
    /// <returns>活跃租户列表</returns>
    public async Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync()
    {
        try
        {
            _logger.LogDebug("从API获取活跃租户列表");
            var tenants = await _apiStore.GetActiveTenantsAsync();
            
            // 将活跃租户同步到缓存层
            var tenantList = tenants.ToList();
            foreach (var tenant in tenantList)
            {
                try
                {
                    await _distributedStore.CreateTenantAsync(tenant);
                    await _memoryStore.CreateTenantAsync(tenant);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "同步活跃租户到缓存失败: {TenantId}", tenant.TenantId);
                }
            }
            
            _logger.LogDebug("获取到 {Count} 个活跃租户", tenantList.Count);
            return tenantList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃租户列表失败");
            return Enumerable.Empty<ITenantInfo>();
        }
    }

    /// <summary>
    /// 创建租户
    /// 在所有存储层创建租户信息
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    public async Task<bool> CreateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null)
        {
            return false;
        }

        var success = false;

        // 在API中创建（主要存储）
        try
        {
            success = await _apiStore.CreateTenantAsync(tenantInfo);
            if (success)
            {
                _logger.LogDebug("成功在API中创建租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "在API中创建租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 如果API创建成功，同步到缓存层
        if (success)
        {
            try
            {
                await _distributedStore.CreateTenantAsync(tenantInfo);
                await _memoryStore.CreateTenantAsync(tenantInfo);
                _logger.LogDebug("成功将租户同步到缓存层: {TenantId}", tenantInfo.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步租户到缓存层失败: {TenantId}", tenantInfo.TenantId);
            }
        }

        return success;
    }

    /// <summary>
    /// 更新租户
    /// 在所有存储层更新租户信息
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    public async Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null)
        {
            return false;
        }

        var success = false;

        // 在API中更新（主要存储）
        try
        {
            success = await _apiStore.UpdateTenantAsync(tenantInfo);
            if (success)
            {
                _logger.LogDebug("成功在API中更新租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "在API中更新租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 如果API更新成功，同步到缓存层
        if (success)
        {
            try
            {
                await _distributedStore.UpdateTenantAsync(tenantInfo);
                await _memoryStore.UpdateTenantAsync(tenantInfo);
                _logger.LogDebug("成功将租户更新同步到缓存层: {TenantId}", tenantInfo.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步租户更新到缓存层失败: {TenantId}", tenantInfo.TenantId);
            }
        }

        return success;
    }

    /// <summary>
    /// 删除租户
    /// 在所有存储层删除租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        var success = false;

        // 在API中删除（主要存储）
        try
        {
            success = await _apiStore.DeleteTenantAsync(tenantId);
            if (success)
            {
                _logger.LogDebug("成功在API中删除租户: {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "在API中删除租户失败: {TenantId}", tenantId);
        }

        // 无论API删除是否成功，都清理缓存层
        try
        {
            await _distributedStore.DeleteTenantAsync(tenantId);
            await _memoryStore.DeleteTenantAsync(tenantId);
            _logger.LogDebug("成功从缓存层删除租户: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从缓存层删除租户失败: {TenantId}", tenantId);
        }

        return success;
    }

    /// <summary>
    /// 检查租户是否存在
    /// 按照内存→分布式缓存→API的优先级检查
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    public async Task<bool> TenantExistsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        // 1. 检查内存存储
        try
        {
            if (await _memoryStore.TenantExistsAsync(tenantId))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查内存存储中租户存在性失败: {TenantId}", tenantId);
        }

        // 2. 检查分布式缓存
        try
        {
            if (await _distributedStore.TenantExistsAsync(tenantId))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查分布式缓存中租户存在性失败: {TenantId}", tenantId);
        }

        // 3. 检查API
        try
        {
            return await _apiStore.TenantExistsAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查API中租户存在性失败: {TenantId}", tenantId);
            return false;
        }
    }
}
