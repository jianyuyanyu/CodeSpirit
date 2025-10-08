using CodeSpirit.Audit.Middleware;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.MultiTenant.Abstractions;
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
        
        // 获取存储提供者类型 - 明确检查多个可能的配置位置
        var storageProvider = auditConfig.GetValue<string>("StorageProvider") 
                            ?? configuration.GetValue<string>("Audit:StorageProvider")
                            ?? configuration.GetValue<string>("StorageProvider")
                            ?? "Elasticsearch";
        
        // 强制检查 Elasticsearch 配置是否存在
        var hasElasticsearchConfig = auditConfig.GetSection("Elasticsearch:Urls").Exists() 
                                   && auditConfig.GetSection("Elasticsearch:Urls").Get<List<string>>()?.Any() == true;
        
        // 如果有 Elasticsearch 配置但没有明确设置存储提供者，强制使用 Elasticsearch
        if (hasElasticsearchConfig && string.Equals(storageProvider, "Elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            storageProvider = "Elasticsearch";
            Console.WriteLine($"[审计配置] 检测到 Elasticsearch 配置，强制使用 Elasticsearch 存储提供者");
        }
        
        // 添加调试日志来确认配置
        Console.WriteLine($"[审计配置] === 配置检测开始 ===");
        Console.WriteLine($"[审计配置] 存储提供者: '{storageProvider}'");
        Console.WriteLine($"[审计配置] 配置节存在: {(auditConfig as IConfigurationSection)?.Exists() ?? true}");
        Console.WriteLine($"[审计配置] 配置节路径: {(auditConfig as IConfigurationSection)?.Path ?? "根配置"}");
        
        // 显示所有审计相关的配置键
        var allConfigs = auditConfig.AsEnumerable().Where(kv => !string.IsNullOrEmpty(kv.Key)).ToList();
        Console.WriteLine($"[审计配置] 找到 {allConfigs.Count} 个配置项:");
        foreach (var config in allConfigs.Take(10)) // 只显示前10个避免日志过长
        {
            Console.WriteLine($"[审计配置]   {config.Key} = {config.Value}");
        }
        
        // 特别检查 Elasticsearch 配置
        var esUrls = auditConfig.GetSection("Elasticsearch:Urls").Get<List<string>>();
        Console.WriteLine($"[审计配置] Elasticsearch URLs: {string.Join(", ", esUrls ?? new List<string>())}");
        Console.WriteLine($"[审计配置] === 配置检测结束 ===");
        
        // 注册RabbitMQ服务
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        
        // 根据配置注册存储服务
        switch (storageProvider.ToLowerInvariant())
        {
            case "greptimedb":
                Console.WriteLine("[审计配置] 使用GreptimeDB存储提供者，跳过Elasticsearch服务注册");
                // 注册GreptimeDB存储服务
                services.AddHttpClient<GreptimeDbAuditStorageService>();
                services.AddScoped<IAuditStorageService>(provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(nameof(GreptimeDbAuditStorageService));
                    var logger = provider.GetRequiredService<ILogger<GreptimeDbAuditStorageService>>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var tenantContext = provider.GetService<ITenantContext>();
                    
                    return new GreptimeDbAuditStorageService(httpClient, logger, configuration, tenantContext);
                });
                
                // 注册GreptimeDB初始化服务，确保在应用启动时主动初始化数据库
                services.AddHostedService<GreptimeDbInitializationService>();
                
                // 注册一个空的Elasticsearch服务实现，防止依赖注入错误
                services.AddSingleton<IElasticsearchService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<ElasticsearchService>>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    return new ElasticsearchService(logger, configuration);
                });
                break;
            
            case "elasticsearch":
            default:
                Console.WriteLine("[审计配置] 使用Elasticsearch存储提供者");
                // 注册Elasticsearch服务（默认）
                services.AddSingleton<IElasticsearchService, ElasticsearchService>();
                services.AddScoped<IAuditStorageService>(provider =>
                {
                    var elasticsearchService = provider.GetRequiredService<IElasticsearchService>();
                    var tenantContext = provider.GetService<ITenantContext>();
                    var logger = provider.GetRequiredService<ILogger<ElasticsearchAuditStorageService>>();
                    
                    return new ElasticsearchAuditStorageService(elasticsearchService, tenantContext, logger);
                });
                break;
        }
        
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
    
    /// <summary>
    /// 添加审计元数据过滤器
    /// 用于分布式环境中，通过响应头传递审计元数据给Web项目
    /// </summary>
    /// <param name="builder">MVC构建器</param>
    /// <returns>MVC构建器</returns>
    public static IMvcBuilder AddAuditMetadataFilter(this IMvcBuilder builder)
    {
        builder.Services.AddScoped<Filters.AuditMetadataFilter>();
        
        builder.AddMvcOptions(options =>
        {
            options.Filters.Add<Filters.AuditMetadataFilter>();
        });
        
        return builder;
    }
}
