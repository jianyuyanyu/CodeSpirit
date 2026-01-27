using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Startup;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.EventHandlers;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace CodeSpirit.IdentityApi.Configuration;

/// <summary>
/// 身份认证API服务配置
/// </summary>
public class IdentityApiConfiguration : AuditAwareApiConfiguration
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
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
        
        // 配置标准数据库服务（多数据库支持、仓储模式）
        this.ConfigureStandardDatabaseServices<ApplicationDbContext, MySqlDbContext, SqlServerDbContext>(
            services, configuration);
        
        // 配置标准基础设施服务（事件总线、HTTP客户端）+ 可选组件（多租户、设置管理）
        this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
        {
            s.AddCodeSpiritMultiTenant(c);
            s.AddSettingsManagerWithDatabase(c);
        });
        
        // 添加自定义业务服务
        AddCustomServices(services);
        
        // 添加LLM服务
        AddLLMServices(services);
        
        // 添加Identity服务
        AddIdentityServices(services, configuration);
        
        // 配置自定义控制器（审计元数据过滤器已由 BaseApiConfiguration 自动配置）
        ConfigureCustomControllers(services);
        
        // 注册Charts服务
        RegisterChartServices(services);
        
        // 注册事件处理器
        services.AddTenantAwareEventHandler<UserCreatedOrUpdatedEvent, UserCreatedOrUpdatedEventHandler>();
        services.AddTenantAwareEventHandler<UserDeletedEvent, UserDeletedEventHandler>();

        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();

        // 添加Redis分布式锁服务
        AddRedisDistributedLock(services);
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
        // 审计中间件由网关层统一处理，API服务不需要使用
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 配置身份认证特定中间件（在通用中间件之后）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 配置标准中间件（多租户、聚合器、AI表单填充）
        // 注意：Identity API 的多租户中间件在 ConfigurePreAuthenticationMiddlewareAsync 中配置
        app.UseCodeSpiritAggregator();
        app.UseAiFormFillEndpoints();

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
        var configuration = services.GetRequiredService<IConfiguration>();
        
        try
        {
            // 首先应用数据库迁移
            await ApplyDatabaseMigrationsAsync(services, configuration, logger);
            
            // 初始化设置数据库
            await app.UseSettingsManagerAsync();
            
            // 然后执行数据初始化
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
    /// 应用数据库迁移
    /// </summary>
    /// <param name="services">服务提供者</param>
    /// <param name="configuration">配置</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>异步任务</returns>
    private static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        // 使用统一的数据库迁移方法
        await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlDbContext, SqlServerDbContext>(
            services, configuration, logger, "IdentityApi");
    }
    
    /// <summary>
    /// 添加自定义业务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddCustomServices(IServiceCollection services)
    {
        // 注意：IRepository<> 已在 ConfigureStandardDatabaseServices 中注册，无需重复注册
        
        // 注册自定义授权处理程序（这个需要特殊处理，因为是 Identity 框架的组件）
        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();

        // 注册第三方API服务
        services.AddScoped<Services.ThirdParty.WeChatApiService>();
        services.AddScoped<Services.ThirdParty.IThirdPartyApiService, Services.ThirdParty.ThirdPartyApiServiceFactory>();
    }
    
    /// <summary>
    /// 添加LLM服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddLLMServices(IServiceCollection services)
    {
        // 添加LLM服务（使用统一配置）
        services.AddLLMServices();
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
        var identityBuilder = services.AddIdentityCore<ApplicationUser>(options =>
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
            // 禁用默认的用户名唯一性要求，我们将在自定义验证器中处理
            options.User.RequireUniqueEmail = false;
        });
        
        // 添加角色支持
        identityBuilder.AddRoles<ApplicationRole>();
        
        // 添加 Entity Framework 存储
        identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
        
        // 添加默认令牌提供程序
        identityBuilder.AddDefaultTokenProviders();
        
        // 添加登录管理器
        identityBuilder.AddSignInManager<SignInManager<ApplicationUser>>();
        
        // 完全替换默认的用户验证器和角色验证器
        // 注意：必须在 AddEntityFrameworkStores 之后执行，因为 EF Store 会注册默认验证器
        services.Replace(ServiceDescriptor.Scoped<IUserValidator<ApplicationUser>, TenantAwareUserValidator>());
        services.Replace(ServiceDescriptor.Scoped<IRoleValidator<ApplicationRole>, TenantAwareRoleValidator>());
    }
    

    
    /// <summary>
    /// 配置自定义控制器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <remarks>
    /// 审计元数据过滤器已由 BaseApiConfiguration 根据配置自动添加，无需手动配置
    /// </remarks>
    private static void ConfigureCustomControllers(IServiceCollection services)
    {
        services.ConfigureDefaultControllers((options) =>
        {
            
        });
        
        // 注意：审计元数据过滤器已由 BaseApiConfiguration.ConfigureAuditMetadataFilter 自动配置
        // 如果需要在配置中禁用，设置 Audit:EnableMetadataFilter = false
        services.AddControllers();
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

    /// <summary>
    /// 添加Redis分布式锁服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddRedisDistributedLock(IServiceCollection services)
    {
        services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:Identity:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
    }
}
