using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Data.Seeders
{
    /// <summary>
    /// 用户种子数据服务接口
    /// </summary>
    public interface IUserSeederService : IScopedDependency
    {
        /// <summary>
        /// 确保用户存在
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="email">邮箱</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="password">密码</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns>用户实体</returns>
        Task<ApplicationUser> EnsureUserExistsAsync(string userName, string email, string displayName, string password, string tenantId);

        /// <summary>
        /// 批量创建用户
        /// </summary>
        /// <param name="users">用户定义列表</param>
        /// <returns></returns>
        Task<List<ApplicationUser>> CreateUsersBatchAsync(List<UserDefinition> users);

        /// <summary>
        /// 确保用户角色关联存在
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="tenantId">租户ID</param>
        /// <returns></returns>
        Task EnsureUserRoleExistsAsync(long userId, long roleId, string tenantId);

        /// <summary>
        /// 获取预定义的系统用户
        /// </summary>
        /// <returns>系统用户定义列表</returns>
        List<UserDefinition> GetSystemUsers();

        /// <summary>
        /// 获取预定义的业务用户
        /// </summary>
        /// <returns>业务用户定义列表</returns>
        List<UserDefinition> GetBusinessUsers();
    }

    /// <summary>
    /// 用户定义
    /// </summary>
    public class UserDefinition
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// 是否为系统用户
        /// </summary>
        public bool IsSystemUser { get; set; }

        /// <summary>
        /// 分配的角色名称列表
        /// </summary>
        public List<string> Roles { get; set; } = new();
    }
}