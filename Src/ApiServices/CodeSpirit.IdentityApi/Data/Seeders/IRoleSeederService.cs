using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 角色种子数据服务接口
    /// </summary>
    public interface IRoleSeederService : IScopedDependency
    {
        /// <summary>
        /// 确保角色存在
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <param name="description">描述</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns>角色实体</returns>
        Task<ApplicationRole> EnsureRoleExistsAsync(string roleName, string description, string tenantId);

        /// <summary>
        /// 批量创建角色
        /// </summary>
        /// <param name="roles">角色定义列表</param>
        /// <returns></returns>
        Task<List<ApplicationRole>> CreateRolesBatchAsync(List<RoleDefinition> roles);

        /// <summary>
        /// 获取预定义的系统角色
        /// </summary>
        /// <returns>系统角色定义列表</returns>
        List<RoleDefinition> GetSystemRoles();

        /// <summary>
        /// 获取预定义的业务角色
        /// </summary>
        /// <returns>业务角色定义列表</returns>
        List<RoleDefinition> GetBusinessRoles();
    }

    /// <summary>
    /// 角色定义
    /// </summary>
    public class RoleDefinition
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 是否为系统角色
        /// </summary>
        public bool IsSystemRole { get; set; }
    }
}