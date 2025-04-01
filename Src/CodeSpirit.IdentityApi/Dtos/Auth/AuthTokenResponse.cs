using CodeSpirit.IdentityApi.Dtos.User;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 认证令牌响应类，用于返回登录和刷新令牌的结果
    /// </summary>
    public class AuthTokenResponse
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserDto User { get; set; }
    }
} 