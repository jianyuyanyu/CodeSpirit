using CodeSpirit.Audit.Middleware;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        // 获取审计配置节
        var auditConfig = configuration.GetSection("Audit").Exists() 
            ? configuration.GetSection("Audit") 
            : configuration;
        
        // 注册选项并添加验证
        services.Configure<AuditOptions>(auditConfig);
        services.AddSingleton<IValidateOptions<AuditOptions>, AuditOptionsValidator>();
        
        // 获取存储提供者类型（统一从 Audit:StorageProvider 读取）
        var storageProvider = auditConfig.GetValue<string>("StorageProvider") ?? "Elasticsearch";
        
        // 创建临时服务提供者以获取日志记录器
        using var tempProvider = services.BuildServiceProvider();
        var logger = tempProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(AuditExtensions));
        
        // 记录配置信息（使用结构化日志）
        logger?.LogInformation("审计服务配置: StorageProvider={StorageProvider}", storageProvider);
        
        // 注册RabbitMQ服务
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        
        // 根据配置注册存储服务
        switch (storageProvider.ToLowerInvariant())
        {
            case "greptimedb":
                logger?.LogInformation("使用 GreptimeDB 存储提供者");
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
                logger?.LogInformation("使用 Elasticsearch 存储提供者");
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
        
        // 注册拆分后的审计服务（职责单一）
        services.AddScoped<IAuditRecorder, AuditRecorder>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAuditStatisticsService, AuditStatisticsService>();
        
        // 注册审计服务（向后兼容，内部委托给拆分后的服务）
        services.AddScoped<IAuditService, AuditService>();
        
        // 注册中间件辅助类
        services.AddSingleton<Middleware.ControllerTypeRegistry>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Middleware.ControllerTypeRegistry>>();
            var actionDescriptorProvider = provider.GetService<IActionDescriptorCollectionProvider>();
            return new Middleware.ControllerTypeRegistry(logger, actionDescriptorProvider);
        });
        // 注册 ControllerTypeRegistry 为托管服务，以便在启动时初始化控制器类型缓存
        services.AddHostedService(provider => provider.GetRequiredService<Middleware.ControllerTypeRegistry>());
        services.AddScoped<Middleware.SensitiveDataProcessor>();
        services.AddScoped<Middleware.AuditContextBuilder>();
        services.AddScoped<Middleware.AuditLogBuilder>();
        
        // 注册地理位置服务
        services.AddSingleton<IGeoLocationService, GeoLocationService>();
        
        // 注册错误处理服务
        services.AddSingleton<IAuditErrorHandler, AuditErrorHandler>();
        
        // 注册审计指标
        services.AddSingleton<Metrics.AuditMetrics>();
        
        // 注册健康检查
        services.AddHealthChecks()
            .AddCheck<HealthChecks.AuditHealthCheck>("audit", tags: new[] { "audit", "ready" });
        
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
    /// 使用审计中间件（重构版本）
    /// </summary>
    /// <remarks>
    /// 使用重构后的中间件，代码更简洁，职责更清晰。
    /// 如需使用旧版本，请调用 <see cref="UseAuditMiddlewareLegacy"/>。
    /// </remarks>
    public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuditMiddlewareV2>();
    }

    /// <summary>
    /// 使用审计中间件（旧版本）
    /// </summary>
    /// <remarks>
    /// 使用原始的审计中间件实现，功能完整但代码较复杂。
    /// </remarks>
    [Obsolete]
    public static IApplicationBuilder UseAuditMiddlewareLegacy(this IApplicationBuilder app)
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
    
    /// <summary>
    /// 添加LLM审计服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLLMAuditServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 获取审计配置，LLM审计跟随统一的存储提供者配置
        var auditConfig = configuration.GetSection("Audit");
        
        // ⚠️ 重要：配置绑定 AuditOptions（包含 LLMAudit 配置）
        services.Configure<AuditOptions>(auditConfig);
        
        var storageProvider = auditConfig.GetValue<string>("StorageProvider") ?? "Elasticsearch";
        
        // 创建临时服务提供者以获取日志记录器
        using var tempProvider = services.BuildServiceProvider();
        var logger = tempProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(AuditExtensions));
        logger?.LogInformation("LLM审计服务配置: StorageProvider={StorageProvider}", storageProvider);
        
        // 根据配置注册存储服务
        switch (storageProvider.ToLowerInvariant())
        {
            case "greptimedb":
                logger?.LogInformation("LLM审计使用 GreptimeDB 存储提供者");
                services.AddHttpClient<Services.LLM.Implementation.LLMGreptimeDbStorageService>();
                services.AddScoped<Services.LLM.ILLMAuditStorageService, Services.LLM.Implementation.LLMGreptimeDbStorageService>();
                break;
            
            case "rabbitmq":
                logger?.LogInformation("LLM审计使用 RabbitMQ 存储提供者，将通过消息队列异步处理");
                // RabbitMQ模式下，LLM审计通过消息队列异步处理，但仍需要一个存储服务作为最终存储
                // 默认使用GreptimeDB作为最终存储，如果需要其他存储可以通过配置指定
                services.AddHttpClient<Services.LLM.Implementation.LLMGreptimeDbStorageService>();
                services.AddScoped<Services.LLM.ILLMAuditStorageService, Services.LLM.Implementation.LLMGreptimeDbStorageService>();
                break;
            
            case "elasticsearch":
            default:
                logger?.LogInformation("LLM审计使用 Elasticsearch 存储提供者");
                services.AddScoped<Services.LLM.ILLMAuditStorageService, Services.LLM.Implementation.LLMElasticsearchStorageService>();
                break;
        }
        
        // 注册LLM审计服务
        services.AddScoped<Services.LLM.ILLMAuditService, Services.LLM.Implementation.LLMAuditService>();
        
        // 注册LLM审计消费者后台服务
        services.AddHostedService<Services.LLM.Implementation.LLMAuditConsumerService>();
        
        // 注册可审计的LLM助手
        services.AddScoped<LLM.AuditableLLMAssistant>();
        
        return services;
    }
    
}
