using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.MultiTenant.Middleware;

/// <summary>
/// 多租户中间件
/// 负责在请求处理过程中解析和验证租户信息
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

        try
        {
            // 解析租户ID
            var tenantId = await tenantResolver.ResolveTenantIdAsync();
            
            if (string.IsNullOrEmpty(tenantId))
            {
                await HandleTenantResolutionFailure(context, "无法解析租户ID");
                return;
            }

            // 获取租户信息
            var tenantInfo = await tenantResolver.GetTenantInfoAsync(tenantId);
            
            if (tenantInfo == null)
            {
                await HandleTenantResolutionFailure(context, $"租户不存在: {tenantId}");
                return;
            }

            if (!tenantInfo.IsActive)
            {
                await HandleTenantResolutionFailure(context, $"租户已禁用: {tenantId}");
                return;
            }

            // 将租户信息添加到HTTP上下文
            context.Items["TenantId"] = tenantId;
            context.Items["TenantInfo"] = tenantInfo;

            _logger.LogDebug("成功解析租户: {TenantId}", tenantId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多租户中间件处理异常");
            await HandleTenantResolutionFailure(context, "租户解析异常");
        }
    }

    /// <summary>
    /// 处理租户解析失败
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="message">错误消息</param>
    private async Task HandleTenantResolutionFailure(HttpContext context, string message)
    {
        _logger.LogWarning("租户解析失败: {Message}", message);

        switch (_options.FailureStrategy)
        {
            case TenantResolutionFailureStrategy.UseDefault:
                // 使用默认租户继续处理
                context.Items["TenantId"] = _options.DefaultTenantId;
                await _next(context);
                break;
                
            case TenantResolutionFailureStrategy.Return404:
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("租户不存在");
                break;
                
            case TenantResolutionFailureStrategy.ThrowException:
            default:
                throw new InvalidOperationException(message);
        }
    }
} 