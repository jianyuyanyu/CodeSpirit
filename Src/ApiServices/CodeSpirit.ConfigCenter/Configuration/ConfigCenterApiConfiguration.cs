using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.ConfigCenter.Data;
using CodeSpirit.ConfigCenter.Data.Seeders;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Configuration;

/// <summary>
/// 配置中心API服务配置
/// </summary>
public class ConfigCenterApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "config";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "config-api";
    
    /// <summary>
    /// 配置配置中心特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置多数据库支持的配置中心数据库
        DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<ConfigDbContext, MySqlConfigDbContext, SqlServerConfigDbContext>(
            services, configuration, ConnectionStringKey);
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加LLM服务
        AddLLMServices(services);
        
        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
        
        // 注册事件总线（用于分布式通知）
        services.AddEventBus();
        
        // 注册配置变更事件处理器（订阅事件并推送给本地SSE客户端）
        services.AddEventHandler<Events.ConfigChangedEvent, ConfigChangedEventHandler>();
        
        // 注册SSE连接管理器（自动管理健康状态）
        services.AddSingleton<SseConnectionManager>();
        
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
    /// 配置配置中心特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 多租户中间件 - 必须在认证之前添加
        app.UseCodeSpiritMultiTenant();
        
        // 使用聚合器
        app.UseCodeSpiritAggregator();

        // 使用AI表单填充自动端点
        app.UseAiFormFillEndpoints();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 添加LLM服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddLLMServices(IServiceCollection services)
    {
        // 添加LLM服务，使用配置中心专用的设置提供者
        services.AddLLMServices();
    }
    
    /// <summary>
    /// 配置中心数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 初始化数据库
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ConfigCenterApiConfiguration>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        
        try
        {
            // 应用数据库迁移
            await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlConfigDbContext, SqlServerConfigDbContext>(
                services, configuration, logger, "ConfigCenter");
            
            // 初始化配置中心种子数据
            var seederService = services.GetRequiredService<ConfigSeederService>();
            await seederService.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化配置中心数据库时发生错误：{Message}", ex.Message);
            throw;
        }
    }
}
