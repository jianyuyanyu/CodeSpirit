using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using System.Collections.Concurrent;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 内存租户存储实现
/// 用于开发和测试环境
/// </summary>
public class MemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<string, ITenantInfo> _tenants;
    private readonly ILogger<MemoryTenantStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public MemoryTenantStore(ILogger<MemoryTenantStore> logger)
    {
        _logger = logger;
        _tenants = new ConcurrentDictionary<string, ITenantInfo>();
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    public Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync()
    {
        var activeTenants = _tenants.Values.Where(t => t.IsActive);
        return Task.FromResult(activeTenants);
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    public Task<bool> CreateTenantAsync(ITenantInfo tenantInfo)
    {
        try
        {
            var result = _tenants.TryAdd(tenantInfo.TenantId, tenantInfo);
            if (result)
            {
                _logger.LogInformation("成功创建租户: {TenantId}", tenantInfo.TenantId);
            }
            else
            {
                _logger.LogWarning("租户已存在: {TenantId}", tenantInfo.TenantId);
            }
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建租户失败: {TenantId}", tenantInfo.TenantId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    public Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo)
    {
        try
        {
            _tenants.AddOrUpdate(tenantInfo.TenantId, tenantInfo, (key, oldValue) => tenantInfo);
            _logger.LogInformation("成功更新租户: {TenantId}", tenantInfo.TenantId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新租户失败: {TenantId}", tenantInfo.TenantId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    public Task<bool> DeleteTenantAsync(string tenantId)
    {
        try
        {
            var result = _tenants.TryRemove(tenantId, out _);
            if (result)
            {
                _logger.LogInformation("成功删除租户: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("租户不存在: {TenantId}", tenantId);
            }
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除租户失败: {TenantId}", tenantId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 检查租户是否存在
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    public Task<bool> TenantExistsAsync(string tenantId)
    {
        return Task.FromResult(_tenants.ContainsKey(tenantId));
    }
}