using CodeSpirit.Audit.Middleware;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Infrastructure;

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
        // 注册选项
        services.Configure<AuditOptions>(configuration.GetSection("Audit"));
        
        // 注册Elasticsearch服务
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        
        // 注册RabbitMQ服务
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        
        // 注册审计服务
        services.AddScoped<IAuditService, AuditService>();
        
        // 注册地理位置服务
        services.AddSingleton<IGeoLocationService, GeoLocationService>();
        
        // 注册内存缓存（如果尚未注册）
        if (!services.Any(s => s.ServiceType == typeof(IMemoryCache)))
        {
            services.AddMemoryCache();
        }
        
        // 添加HTTP客户端
        services.AddHttpClient("GeoLocation", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "CodeSpirit-Audit");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        
        // 注册审计日志消费者后台服务
        services.AddHostedService<AuditLogConsumerService>();
        
        return services;
    }
    
    /// <summary>
    /// 使用审计中间件
    /// </summary>
    public static IApplicationBuilder UseAudit(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuditMiddleware>();
        return app;
    }
}

/// <summary>
/// 审计日志消费者后台服务
/// </summary>
public class AuditLogConsumerService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<AuditLogConsumerService> _logger;
    private readonly AuditOptions _options;
    private string _consumerTag;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogConsumerService(
        IRabbitMQService rabbitMQService,
        IElasticsearchService elasticsearchService,
        IGeoLocationService geoLocationService,
        IConfiguration configuration,
        ILogger<AuditLogConsumerService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _elasticsearchService = elasticsearchService;
        _geoLocationService = geoLocationService;
        _logger = logger;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options;
    }
    
    /// <summary>
    /// 执行服务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 确保索引存在
            await _elasticsearchService.CreateIndexAsync();
            
            // 订阅审计日志消息
            _consumerTag = _rabbitMQService.SubscribeMessage<Models.AuditLog>(async auditLog =>
            {
                try
                {
                    // 处理地理位置
                    await EnrichWithGeoLocationAsync(auditLog);
                    
                    // 保存到Elasticsearch
                    await _elasticsearchService.IndexDocumentAsync(auditLog);
                    _logger.LogDebug("审计日志已成功索引到Elasticsearch: {Id}", auditLog.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "审计日志消费失败: {Id}", auditLog.Id);
                    throw; // 重新抛出异常以便RabbitMQ可以重新入队消息
                }
            });
            
            _logger.LogInformation("审计日志消费者已启动");
            
            // 等待取消令牌
            await Task.Delay(-1, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审计日志消费者启动失败");
        }
    }
    
    /// <summary>
    /// 为审计日志添加地理位置信息
    /// </summary>
    private async Task EnrichWithGeoLocationAsync(Models.AuditLog auditLog)
    {
        // 如果未启用地理位置或IP地址为空，则跳过
        if (!_options.EnableGeoLocation || string.IsNullOrEmpty(auditLog.IpAddress))
        {
            return;
        }
        
        try
        {
            // 获取地理位置信息
            var geoLocation = await _geoLocationService.GetLocationByIpAsync(auditLog.IpAddress);
            if (geoLocation != null)
            {
                auditLog.Location = geoLocation;
                _logger.LogDebug("已为审计日志 {Id} 添加地理位置信息: {Country}, {City}", 
                    auditLog.Id, geoLocation.Country, geoLocation.City);
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不中断流程
            _logger.LogWarning(ex, "获取IP地址 {IpAddress} 的地理位置信息时发生错误", auditLog.IpAddress);
        }
    }
    
    /// <summary>
    /// 停止服务
    /// </summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("审计日志消费者正在停止");
        
        // 取消订阅
        if (!string.IsNullOrEmpty(_consumerTag))
        {
            _rabbitMQService.Unsubscribe(_consumerTag);
        }
        
        return base.StopAsync(cancellationToken);
    }
} 