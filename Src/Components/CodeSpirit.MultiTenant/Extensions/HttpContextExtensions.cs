using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.MultiTenant.Extensions;

/// <summary>
/// HTTP上下文扩展方法
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>租户ID</returns>
    public static string? GetTenantId(this HttpContext context)
    {
        return context.Items["TenantId"] as string;
    }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>租户信息</returns>
    public static ITenantInfo? GetTenantInfo(this HttpContext context)
    {
        return context.Items["TenantInfo"] as ITenantInfo;
    }

    /// <summary>
    /// 设置租户ID
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="tenantId">租户ID</param>
    public static void SetTenantId(this HttpContext context, string tenantId)
    {
        context.Items["TenantId"] = tenantId;
    }

    /// <summary>
    /// 设置租户信息
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="tenantInfo">租户信息</param>
    public static void SetTenantInfo(this HttpContext context, ITenantInfo tenantInfo)
    {
        context.Items["TenantInfo"] = tenantInfo;
    }
} 