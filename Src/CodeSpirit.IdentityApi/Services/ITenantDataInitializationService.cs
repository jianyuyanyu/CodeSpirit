using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 租户数据初始化服务接口
    /// </summary>
    public interface ITenantDataInitializationService : IScopedDependency
    {
        /// <summary>
        /// 为新创建的租户初始化默认数据
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>初始化结果</returns>
        Task<ApiResponse> InitializeTenantDataAsync(string tenantId, bool createDefaultAdmin = true);

        /// <summary>
        /// 为租户初始化默认角色
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>初始化结果</returns>
        Task<ApiResponse> InitializeTenantRolesAsync(string tenantId);

        /// <summary>
        /// 为租户初始化默认用户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>初始化结果</returns>
        Task<ApiResponse> InitializeTenantUsersAsync(string tenantId, bool createDefaultAdmin = true);

        /// <summary>
        /// 获取租户的默认角色定义
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>角色定义列表</returns>
        List<string> GetDefaultRolesForTenant(string tenantId);

        /// <summary>
        /// 获取租户的默认用户定义
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="createDefaultAdmin">是否创建默认管理员</param>
        /// <returns>用户定义列表</returns>
        List<(string UserName, string Email, string DisplayName, string Password, List<string> Roles)> GetDefaultUsersForTenant(string tenantId, bool createDefaultAdmin = true);
    }
} 