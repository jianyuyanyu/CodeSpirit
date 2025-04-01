using CodeSpirit.IdentityApi.Dtos.User;

namespace CodeSpirit.IdentityApi.Dtos.Auth
{
    /// <summary>
    /// 身份验证结果数据传输对象
    /// </summary>
    public class AuthResultDto
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 提示消息
        /// </summary>
        public string Message { get; set; }
        
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
        public UserDto UserInfo { get; set; }
        
        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static AuthResultDto CreateSuccess(string token, string refreshToken, UserDto userInfo)
        {
            return new AuthResultDto
            {
                Success = true,
                Message = "认证成功",
                Token = token,
                RefreshToken = refreshToken,
                UserInfo = userInfo
            };
        }
        
        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static AuthResultDto CreateFailure(string message)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = message
            };
        }
    }
} 