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
        /// 系统平台登录方法
        /// </summary>
        /// <param name="model">系统平台登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        Task<AuthResultDto> SystemLoginAsync(SystemLoginModel model, string ipAddress, string userAgent);

        /// <summary>
        /// 租户平台登录方法
        /// </summary>
        /// <param name="model">租户平台登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        Task<AuthResultDto> TenantLoginAsync(TenantLoginModel model, string ipAddress, string userAgent);

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
        /// <param name="tenantId">租户ID（可选）</param>
        /// <returns>登录结果</returns>
        Task<(bool Success, string Message, string Token, UserDto UserInfo)> ImpersonateLoginAsync(string userName, string tenantId = null);

        /// <summary>
        /// 第三方平台登录方法
        /// </summary>
        /// <param name="model">第三方登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        Task<AuthResultDto> ThirdPartyLoginAsync(ThirdPartyLoginModel model, string ipAddress, string userAgent);

        /// <summary>
        /// 微信小程序登录方法
        /// </summary>
        /// <param name="model">微信登录请求</param>
        /// <param name="ipAddress">客户端IP地址</param>
        /// <param name="userAgent">客户端信息</param>
        /// <returns>登录结果</returns>
        Task<AuthResultDto> WeChatLoginAsync(WeChatLoginModel model, string ipAddress, string userAgent);
    }
}