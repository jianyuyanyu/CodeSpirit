namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IIdentityAccessor
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        int? UserId { get; }

        /// <summary>
        /// 用户名（账户名）
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 租户ID
        /// </summary>
        int? TenantId { get; }

        /// <summary>
        /// 姓名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 身份类型
        /// </summary>
        int? IdentityType { get; }
        /// <summary>
        /// 角色Id
        /// </summary>
        int? RoleId { get; }

        IDisposable ChangeTenantId(int tenantId);
    }
}