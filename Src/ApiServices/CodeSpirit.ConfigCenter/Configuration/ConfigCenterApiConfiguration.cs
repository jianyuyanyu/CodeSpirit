using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Startup;
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
public class ConfigCenterApiConfiguration : AuditAwareApiConfiguration
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
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
        
        // 配置标准数据库服务（多数据库支持、仓储模式）
        this.ConfigureStandardDatabaseServices<ConfigDbContext, MySqlConfigDbContext, SqlServerConfigDbContext>(
            services, configuration);
        
        // 配置标准基础设施服务（事件总线、HTTP客户端）+ 可选组件（多租户）
        // 注意：配置中心不使用设置管理
        this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
        {
            s.AddCodeSpiritMultiTenant(c);
        });
        
        // 添加LLM服务
        AddLLMServices(services);
        
        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
        
        // 注册配置变更事件处理器（订阅事件并推送给本地SSE客户端）
        services.AddEventHandler<Events.ConfigChangedEvent, ConfigChangedEventHandler>();
        
        // 注册SSE连接管理器（自动管理健康状态）
        services.AddSingleton<SseConnectionManager>();
        
        // 注意：审计元数据过滤器已由 BaseApiConfiguration 根据配置自动添加
    }
    
    
    /// <summary>
    /// 配置配置中心特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 配置标准中间件（聚合器）+ 可选组件（多租户、AI表单填充）
        await this.ConfigureStandardMiddlewareAsync(app, a =>
        {
            a.UseCodeSpiritMultiTenant();
            a.UseAiFormFillEndpoints();
        });
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
