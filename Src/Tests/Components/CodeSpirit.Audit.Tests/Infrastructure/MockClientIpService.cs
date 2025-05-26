using Microsoft.AspNetCore.Http;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 模拟客户端IP服务，用于测试
/// </summary>
public class MockClientIpService : IClientIpService
{
    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    public string GetClientIpAddress(HttpContext httpContext)
    {
        return "127.0.0.1";
    }

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>客户端IP地址</returns>
    public string GetClientIpAddress(IHttpContextAccessor httpContextAccessor)
    {
        return "127.0.0.1";
    }
} 