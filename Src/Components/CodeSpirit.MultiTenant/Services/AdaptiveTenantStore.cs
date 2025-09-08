using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.Extensions.Options;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 自适应租户存储实现
/// 优先从数据库存储获取，失败后从API存储获取
/// </summary>
public class AdaptiveTenantStore : ITenantStore
{
    private readonly ITenantStore _primaryStore;
    private readonly ITenantStore _fallbackStore;
    private readonly ILogger<AdaptiveTenantStore> _logger;
    private readonly AdaptiveTenantStoreOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="primaryStore">主要存储（数据库存储）</param>
    /// <param name="fallbackStore">备用存储（API存储）</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">自适应存储配置选项</param>
    public AdaptiveTenantStore(
        ITenantStore primaryStore,
        ITenantStore fallbackStore,
        ILogger<AdaptiveTenantStore> logger,
        IOptions<AdaptiveTenantStoreOptions> options)
    {
        _primaryStore = primaryStore;
        _fallbackStore = fallbackStore;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        try
        {
            // 首先尝试从主要存储获取
            _logger.LogDebug("尝试从主要存储获取租户信息: {TenantId}", tenantId);
            var tenant = await _primaryStore.GetTenantAsync(tenantId);
            
            if (tenant != null)
            {
                _logger.LogDebug("成功从主要存储获取租户信息: {TenantId}", tenantId);
                return tenant;
            }

            _logger.LogDebug("主要存储中未找到租户，尝试从备用存储获取: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从主要存储获取租户信息失败，尝试从备用存储获取: {TenantId}", tenantId);
        }

        try
        {
            // 从备用存储获取
            var tenant = await _fallbackStore.GetTenantAsync(tenantId);
            
            if (tenant != null)
            {
                _logger.LogDebug("成功从备用存储获取租户信息: {TenantId}", tenantId);
                
                // 如果启用了同步到主存储，则尝试同步
                if (_options.SyncToPrimaryStore)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _primaryStore.CreateTenantAsync(tenant);
                            _logger.LogDebug("成功将租户信息同步到主要存储: {TenantId}", tenantId);
                        }
                        catch (Exception syncEx)
                        {
                            _logger.LogWarning(syncEx, "同步租户信息到主要存储失败: {TenantId}", tenantId);
                        }
                    });
                }
                
                return tenant;
            }

            _logger.LogDebug("备用存储中也未找到租户: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从备用存储获取租户信息失败: {TenantId}", tenantId);
        }

        return null;
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    public async Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync()
    {
        var tenants = new List<ITenantInfo>();

        try
        {
            // 首先从主要存储获取
            _logger.LogDebug("尝试从主要存储获取活跃租户列表");
            var primaryTenants = await _primaryStore.GetActiveTenantsAsync();
            tenants.AddRange(primaryTenants);
            _logger.LogDebug("从主要存储获取到 {Count} 个活跃租户", tenants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从主要存储获取活跃租户列表失败");
        }

        try
        {
            // 从备用存储获取
            _logger.LogDebug("尝试从备用存储获取活跃租户列表");
            var fallbackTenants = await _fallbackStore.GetActiveTenantsAsync();
            
            // 去重合并（以TenantId为准）
            var existingTenantIds = tenants.Select(t => t.TenantId).ToHashSet();
            var newTenants = fallbackTenants.Where(t => !existingTenantIds.Contains(t.TenantId));
            tenants.AddRange(newTenants);
            
            _logger.LogDebug("从备用存储获取到 {Count} 个新的活跃租户，总计 {Total} 个", 
                newTenants.Count(), tenants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从备用存储获取活跃租户列表失败");
        }

        return tenants;
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    public async Task<bool> CreateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null)
        {
            return false;
        }

        var primaryResult = false;
        var fallbackResult = false;

        // 尝试在主要存储中创建
        try
        {
            _logger.LogDebug("尝试在主要存储中创建租户: {TenantId}", tenantInfo.TenantId);
            primaryResult = await _primaryStore.CreateTenantAsync(tenantInfo);
            
            if (primaryResult)
            {
                _logger.LogDebug("成功在主要存储中创建租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在主要存储中创建租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 尝试在备用存储中创建
        try
        {
            _logger.LogDebug("尝试在备用存储中创建租户: {TenantId}", tenantInfo.TenantId);
            fallbackResult = await _fallbackStore.CreateTenantAsync(tenantInfo);
            
            if (fallbackResult)
            {
                _logger.LogDebug("成功在备用存储中创建租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在备用存储中创建租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 至少一个存储成功即认为成功
        return primaryResult || fallbackResult;
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    public async Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null)
        {
            return false;
        }

        var primaryResult = false;
        var fallbackResult = false;

        // 尝试在主要存储中更新
        try
        {
            _logger.LogDebug("尝试在主要存储中更新租户: {TenantId}", tenantInfo.TenantId);
            primaryResult = await _primaryStore.UpdateTenantAsync(tenantInfo);
            
            if (primaryResult)
            {
                _logger.LogDebug("成功在主要存储中更新租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在主要存储中更新租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 尝试在备用存储中更新
        try
        {
            _logger.LogDebug("尝试在备用存储中更新租户: {TenantId}", tenantInfo.TenantId);
            fallbackResult = await _fallbackStore.UpdateTenantAsync(tenantInfo);
            
            if (fallbackResult)
            {
                _logger.LogDebug("成功在备用存储中更新租户: {TenantId}", tenantInfo.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在备用存储中更新租户失败: {TenantId}", tenantInfo.TenantId);
        }

        // 至少一个存储成功即认为成功
        return primaryResult || fallbackResult;
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        var primaryResult = false;
        var fallbackResult = false;

        // 尝试在主要存储中删除
        try
        {
            _logger.LogDebug("尝试在主要存储中删除租户: {TenantId}", tenantId);
            primaryResult = await _primaryStore.DeleteTenantAsync(tenantId);
            
            if (primaryResult)
            {
                _logger.LogDebug("成功在主要存储中删除租户: {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在主要存储中删除租户失败: {TenantId}", tenantId);
        }

        // 尝试在备用存储中删除
        try
        {
            _logger.LogDebug("尝试在备用存储中删除租户: {TenantId}", tenantId);
            fallbackResult = await _fallbackStore.DeleteTenantAsync(tenantId);
            
            if (fallbackResult)
            {
                _logger.LogDebug("成功在备用存储中删除租户: {TenantId}", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在备用存储中删除租户失败: {TenantId}", tenantId);
        }

        // 至少一个存储成功即认为成功
        return primaryResult || fallbackResult;
    }

    /// <summary>
    /// 检查租户是否存在
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    public async Task<bool> TenantExistsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        try
        {
            // 首先检查主要存储
            _logger.LogDebug("检查租户是否在主要存储中存在: {TenantId}", tenantId);
            var existsInPrimary = await _primaryStore.TenantExistsAsync(tenantId);
            
            if (existsInPrimary)
            {
                _logger.LogDebug("租户在主要存储中存在: {TenantId}", tenantId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查主要存储中租户存在性失败: {TenantId}", tenantId);
        }

        try
        {
            // 检查备用存储
            _logger.LogDebug("检查租户是否在备用存储中存在: {TenantId}", tenantId);
            var existsInFallback = await _fallbackStore.TenantExistsAsync(tenantId);
            
            if (existsInFallback)
            {
                _logger.LogDebug("租户在备用存储中存在: {TenantId}", tenantId);
            }
            
            return existsInFallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查备用存储中租户存在性失败: {TenantId}", tenantId);
            return false;
        }
    }
}

