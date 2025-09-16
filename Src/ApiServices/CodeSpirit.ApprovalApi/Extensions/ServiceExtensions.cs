using Microsoft.AspNetCore.Http;

namespace CodeSpirit.ApprovalApi.Extensions;

/// <summary>
/// 客户端IP服务扩展
/// </summary>
public static class ClientIpServiceExtensions
{
    /// <summary>
    /// 获取客户端IP
    /// </summary>
    /// <param name="clientIpService">客户端IP服务</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>客户端IP</returns>
    public static string GetClientIp(this IClientIpService clientIpService, IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            return clientIpService.GetClientIpAddress(httpContextAccessor) ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// 获取用户代理
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>用户代理</returns>
    public static string GetUserAgent(this IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            return httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// 当前用户扩展
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// 获取显示名称
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(this ICurrentUser currentUser)
    {
        return currentUser.UserName ?? currentUser.Id?.ToString() ?? "Unknown";
    }
}
