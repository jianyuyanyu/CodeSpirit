using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using CodeSpirit.Shared.Repositories;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    /// <summary>
    /// AuthService测试辅助类
    /// </summary>
    public static class AuthServiceTestHelper
    {
        /// <summary>
        /// 创建测试用的JWT令牌
        /// </summary>
        public static string CreateTestJwt(string userId = "1", string userName = "testuser")
        {
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwianRpIjoiMTIzNDU2Nzg5MCIsIm5hbWUiOiJUZXN0IFVzZXIiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJ1bmlxdWVfbmFtZSI6InRlc3R1c2VyIiwibmJmIjoxNjI0OTU0MTkxLCJleHAiOjE2MjU4MTgxOTEsImlhdCI6MTYyNDk1NDE5MX0.bWXGkYQZp1uZAvzn5Tms_JJUmZf9gS5Jn7QMXJLYd8A";
        }
        
        /// <summary>
        /// 为AuthService的ValidateToken方法安装一个特定的回调，以返回预期结果
        /// </summary>
        public static AuthService SetupValidateToken(
            this AuthService authService, 
            string token,
            long userId)
        {
            var validateTokenMethod = typeof(AuthService).GetMethod("ValidateToken", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (validateTokenMethod == null)
            {
                throw new InvalidOperationException("无法找到ValidateToken方法");
            }
            
            // 创建测试用ClaimsPrincipal
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(JwtRegisteredClaimNames.UniqueName, "testuser")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            
            // 创建测试用JwtSecurityToken
            var jwtToken = new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: null
            );
            
            // 使用反射将ValidateToken方法的实现替换成我们的自定义实现
            var fieldInfo = typeof(AuthService).GetField("_validateTokenCallback", 
                BindingFlags.NonPublic | BindingFlags.Instance);
                
            if (fieldInfo != null)
            {
                // 如果字段已经存在，直接设置值
                fieldInfo.SetValue(authService, new Func<string, (JwtSecurityToken, ClaimsPrincipal)?>(
                    (t) => t == token ? (jwtToken, principal) : null));
            }
            
            return authService;
        }
    }
} 