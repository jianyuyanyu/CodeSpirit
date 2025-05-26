using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 审计性能优化扩展
/// </summary>
public static class AuditPerformanceExtensions
{
    /// <summary>
    /// 添加审计性能监控
    /// </summary>
    public static IServiceCollection AddAuditPerformanceMonitoring(this IServiceCollection services)
    {
        // 注册性能计数器
        services.AddSingleton<AuditPerformanceCounters>();
        
        return services;
    }
    
    /// <summary>
    /// 使用审计性能监控中间件
    /// </summary>
    public static IApplicationBuilder UseAuditPerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditPerformanceMiddleware>();
    }
}

/// <summary>
/// 审计性能计数器
/// </summary>
public class AuditPerformanceCounters
{
    private long _totalRequests = 0;
    private long _auditedRequests = 0;
    private long _skippedRequests = 0;
    private long _totalProcessingTime = 0;
    
    /// <summary>
    /// 总请求数
    /// </summary>
    public long TotalRequests => _totalRequests;
    
    /// <summary>
    /// 已审计请求数
    /// </summary>
    public long AuditedRequests => _auditedRequests;
    
    /// <summary>
    /// 跳过的请求数
    /// </summary>
    public long SkippedRequests => _skippedRequests;
    
    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime => _auditedRequests > 0 ? (double)_totalProcessingTime / _auditedRequests : 0;
    
    /// <summary>
    /// 增加总请求数
    /// </summary>
    public void IncrementTotalRequests() => Interlocked.Increment(ref _totalRequests);
    
    /// <summary>
    /// 增加已审计请求数
    /// </summary>
    public void IncrementAuditedRequests() => Interlocked.Increment(ref _auditedRequests);
    
    /// <summary>
    /// 增加跳过的请求数
    /// </summary>
    public void IncrementSkippedRequests() => Interlocked.Increment(ref _skippedRequests);
    
    /// <summary>
    /// 添加处理时间
    /// </summary>
    public void AddProcessingTime(long milliseconds) => Interlocked.Add(ref _totalProcessingTime, milliseconds);
    
    /// <summary>
    /// 重置计数器
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _auditedRequests, 0);
        Interlocked.Exchange(ref _skippedRequests, 0);
        Interlocked.Exchange(ref _totalProcessingTime, 0);
    }
}

/// <summary>
/// 审计性能监控中间件
/// </summary>
public class AuditPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditPerformanceMiddleware> _logger;
    private readonly AuditPerformanceCounters _counters;
    
    public AuditPerformanceMiddleware(
        RequestDelegate next,
        ILogger<AuditPerformanceMiddleware> logger,
        AuditPerformanceCounters counters)
    {
        _next = next;
        _logger = logger;
        _counters = counters;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        _counters.IncrementTotalRequests();
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // 检查是否进行了审计（通过检查上下文中的标记）
            if (context.Items.ContainsKey("AuditProcessed"))
            {
                _counters.IncrementAuditedRequests();
                _counters.AddProcessingTime(stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _counters.IncrementSkippedRequests();
            }
            
            // 定期记录性能统计
            if (_counters.TotalRequests % 1000 == 0)
            {
                _logger.LogInformation("审计性能统计 - 总请求: {Total}, 已审计: {Audited}, 跳过: {Skipped}, 平均处理时间: {AvgTime}ms",
                    _counters.TotalRequests, _counters.AuditedRequests, _counters.SkippedRequests, _counters.AverageProcessingTime);
            }
        }
    }
} 