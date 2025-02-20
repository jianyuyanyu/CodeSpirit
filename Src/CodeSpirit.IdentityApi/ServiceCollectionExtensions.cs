using Audit.Core;
using Audit.WebApi;
using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Audit;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<SeederService>();
        services.AddScoped<UserSeeder>();
        services.AddScoped<RoleSeeder>();
        services.AddScoped<ILoginLogService, LoginLogService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // 注册自定义授权处理程序（这个需要特殊处理，因为是 Identity 框架的组件）
        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // 密码设置
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // 锁定设置
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // 用户设置
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection ConfigureCustomControllers(this IServiceCollection services)
    {
        //TODO:抽取独立的审计模块
        // 配置审计
        Audit.Core.Configuration.Setup()
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

        return services;
    }

    /// <summary>
    /// 添加Jwt认证
    /// </summary>
    /// <param name="builder"></param>
    public static void AddJwtAuthentication(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.IncludeErrorDetails = true;
            //用于指定是否将从令牌提取的数据保存到当前的安全上下文中。当设置为true时，可以通过HttpContext.User.Claims来访问这些数据。
            //如果不需要使用从令牌提取的数据，可以将该属性设置为false以节省系统资源和提高性能。
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"])),
                ClockSkew = TimeSpan.Zero, // 设置时钟偏移量为0，即不允许过期的Token被接受
                RequireExpirationTime = true, // 要求Token必须有过期时间
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                NameClaimType = "id"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    ITokenBlacklistService blacklistService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenBlacklistService>();

                    // 获取原始令牌
                    string token = context.SecurityToken.ToString();

                    // 检查令牌是否在黑名单中
                    if (await blacklistService.IsBlacklistedAsync(token))
                    {
                        context.Fail("令牌已被禁用！");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            new ApiResponse(401, "令牌已被禁用，请重新登录！"));
                        return;
                    }

                    return;
                }
            };
        });
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("identity-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }

    public static WebApplicationBuilder AddIdentityApiServices(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults();

        // Add services to the container
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddCustomServices();
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        builder.Services.AddIdentityServices();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.ConfigureCustomControllers();

        // 配置审计
        builder.Services.Configure<AuditConfig>(
            builder.Configuration.GetSection("Audit"));

        return builder;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        // 执行数据初始化
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            ILogger<SeederService> logger = services.GetRequiredService<ILogger<SeederService>>();
            try
            {
                // 调用数据初始化方法
                await DataSeeder.SeedAsync(services);
            }
            catch (Exception ex)
            {
                // 在控制台输出错误
                logger.LogError(ex, $"数据初始化失败：{ex.Message}");
                throw;
            }
        }
    }

    public static async Task<WebApplication> ConfigureAppAsync(this WebApplication app)
    {
        await app.InitializeDatabaseAsync();

        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuditLogging();
        app.MapControllers();
        app.UseAmis();
        app.UseCodeSpiritAuthorization();
        return app;
    }
}