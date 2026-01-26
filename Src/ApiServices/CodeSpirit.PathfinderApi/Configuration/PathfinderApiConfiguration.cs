using CodeSpirit.AiFormFill;
using CodeSpirit.Aggregator;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.PathfinderApi.Data;
using CodeSpirit.PathfinderApi.Services.Implementations;
using CodeSpirit.PathfinderApi.Services.Interfaces;
using CodeSpirit.ScheduledTasks.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Performance;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.PathfinderApi.Configuration;

/// <summary>
/// Pathfinder API服务配置
/// </summary>
public class PathfinderApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "pathfinder";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "pathfinder-api";
    
    /// <summary>
    /// 配置Pathfinder特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置线程池（中等并发服务等级）
        var logger = services.BuildServiceProvider().GetService<ILoggerFactory>()?.CreateLogger<PathfinderApiConfiguration>();
        ThreadPoolConfiguration.ConfigureThreadPool(ThreadPoolConfiguration.ServiceTier.Medium, expectedInstances: 2, logger);
        
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
        
        // 配置标准数据库服务（多数据库支持、仓储模式）
        this.ConfigureStandardDatabaseServices<PathfinderDbContext, MySqlPathfinderDbContext, SqlServerPathfinderDbContext>(
            services, configuration);
        
        // 配置标准基础设施服务（事件总线、HTTP客户端）+ 可选组件（多租户）
        this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
        {
            s.AddCodeSpiritMultiTenant(c);
        });
        
        // 添加CodeSpirit缓存服务
        AddCachingServices(services, configuration);
        
        // 添加定时任务服务
        AddScheduledTasksServices(services, configuration);
        
        // 添加LLM服务
        AddLLMServices(services, configuration);
        
        // 添加AI表单填充服务
        AddAiFormFillServices(services);
        
        // 注册Pathfinder业务服务
        AddPathfinderServices(services);
        
        // 配置控制器和审计元数据过滤器
        ConfigureControllersWithAudit(services, configuration);
    }
    
    /// <summary>
    /// 添加LLM服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddLLMServices(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // 注册CodeSpirit LLM服务（包含LLMAssistant）
            services.AddLLMServices();
            
            // 注册LLM审计服务
            services.AddLLMAuditServices(configuration);
            
            Console.WriteLine("Pathfinder LLM服务及审计配置完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册LLM服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加AI表单填充服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddAiFormFillServices(IServiceCollection services)
    {
        try
        {
            // 注册AI表单填充服务（自动端点模式）
            services.AddAiFormFillEndpoints();
            
            Console.WriteLine("Pathfinder AI表单填充服务配置完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册AI表单填充服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 注册Pathfinder业务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddPathfinderServices(IServiceCollection services)
    {
        // 注册Goal服务
        services.AddScoped<IGoalService, GoalService>();
        
        // 注册Task服务
        services.AddScoped<ITaskService, TaskService>();
    }
    
    /// <summary>
    /// 配置控制器和审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void ConfigureControllersWithAudit(IServiceCollection services, IConfiguration configuration)
    {
        // 添加审计元数据过滤器到控制器
        services.AddControllers().AddAuditMetadataFilter();
    }
    
    /// <summary>
    /// 配置Pathfinder特定中间件
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
    /// Pathfinder数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 使用标准数据库初始化方法
        await this.InitializeStandardDatabaseAsync<PathfinderDbContext, MySqlPathfinderDbContext, SqlServerPathfinderDbContext>(
            app, "PathfinderApi");
    }
    
    /// <summary>
    /// 添加Pathfinder缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddCachingServices(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // 注册缓存服务（暂时留空，后续扩展）
            Console.WriteLine("Pathfinder缓存服务配置完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册Pathfinder缓存服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加定时任务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private void AddScheduledTasksServices(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // 注册定时任务组件
            services.AddCodeSpiritScheduledTasks(configuration, ServiceName);
            
            Console.WriteLine("Pathfinder定时任务服务配置完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册定时任务服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
}

