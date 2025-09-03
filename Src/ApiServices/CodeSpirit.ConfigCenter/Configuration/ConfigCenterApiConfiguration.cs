using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.ConfigCenter.Data;
using CodeSpirit.ConfigCenter.Data.Seeders;
using CodeSpirit.ConfigCenter.Hubs;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.ConfigCenter.Services.Implementations;
using CodeSpirit.ConfigCenter.Services.Settings;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        // 配置配置中心数据库
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<ConfigDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ConfigDbContext>());
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加LLM服务
        AddLLMServices(services);
        
        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
        
        // 添加SignalR服务
        services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
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
        
        // 配置SignalR Hub路由
        app.MapHub<ConfigHub>("/config-hub");
        
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
        services.AddLLMServices<ConfigCenterLLMSettingsProvider>();
    }
    
    /// <summary>
    /// 配置中心数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 使用通用数据库初始化方法
        await app.InitializeApiDatabaseAsync<ConfigDbContext>(async services =>
        {
            try
            {
                // 初始化配置中心种子数据
                var seederService = services.GetRequiredService<ConfigSeederService>();
                await seederService.SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<ConfigCenterApiConfiguration>>();
                logger.LogError(ex, "初始化配置中心种子数据时发生错误");
                throw;
            }
        });
    }
}
