namespace CodeSpirit.MultiTenant.Abstractions;

/// <summary>
/// 租户解析器接口
/// </summary>
public interface ITenantResolver : IScopedDependency
{
    /// <summary>
    /// 解析当前请求的租户ID
    /// </summary>
    /// <returns>租户ID，如果无法解析返回null</returns>
    Task<string?> ResolveTenantIdAsync();
    
    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    Task<ITenantInfo?> GetTenantInfoAsync(string tenantId);
    
    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    Task<IEnumerable<ITenantInfo>> GetActiveTenantInfosAsync();
    
    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    /// <returns>当前租户信息</returns>
    Task<ITenantInfo?> GetCurrentTenantInfoAsync();
} 