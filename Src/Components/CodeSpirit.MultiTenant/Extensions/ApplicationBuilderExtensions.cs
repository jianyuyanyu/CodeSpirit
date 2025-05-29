using CodeSpirit.MultiTenant.Middleware;
using Microsoft.AspNetCore.Builder;

namespace CodeSpirit.MultiTenant.Extensions;

/// <summary>
/// 应用程序构建器扩展
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用多租户中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseCodeSpiritMultiTenant(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MultiTenantMiddleware>();
    }
} 