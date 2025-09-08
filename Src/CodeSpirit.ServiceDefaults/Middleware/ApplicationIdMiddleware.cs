using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ServiceDefaults.Middleware;

/// <summary>
/// 应用程序ID中间件，为每个请求添加应用程序标识到日志上下文
/// </summary>
public class ApplicationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApplicationIdMiddleware> _logger;
    private readonly string _applicationId;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="hostEnvironment">主机环境</param>
    public ApplicationIdMiddleware(RequestDelegate next, ILogger<ApplicationIdMiddleware> logger, IHostEnvironment hostEnvironment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationId = hostEnvironment.ApplicationName ?? "Unknown";
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 使用日志作用域添加应用程序ID
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ApplicationId"] = _applicationId,
            ["Service"] = _applicationId,
            ["RequestId"] = context.TraceIdentifier
        });

        // 将应用程序ID添加到HTTP上下文项中，供其他组件使用
        context.Items["ApplicationId"] = _applicationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求处理时发生异常，应用程序: {ApplicationId}, 请求路径: {RequestPath}", 
                _applicationId, context.Request.Path);
            throw;
        }
    }
}

/// <summary>
/// 应用程序ID中间件扩展方法
/// </summary>
public static class ApplicationIdMiddlewareExtensions
{
    /// <summary>
    /// 使用应用程序ID中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseApplicationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApplicationIdMiddleware>();
    }
}
