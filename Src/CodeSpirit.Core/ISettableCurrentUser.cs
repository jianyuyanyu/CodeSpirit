namespace CodeSpirit.Core
{
    /// <summary>
    /// 可设置的当前用户接口，支持动态修改用户信息（主要用于事件处理等场景）
    /// </summary>
    public interface ISettableCurrentUser : ICurrentUser
    {
        /// <summary>
        /// 设置当前用户ID
        /// </summary>
        /// <param name="userId">用户ID</param>
        void SetUserId(long? userId);

        /// <summary>
        /// 设置当前租户ID
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        void SetTenantId(string tenantId);

        /// <summary>
        /// 设置当前用户名
        /// </summary>
        /// <param name="userName">用户名</param>
        void SetUserName(string userName);

        /// <summary>
        /// 重置为原始状态
        /// </summary>
        void Reset();
    }
}