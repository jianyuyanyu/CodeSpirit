using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Data.Models;
using System.Security.Claims;

namespace CodeSpirit.IdentityApi.Jwt
{
    /// <summary>
    /// JWT令牌处理接口
    /// </summary>
    public interface IJwtTokenHandler : IScopedDependency
    {
        /// <summary>
        /// 生成访问令牌
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>访问令牌</returns>
        Task<string> GenerateTokenAsync(ApplicationUser user);

        /// <summary>
        /// 验证令牌而不检查过期时间
        /// </summary>
        /// <param name="token">令牌</param>
        /// <returns>ClaimsPrincipal</returns>
        ClaimsPrincipal ValidateTokenWithoutLifetime(string token);
    }
} 