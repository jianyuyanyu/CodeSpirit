using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.MultiTenant.Abstractions;

/// <summary>
/// 租户上下文接口，提供统一的租户信息获取方式
/// 支持登录和免登录场景下的租户ID获取
/// </summary>
public interface ITenantContext : IScopedDependency
{
    /// <summary>
    /// 获取当前租户ID
    /// 优先级：JWT Claims -> HTTP上下文 -> 默认租户
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// 获取当前租户名称
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    /// <returns>当前租户信息，如果无法获取返回null</returns>
    Task<ITenantInfo?> GetCurrentTenantInfoAsync();

    /// <summary>
    /// 判断是否为指定租户
    /// </summary>
    /// <param name="tenantId">要检查的租户ID</param>
    /// <returns>如果当前租户匹配返回true，否则返回false</returns>
    bool IsInTenant(string tenantId);

    /// <summary>
    /// 判断当前是否有有效的租户上下文
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// 强制刷新租户上下文
    /// 用于在运行时切换租户或重新加载租户信息
    /// </summary>
    Task RefreshTenantContextAsync();

    /// <summary>
    /// 验证当前租户是否有效（存在且活跃）
    /// 用于按需验证场景
    /// </summary>
    /// <returns>如果租户有效返回true，否则返回false</returns>
    Task<bool> ValidateCurrentTenantAsync();

    /// <summary>
    /// 获取当前租户信息并验证其有效性
    /// 如果租户无效，根据配置的失败策略处理
    /// </summary>
    /// <returns>有效的租户信息，如果无效则根据策略返回null或抛出异常</returns>
    Task<ITenantInfo?> GetValidatedCurrentTenantInfoAsync();
}
