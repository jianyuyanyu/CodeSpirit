using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 多租户审计服务扩展
/// </summary>
public static class MultiTenantAuditExtensions
{
    /// <summary>
    /// 添加多租户审计服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMultiTenantAuditServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加基础审计服务
        AuditExtensions.AddAuditServices(services, configuration);
        
        // 注册多租户感知的审计服务（覆盖默认实现）
        services.AddScoped<IAuditService, AuditService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加多租户审计中间件配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMultiTenantAuditMiddleware(this IServiceCollection services, IConfiguration configuration)
    {
        // 绑定配置
        var auditOptions = new AuditOptions();
        configuration.GetSection("Audit").Bind(auditOptions);
        services.Configure<AuditOptions>(configuration.GetSection("Audit"));
        
        // 注册多租户相关服务
        services.AddScoped<IAuditService, AuditService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加审计服务（包含多租户支持）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAuditServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 绑定配置
        var auditOptions = new AuditOptions();
        configuration.GetSection("Audit").Bind(auditOptions);
        services.Configure<AuditOptions>(configuration.GetSection("Audit"));
        
        // 注册核心服务
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IElasticsearchService, ElasticsearchService>();
        services.AddScoped<IRabbitMQService, RabbitMQService>();
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        
        // 添加HTTP客户端（用于地理位置服务）
        services.AddHttpClient<IGeoLocationService, GeoLocationService>();
        
        // 添加Elasticsearch客户端
        if (auditOptions.Elasticsearch?.Urls?.Any() == true)
        {
            services.AddElasticsearch(configuration);
        }
        
        // 添加RabbitMQ客户端
        if (!string.IsNullOrEmpty(auditOptions.RabbitMQ?.ExchangeName))
        {
            services.AddRabbitMQ(configuration);
        }
        
        return services;
    }
    
    /// <summary>
    /// 添加Elasticsearch服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddElasticsearch(this IServiceCollection services, IConfiguration configuration)
    {
        // 这里可以添加Elasticsearch客户端配置
        // 具体实现取决于使用的Elasticsearch客户端库
        return services;
    }
    
    /// <summary>
    /// 添加RabbitMQ服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        // 这里可以添加RabbitMQ客户端配置
        // 具体实现取决于使用的RabbitMQ客户端库
        return services;
    }
} 