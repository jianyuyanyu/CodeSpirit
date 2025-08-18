using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 租户服务接口
    /// </summary>
    public interface ITenantService : IBaseCRUDService<TenantInfo, TenantDto, string, TenantCreateDto, TenantUpdateDto>, IScopedDependency
    {
        /// <summary>
        /// 根据租户ID获取租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户信息</returns>
        Task<TenantInfo> GetByTenantIdAsync(string tenantId);

        /// <summary>
        /// 检查租户ID是否存在
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsByTenantIdAsync(string tenantId);

        /// <summary>
        /// 启用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        Task<ApiResponse> EnableTenantAsync(string tenantId);

        /// <summary>
        /// 禁用租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>操作结果</returns>
        Task<ApiResponse> DisableTenantAsync(string tenantId);

        /// <summary>
        /// 获取租户统计信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>统计信息</returns>
        Task<object> GetTenantStatisticsAsync(string tenantId);

        /// <summary>
        /// 检查租户是否过期
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否过期</returns>
        Task<bool> IsTenantExpiredAsync(string tenantId);

        /// <summary>
        /// 续期租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="expiresAt">新的过期时间</param>
        /// <returns>操作结果</returns>
        Task<ApiResponse> RenewTenantAsync(string tenantId, DateTime expiresAt);

        /// <summary>
        /// 分页查询租户
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页结果</returns>
        Task<PageList<TenantDto>> GetPagedListAsync(TenantQueryDto queryDto);
    }
} 