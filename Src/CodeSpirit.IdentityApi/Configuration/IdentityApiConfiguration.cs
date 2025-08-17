using Audit.Core;
using Audit.WebApi;
using CodeSpirit.Aggregator;
using CodeSpirit.Amis;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Audit;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.EventHandlers;
using CodeSpirit.IdentityApi.Jwt;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace CodeSpirit.IdentityApi.Configuration;

/// <summary>
/// 身份认证API服务配置
/// </summary>
public class IdentityApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "identity";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "identity-api";
    
    /// <summary>
    /// 配置身份认证特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置身份认证数据库
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        
        // 添加自定义业务服务
        AddCustomServices(services);
        
        // 添加Identity服务
        AddIdentityServices(services, configuration);
        
        // 配置自定义控制器和审计
        ConfigureCustomControllers(services);
                
        // 注册多租户服务
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 注册Charts服务
        RegisterChartServices(services);
        
        // 配置审计
        services.Configure<AuditConfig>(configuration.GetSection("Audit"));
        
        // 注册事件总线
        services.AddEventBus();
        
        // 注册事件处理器
        services.AddTenantAwareEventHandler<UserCreatedOrUpdatedEvent, UserCreatedOrUpdatedEventHandler>();
        services.AddTenantAwareEventHandler<UserDeletedEvent, UserDeletedEventHandler>();
    }
    
    /// <summary>
    /// 配置在认证前的中间件（多租户中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
    {
        // 启用多租户中间件（在认证之前）
        app.UseCodeSpiritMultiTenant();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 配置在控制器映射前的中间件（审计日志中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
    {
        // 使用审计日志中间件（在控制器映射前）
        app.UseAuditLogging();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 配置身份认证特定中间件（在通用中间件之后）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 使用聚合器（在导航之后）
        app.UseCodeSpiritAggregator();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 身份认证数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 执行数据初始化
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<IdentityApiConfiguration>>();
        try
        {
            // 调用数据初始化方法
            await DataSeeder.SeedAsync(services);
        }
        catch (Exception ex)
        {
            // 在控制台输出错误
            logger.LogError(ex, "数据初始化失败：{Message}", ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// 添加自定义业务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddCustomServices(IServiceCollection services)
    {
        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 注册自定义授权处理程序（这个需要特殊处理，因为是 Identity 框架的组件）
        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();
    }
    
    /// <summary>
    /// 添加Identity服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddIdentityServices(IServiceCollection services, IConfiguration configuration)
    {
        // 获取密码和锁定相关设置
        bool requireDigit = true;
        bool requireLowercase = true;
        bool requireNonAlphanumeric = false;
        bool requireUppercase = true;
        int requiredLength = 6;
        int requiredUniqueChars = 1;
        int defaultLockoutMinutes = 5;
        int maxFailedAttempts = 5;

        // 尝试从配置中读取密码设置
        bool.TryParse(configuration["User:Password:RequireDigit"], out requireDigit);
        bool.TryParse(configuration["User:Password:RequireLowercase"], out requireLowercase);
        bool.TryParse(configuration["User:Password:RequireNonAlphanumeric"], out requireNonAlphanumeric);
        bool.TryParse(configuration["User:Password:RequireUppercase"], out requireUppercase);
        int.TryParse(configuration["User:Password:RequiredLength"], out requiredLength);

        // 尝试从配置中读取锁定设置
        int.TryParse(configuration["User:Lockout:DefaultLockoutMinutes"], out defaultLockoutMinutes);
        int.TryParse(configuration["User:Lockout:MaxFailedAttempts"], out maxFailedAttempts);

        // 使用 AddIdentityCore 替代 AddIdentity，避免覆盖JWT认证方案
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            // 密码设置
            options.Password.RequireDigit = requireDigit;
            options.Password.RequireLowercase = requireLowercase;
            options.Password.RequireNonAlphanumeric = requireNonAlphanumeric;
            options.Password.RequireUppercase = requireUppercase;
            options.Password.RequiredLength = requiredLength;
            options.Password.RequiredUniqueChars = requiredUniqueChars;

            // 锁定设置
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(defaultLockoutMinutes);
            options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
            options.Lockout.AllowedForNewUsers = true;

            // 用户设置
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager<SignInManager<ApplicationUser>>();
    }
    

    
    /// <summary>
    /// 配置自定义控制器和审计
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureCustomControllers(IServiceCollection services)
    {
        //TODO:抽取独立的审计模块
        // 配置审计
        global::Audit.Core.Configuration.Setup()
            .UseCustomProvider(new CustomAuditDataProvider(serviceProvider: services.BuildServiceProvider()))
            .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

        services.ConfigureDefaultControllers((options) =>
        {
            // 修改审计过滤器配置
            options.AddAuditFilter(config => config
                .LogAllActions()
                .WithEventType("{verb}.{controller}.{action}")
                .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                .IncludeRequestBody()
                .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode != 200)
                .IncludeModelState()
                .SerializeActionParameters()
            );
        });
    }
    
    /// <summary>
    /// 注册图表服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterChartServices(IServiceCollection services)
    {
        // 注册CodeSpirit.Charts服务
        services.AddChartServices(options =>
        {
            options.EnableCache = true;
            options.CacheExpiration = 30; // 修改为int类型的值，表示缓存过期时间（分钟）
        });
    }
}
