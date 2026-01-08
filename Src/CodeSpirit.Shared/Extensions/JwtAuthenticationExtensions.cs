using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CodeSpirit.Shared.Extensions;

/// <summary>
/// JWT 认证扩展方法
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// 添加 JWT 认证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // 读取JWT配置，添加 null 检查
            var secretKey = configuration["Jwt:SecretKey"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            
            // 验证关键配置是否存在
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT 配置错误: 'Jwt:SecretKey' 配置项未找到。" +
                    "\n请检查：" +
                    "\n1. appsettings.json 中是否配置了 Jwt:SecretKey" +
                    "\n2. 配置中心是否正常工作并包含此配置" +
                    "\n3. 配置中心的配置是否已成功加载");
            }
            
            if (string.IsNullOrEmpty(issuer))
            {
                throw new InvalidOperationException("JWT 配置错误: 'Jwt:Issuer' 配置项未找到");
            }
            
            if (string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT 配置错误: 'Jwt:Audience' 配置项未找到");
            }
            
            options.RequireHttpsMetadata = false;
            options.IncludeErrorDetails = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero, // 设置时钟偏移量为0，即不允许过期的Token被接受
                RequireExpirationTime = true, // 要求Token必须有过期时间
                ValidIssuer = issuer,
                ValidAudience = audience,
                NameClaimType = "id"
            };
        });

        return services;
    }
} 