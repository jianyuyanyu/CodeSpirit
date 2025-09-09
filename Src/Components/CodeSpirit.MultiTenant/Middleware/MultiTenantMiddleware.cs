using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CodeSpirit.MultiTenant.Middleware;

/// <summary>
/// 多租户中间件（简化版）
/// 负责在请求处理过程中解析租户ID并设置到HTTP上下文
/// 租户验证由各服务按需进行，提供更好的灵活性和性能
/// </summary>
public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MultiTenantMiddleware> _logger;
    private readonly TenantOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">租户配置选项</param>
    public MultiTenantMiddleware(
        RequestDelegate next,
        ILogger<MultiTenantMiddleware> logger,
        IOptions<TenantOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 中间件执行方法
    /// 简化版本：只负责解析租户ID并设置到上下文，不进行验证
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="tenantResolver">租户解析器</param>
    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // 检查是否为需要跳过的请求
        if (ShouldSkipRequest(context.Request))
        {
            _logger.LogDebug("跳过租户解析: {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        try
        {
            // 仅解析租户ID，不进行验证
            var tenantId = await tenantResolver.ResolveTenantIdAsync();
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                // 将租户ID添加到HTTP上下文，供后续服务使用
                context.Items["TenantId"] = tenantId;
                _logger.LogDebug("成功解析租户ID: {TenantId}", tenantId);
            }
            else if (_options.FailureStrategy == TenantResolutionFailureStrategy.UseDefault 
                     && !string.IsNullOrEmpty(_options.DefaultTenantId))
            {
                // 仅在配置为使用默认租户时才设置
                context.Items["TenantId"] = _options.DefaultTenantId;
                _logger.LogDebug("使用默认租户ID: {TenantId}", _options.DefaultTenantId);
            }
            else
            {
                _logger.LogDebug("无法解析租户ID，跳过设置");
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "租户解析过程中发生异常，继续处理请求");
            await _next(context);
        }
    }


    /// <summary>
    /// 判断是否应该跳过租户解析的请求
    /// 包括内部API、健康检查、静态资源等
    /// </summary>
    /// <param name="request">HTTP请求</param>
    /// <returns>是否应该跳过租户解析</returns>
    private bool ShouldSkipRequest(HttpRequest request)
    {
        var path = request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // 1. 检查内部API，避免循环依赖
        var tenantInternalApiPaths = new[]
        {
            "/api/identity/internal/tenants/" // 租户信息API
        };

        if (tenantInternalApiPaths.Any(apiPath => 
            path.Contains(apiPath, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 2. 检查配置的跳过路径模式
        foreach (var pattern in _options.SkipPathPatterns)
        {
            if (IsPathMatch(path, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查路径是否匹配模式（支持简单通配符）
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <param name="pattern">匹配模式</param>
    /// <returns>是否匹配</returns>
    private static bool IsPathMatch(string path, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // 支持简单的通配符匹配
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
} 