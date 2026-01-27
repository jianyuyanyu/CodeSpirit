using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using CodeSpirit.Core;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Models;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 审计中间件（重构版）
/// </summary>
/// <remarks>
/// 使用辅助类拆分职责，代码更简洁易维护
/// </remarks>
public class AuditMiddlewareV2
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddlewareV2> _logger;
    private readonly AuditOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <remarks>
    /// 只注入 Singleton 服务。Scoped 服务（如 AuditContextBuilder、AuditLogBuilder、IAuditRecorder）
    /// 必须在 InvokeAsync 方法中通过参数注入。
    /// </remarks>
    public AuditMiddlewareV2(
        RequestDelegate next,
        ILogger<AuditMiddlewareV2> logger,
        IOptions<AuditOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <remarks>
    /// Scoped 服务必须在此方法参数中注入，因为中间件是 Singleton。
    /// </remarks>
    public async Task InvokeAsync(
        HttpContext context,
        IClientIpService clientIpService,
        ICurrentUser currentUser,
        AuditContextBuilder contextBuilder,
        AuditLogBuilder logBuilder,
        IAuditRecorder auditRecorder)
    {
        // 检查是否启用审计
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // 检查请求路径是否需要排除
        if (ShouldSkipAudit(context))
        {
            await _next(context);
            return;
        }

        // 开始计时
        var stopwatch = Stopwatch.StartNew();

        // 保存原始请求体
        string originalRequestBody;
        try
        {
            originalRequestBody = await GetRequestBodyAsync(context);
        }
        catch (IOException ex) when (ex.Message.Contains("client reset") || ex.Message.Contains("reset the request"))
        {
            // 客户端重置连接，跳过审计记录
            return;
        }
        catch (OperationCanceledException)
        {
            // 操作被取消（通常是客户端断开连接），跳过审计记录
            return;
        }

        // 记录响应
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var isSuccess = true;
        var errorMessage = string.Empty;
        var shouldSkipAudit = false;

        try
        {
            // 构建审计上下文
            var auditContext = contextBuilder.Build(context, currentUser, clientIpService);

            // 如果不记录匿名请求且用户未认证，则跳过审计
            if (!_options.LogAnonymousRequests && string.IsNullOrEmpty(auditContext.UserId))
            {
                await _next(context);
                return;
            }

            // 调用下一个中间件
            await _next(context);

            // 检查响应状态
            if (context.Response.StatusCode >= 400)
            {
                isSuccess = false;
                errorMessage = $"HTTP Error: {context.Response.StatusCode}";
            }

            // 如果不记录未授权请求且响应状态为401或403，则跳过审计
            if (!_options.LogUnauthorizedRequests && (context.Response.StatusCode == 401 || context.Response.StatusCode == 403))
            {
                return;
            }

            // 构建审计日志
            var (auditLog, shouldSkipFromBuilder) = await logBuilder.BuildAsync(
                auditContext,
                context,
                originalRequestBody,
                responseBodyStream,
                isSuccess,
                errorMessage,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            shouldSkipAudit = shouldSkipFromBuilder;

            // 记录审计日志
            if (!shouldSkipAudit)
            {
                try
                {
                    await auditRecorder.RecordAsync(auditLog);
                    context.Items["AuditProcessed"] = true;
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "记录审计日志失败");
                }
            }
            else
            {
                _logger.LogDebug("跳过审计日志记录 - 请求路径: {RequestPath}", auditLog.RequestPath);
            }
        }
        catch (IOException ex) when (ex.Message.Contains("client reset") || ex.Message.Contains("reset the request"))
        {
            // 客户端重置连接，跳过审计记录
            return;
        }
        catch (OperationCanceledException)
        {
            // 操作被取消（通常是客户端断开连接），跳过审计记录
            return;
        }
        catch (Exception ex)
        {
            // 记录处理过程中的错误，但不影响原始请求
            _logger.LogError(ex, "审计处理过程中发生错误");
            isSuccess = false;
            errorMessage = ex.Message;
        }
        finally
        {
            // 复制响应流到原始响应流
            try
            {
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }
            catch (IOException ex) when (ex.Message.Contains("client reset") || ex.Message.Contains("reset the request"))
            {
                // 客户端断开连接，无法复制响应，跳过复制操作
                _logger.LogDebug("客户端断开连接，跳过响应复制操作");
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，无法复制响应，跳过复制操作
                _logger.LogDebug("操作被取消，跳过响应复制操作");
            }
        }
    }

    /// <summary>
    /// 检查是否应该跳过审计
    /// </summary>
    private bool ShouldSkipAudit(HttpContext context)
    {
        // 检查是否为OPTIONS请求（CORS预检请求）
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("跳过审计 - OPTIONS请求: {Path}", context.Request.Path.Value);
            return true;
        }

        // 检查请求路径是否在排除列表中
        var requestPath = context.Request.Path.Value;

        // 检查是否为静态文件或其他需要排除的路径
        foreach (var excludePath in _options.ExcludedPathPrefixes)
        {
            if (requestPath.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("跳过审计 - 路径在排除列表中: {Path}", requestPath);
                return true;
            }
        }

        // 检查是否为健康检查或其他系统路径
        if (requestPath.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Contains("/metrics", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Contains("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("跳过审计 - 系统路径: {Path}", requestPath);
            return true;
        }

        // 检查是否为Blazor相关请求
        if (requestPath.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Contains("/blazor", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("跳过审计 - Blazor相关请求: {Path}", requestPath);
            return true;
        }

        // 检查是否为NoAudit控制器
        if (requestPath.Contains("/NoAudit", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("跳过审计 - NoAudit控制器: {Path}", requestPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取请求体
    /// </summary>
    private static async Task<string> GetRequestBodyAsync(HttpContext context)
    {
        try
        {
            // 启用重新读取请求体
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var requestBody = await reader.ReadToEndAsync();

            // 重置请求体位置，以便后续中间件可以读取
            context.Request.Body.Position = 0;

            return requestBody;
        }
        catch (IOException ex) when (ex.Message.Contains("client reset") || ex.Message.Contains("reset the request"))
        {
            // 客户端重置连接，返回空字符串
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            // 操作被取消（通常是客户端断开连接）
            return string.Empty;
        }
    }
}
