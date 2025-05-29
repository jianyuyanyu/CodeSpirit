namespace CodeSpirit.MultiTenant.Abstractions;

/// <summary>
/// 租户存储接口
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    Task<ITenantInfo?> GetTenantAsync(string tenantId);
    
    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync();
    
    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateTenantAsync(ITenantInfo tenantInfo);
    
    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo);
    
    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteTenantAsync(string tenantId);
    
    /// <summary>
    /// 检查租户是否存在
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    Task<bool> TenantExistsAsync(string tenantId);
} 