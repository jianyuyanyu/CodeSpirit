namespace CodeSpirit.IdentityApi.Dtos.User
{
    /// <summary>
    /// 系统用户DTO，包含租户信息
    /// </summary>
    public class SystemUserDto : UserDto
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }
    }
}
