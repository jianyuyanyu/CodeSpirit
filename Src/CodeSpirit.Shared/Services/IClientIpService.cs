using Microsoft.AspNetCore.Http;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// 客户端IP地址获取服务接口
/// </summary>
public interface IClientIpService
{
    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>客户端IP地址</returns>
    string GetClientIpAddress(HttpContext httpContext);

    /// <summary>
    /// 获取客户端真实IP地址
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>客户端IP地址</returns>
    string GetClientIpAddress(IHttpContextAccessor httpContextAccessor);
} 