using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.EventHandlers;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
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
        // 使用传统方式配置数据库，因为统一启动框架暂时不支持直接使用Aspire扩展方法
        // TODO: 后续需要修改统一启动框架以支持Aspire数据库集成
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? "MySql";
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
        Console.WriteLine($"Identity API 使用数据库类型: {databaseType}");

        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            // 注册MySQL特定的DbContext用于迁移
            services.AddDbContext<MySqlDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });
            
            // 注册ApplicationDbContext，使用MySQL配置
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });
            
            Console.WriteLine("已配置MySql数据库");
        }
        else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // 注册SQL Server特定的DbContext用于迁移
            services.AddDbContext<SqlServerDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            
            // 注册ApplicationDbContext，使用SQL Server配置
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            
            Console.WriteLine("已配置SqlServer数据库");
        }
        else
        {
            throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
        }
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        
        // 添加自定义业务服务
        AddCustomServices(services);
        
        // 添加LLM服务
        AddLLMServices(services);
        
        // 添加Identity服务
        AddIdentityServices(services, configuration);
        
        // 配置自定义控制器和审计
        ConfigureCustomControllers(services);
                
        // 注册多租户服务
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 注册Charts服务
        RegisterChartServices(services);
        
        
        // 审计元数据过滤器将通过AddAuditMetadataFilter自动注册
        
        // 注册事件总线
        services.AddEventBus();
        
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
        // 使用聚合器（在导航之后）
        app.UseCodeSpiritAggregator();

        // 使用AI表单填充自动端点
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
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? "MySql";
        logger.LogInformation("开始应用 {DatabaseType} 数据库迁移...", databaseType);
        
        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            var mySqlContext = services.GetRequiredService<MySqlDbContext>();
            await mySqlContext.Database.MigrateAsync();
            logger.LogInformation("MySQL 数据库迁移应用完成");
        }
        else if (databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var sqlServerContext = services.GetRequiredService<SqlServerDbContext>();
            await sqlServerContext.Database.MigrateAsync();
            logger.LogInformation("SQL Server 数据库迁移应用完成");
        }
        else
        {
            throw new InvalidOperationException($"不支持的数据库类型: {databaseType}");
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
    /// 配置自定义控制器和审计
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ConfigureCustomControllers(IServiceCollection services)
    {
        // 审计配置已由CodeSpirit.Audit组件统一处理

        services.ConfigureDefaultControllers((options) =>
        {
            
        });
        
        // 添加审计元数据过滤器，用于分布式环境中传递审计信息
        // 注意：这里不能链式调用，因为ConfigureDefaultControllers返回的是IServiceCollection
        // 需要使用AddControllers()来获取IMvcBuilder
        services.AddControllers().AddAuditMetadataFilter();
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
