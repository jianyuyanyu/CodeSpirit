using CodeSpirit.Audit.Extensions;
using CodeSpirit.Messaging;
using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Messaging.Services;
using CodeSpirit.MessagingApi.Mappings;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodeSpirit.MessagingApi.Configuration;

/// <summary>
/// 消息系统API服务配置
/// </summary>
public class MessagingApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "messaging";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "messaging-api";
    
    /// <summary>
    /// 配置消息系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 使用多数据库配置助手配置消息系统数据库
        CodeSpirit.Shared.Data.DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<MessagingDbContext, MySqlMessagingDbContext, SqlServerMessagingDbContext>(
            services, configuration, ConnectionStringKey);
        
        // 从已有MessagingServices中迁移 - 保持原有功能
        services.AddMessagingServices(configuration);
        services.AddRealtimeChat();
        
        // 添加AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加控制器和API探索器
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        // 添加CORS策略
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOriginsWithCredentials", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithExposedHeaders("Content-Disposition");
            });
        });
        
        // 添加SignalR服务
        services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
        
        // 配置控制器和审计元数据过滤器
        ConfigureControllersWithAudit(services, configuration);
    }
    
    /// <summary>
    /// 配置控制器和审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void ConfigureControllersWithAudit(IServiceCollection services, IConfiguration configuration)
    {
        // 审计元数据过滤器将通过AddAuditMetadataFilter自动注册
        
        // 添加审计元数据过滤器到控制器
        services.AddControllers().AddAuditMetadataFilter();
    }
    
    /// <summary>
    /// 配置消息系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // HTTPS重定向
        app.UseHttpsRedirection();
        
        // 多租户中间件 - 必须在认证之前
        app.UseCodeSpiritMultiTenant();
        
        // 映射ChatHub
        app.MapHub<ChatHub>("/chathub");
        
        // 尝试映射默认端点（如果可用）
        try
        {
            // MapDefaultEndpoints 可能在某些配置中不可用，所以用try-catch包装
            var mapDefaultEndpointsMethod = typeof(WebApplication).GetMethod("MapDefaultEndpoints");
            if (mapDefaultEndpointsMethod != null)
            {
                mapDefaultEndpointsMethod.Invoke(app, null);
            }
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILogger<MessagingApiConfiguration>>();
            logger?.LogWarning(ex, "Could not map default endpoints: {Message}", ex.Message);
            Console.WriteLine($"Warning: Could not map default endpoints: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 消息系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        try
        {
            // 使用消息系统的数据库迁移和种子数据方法
            await app.Services.MigrateAndSeedMessagingDatabaseAsync();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILogger<MessagingApiConfiguration>>();
            logger?.LogError(ex, "初始化消息系统数据库时发生错误");
            Console.WriteLine($"初始化消息系统数据库时发生错误: {ex.Message}");
            throw;
        }
    }
}
