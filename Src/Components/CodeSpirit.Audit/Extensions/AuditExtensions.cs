using CodeSpirit.Audit.Middleware;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 审计扩展方法
/// </summary>
public static class AuditExtensions
{
    /// <summary>
    /// 添加审计服务
    /// </summary>
    public static IServiceCollection AddAuditServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 智能处理配置 - 检查是否已经是Audit配置节
        IConfiguration auditConfig;
        if (configuration.GetSection("Audit").Exists())
        {
            // 传入的是完整配置，获取Audit节
            auditConfig = configuration.GetSection("Audit");
        }
        else
        {
            // 传入的就是Audit配置节
            auditConfig = configuration;
        }
        
        // 注册选项
        services.Configure<AuditOptions>(auditConfig);
        
        // 注册Elasticsearch服务
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        
        // 注册RabbitMQ服务
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        
        // 注册审计服务
        services.AddScoped<IAuditService, AuditService>();
        
        // 注册地理位置服务
        services.AddSingleton<IGeoLocationService, GeoLocationService>();
        
        // 注册错误处理服务
        services.AddSingleton<IAuditErrorHandler, AuditErrorHandler>();
        
        // 注册内存缓存（如果尚未注册）
        if (!services.Any(x => x.ServiceType == typeof(IMemoryCache)))
        {
            services.AddMemoryCache();
        }
        
        // 添加HTTP客户端用于地理位置服务
        services.AddHttpClient("GeoLocation", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "CodeSpirit-Audit");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        
        services.AddAuditBackgroundServices();
        return services;
    }

    /// <summary>
    /// 使用审计中间件
    /// </summary>
    public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditMiddleware>();
    }
    
    /// <summary>
    /// 添加审计后台服务
    /// </summary>
    public static IServiceCollection AddAuditBackgroundServices(this IServiceCollection services)
    {
        // 注册审计日志消费者后台服务
        services.AddHostedService<AuditLogConsumerService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加审计性能监控
    /// </summary>
    public static IServiceCollection AddAuditPerformanceMonitoring(this IServiceCollection services)
    {
        // 注册性能监控中间件
        services.AddTransient<AuditPerformanceMiddleware>();
        
        return services;
    }
    
    /// <summary>
    /// 使用审计性能监控中间件
    /// </summary>
    public static IApplicationBuilder UseAuditPerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditPerformanceMiddleware>();
    }
    
    /// <summary>
    /// 使用传统命名的审计中间件方法 (兼容性)
    /// </summary>
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
    {
        return UseAuditMiddleware(app);
    }
    
    /// <summary>
    /// 使用传统命名的审计中间件方法 (兼容性)
    /// </summary>
    public static IApplicationBuilder UseAudit(this IApplicationBuilder app)
    {
        return UseAuditMiddleware(app);
    }
}
