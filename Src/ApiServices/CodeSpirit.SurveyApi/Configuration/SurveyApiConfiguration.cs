using CodeSpirit.Aggregator;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.SurveyApi.Data;
using CodeSpirit.SurveyApi.Extensions;

using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.SurveyApi.Services.Implementations;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodeSpirit.SurveyApi.Configuration;

/// <summary>
/// 问卷系统API服务配置
/// </summary>
public class SurveyApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "survey";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "survey-api";
    
    /// <summary>
    /// 配置问卷系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置问卷系统数据库
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        Console.WriteLine($"Connection string: {connectionString}");
        
        services.AddDbContext<SurveyDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<SurveyDbContext>());
        
        // 注册仓储模式
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加Redis分布式锁服务
        AddRedisDistributedLock(services);
        
        // 注册事件总线
        services.AddEventBus();
        
        // 注册Charts服务
        AddChartServices(services);
        
        // 注册LLM和SignalR服务
        AddLLMAndSignalRServices(services);
        
        // 添加设置管理
        services.AddSettingsManagerWithDatabase(configuration);
        
        // 注册问卷系统特定服务
        AddSurveyServices(services);
        
        // 添加HTTP客户端服务
        services.AddHttpClient();
    }
    
    /// <summary>
    /// 配置问卷系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 使用多租户中间件
        app.UseCodeSpiritMultiTenant();
        
        // 初始化设置管理
        await app.UseSettingsManagerAsync();
        
        // 使用聚合器
        app.UseCodeSpiritAggregator();
    }
    
    /// <summary>
    /// 问卷系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 初始化数据库
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<SurveyApiConfiguration>>();
        
        try
        {
            var context = services.GetRequiredService<SurveyDbContext>();
            // 使用迁移而不是EnsureCreated
            await context.Database.MigrateAsync();
            // 初始化数据
            await context.InitializeDatabaseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化问卷系统数据库时发生错误：{Message}", ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// 添加Redis分布式锁服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddRedisDistributedLock(IServiceCollection services)
    {
        services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:Survey:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
    }
    
    /// <summary>
    /// 添加图表服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddChartServices(IServiceCollection services)
    {
        // 注册Charts服务 - 即使Redis不可用，Chart服务也应该可以使用
        try
        {
            services.AddChartServices(options =>
            {
                options.EnableCache = true;
                options.CacheExpiration = 30;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册Charts服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加LLM和SignalR服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddLLMAndSignalRServices(IServiceCollection services)
    {
        // 使用扩展方法注册LLM问卷生成相关服务
        services.AddSurveyLLMServices();
        
        // 添加SignalR服务
        services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
    }
    
    /// <summary>
    /// 添加问卷系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddSurveyServices(IServiceCollection services)
    {
        // 注册问卷相关服务
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<ISurveyDraftService, SurveyDraftService>();
        services.AddScoped<ISurveySettingsService, SurveySettingsService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<ISurveyLLMGeneratorService, SurveyLLMGeneratorService>();
        
        // AutoMapper已在CommonApiServices中配置，会自动扫描当前程序集中的Profile
    }
}
