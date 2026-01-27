using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Claims;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 审计上下文构建器
/// </summary>
/// <remarks>
/// 专门负责从 HttpContext 构建审计上下文信息
/// </remarks>
public class AuditContextBuilder
{
    private readonly ILogger<AuditContextBuilder> _logger;
    private readonly ITenantContext? _tenantContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <remarks>
    /// ITenantContext 是可选的，如果未注册多租户服务则为 null。
    /// </remarks>
    public AuditContextBuilder(
        ILogger<AuditContextBuilder> logger,
        ITenantContext? tenantContext = null)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// 构建审计上下文
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="clientIpService">客户端IP服务</param>
    /// <returns>审计上下文</returns>
    public AuditContext Build(
        HttpContext context,
        ICurrentUser currentUser,
        IClientIpService clientIpService)
    {
        var auditContext = new AuditContext
        {
            RequestPath = context.Request.GetDisplayUrl(),
            RequestMethod = context.Request.Method,
            IpAddress = clientIpService.GetClientIpAddress(context),
            UserAgent = GetUserAgent(context),
            TenantId = GetTenantId(context, currentUser)
        };

        // 提取用户信息
        if (context.User.Identity?.IsAuthenticated == true)
        {
            auditContext.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            auditContext.UserName = context.User.FindFirstValue(ClaimTypes.Name);
        }

        return auditContext;
    }

    /// <summary>
    /// 获取用户代理信息
    /// </summary>
    private string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers.TryGetValue("User-Agent", out var userAgent)
            ? userAgent.ToString()
            : string.Empty;
    }

    /// <summary>
    /// 获取租户ID
    /// </summary>
    /// <remarks>
    /// 使用 ITenantContext 统一获取租户ID，确保与查询时使用相同的租户解析逻辑。
    /// 如果 ITenantContext 无法获取租户ID，则尝试从其他来源获取。
    /// </remarks>
    private string GetTenantId(HttpContext context, ICurrentUser currentUser)
    {
        try
        {
            // 1. 优先使用 ITenantContext（统一的租户解析逻辑，包含默认租户）
            var tenantId = _tenantContext?.TenantId;
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从 ITenantContext 获取租户ID: {TenantId}", tenantId);
                return tenantId;
            }

            // 2. 从当前用户获取租户ID（备用方案）
            tenantId = currentUser?.TenantId;
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从 ICurrentUser 获取租户ID: {TenantId}", tenantId);
                return tenantId;
            }

            // 3. 从HttpContext Items获取（多租户中间件可能已设置）
            if (context.Items.ContainsKey("TenantId"))
            {
                tenantId = context.Items["TenantId"] as string;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogDebug("从 HttpContext.Items 获取租户ID: {TenantId}", tenantId);
                    return tenantId;
                }
            }

            // 4. 从Header获取
            if (context.Request.Headers.TryGetValue("TenantId", out var headerValue))
            {
                tenantId = headerValue.FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogDebug("从 Header 获取租户ID: {TenantId}", tenantId);
                    return tenantId;
                }
            }

            // 5. 从Query参数获取
            if (context.Request.Query.TryGetValue("tenantId", out var queryValue))
            {
                tenantId = queryValue.FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogDebug("从 Query 获取租户ID: {TenantId}", tenantId);
                    return tenantId;
                }
            }

            _logger.LogDebug("无法获取租户ID，返回空字符串");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户ID时发生异常");
            return string.Empty;
        }
    }
}

/// <summary>
/// 审计上下文
/// </summary>
public class AuditContext
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求方法
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }
}
