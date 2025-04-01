// Services/AuthService.cs

using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Dtos.User;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 登录服务接口
    /// </summary>
    public interface IAuthService: IScopedDependency
    {
        /// <summary>
        /// 登录方法
        /// </summary>
        /// <param name="input">登录请求</param>
        /// <returns>登录结果</returns>
        Task<AuthResultDto> LoginAsync(LoginDto input);

        /// <summary>
        /// 退出登录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否成功</returns>
        Task<bool> LogoutAsync(long userId);

        /// <summary>
        /// 刷新令牌
        /// </summary>
        /// <param name="accessToken">当前访问令牌</param>
        /// <param name="refreshToken">刷新令牌</param>
        /// <returns>新的身份验证结果</returns>
        Task<AuthResultDto> RefreshTokenAsync(string accessToken, string refreshToken);
        
        /// <summary>
        /// 生成登录日志
        /// </summary>
        /// <param name="input">登录请求</param>
        /// <param name="userId">用户ID</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="failReason">失败原因</param>
        /// <returns>异步任务</returns>
        Task LogLoginAsync(LoginDto input, long? userId, bool isSuccess, string failReason = null);

        /// <summary>
        /// 模拟用户登录
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <returns>登录结果</returns>
        Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName);
    }
}