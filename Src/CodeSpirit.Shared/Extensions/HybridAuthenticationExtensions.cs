using CodeSpirit.Shared.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CodeSpirit.Shared.Extensions;

/// <summary>
/// 混合认证扩展方法（JWT + API Key）
/// </summary>
public static class HybridAuthenticationExtensions
{
    /// <summary>
    /// 添加混合认证（JWT + API Key）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureApiKey">API Key 认证配置（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHybridAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ApiKeyAuthenticationOptions>? configureApiKey = null)
    {
        // 添加认证服务
        var authBuilder = services.AddAuthentication(options =>
        {
            // 默认方案为 JWT Bearer
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        // 添加 JWT Bearer 认证
        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.IncludeErrorDetails = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                NameClaimType = "id"
            };
        });

        // 添加 API Key 认证
        authBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            "ApiKey",
            options =>
            {
                // 默认从配置读取 IdentityApi 基础 URL
                options.IdentityApiBaseUrl = configuration["IdentityApi:BaseUrl"] ?? "http://identity";

                // 应用自定义配置
                configureApiKey?.Invoke(options);
            });

        // 添加策略选择器（自动选择合适的认证方案）
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme,
                    "ApiKey")
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}

