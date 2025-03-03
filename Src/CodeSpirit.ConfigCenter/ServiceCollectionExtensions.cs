using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.ConfigCenter.Data;
using CodeSpirit.ConfigCenter.Data.Seeders;
using CodeSpirit.ConfigCenter.Hubs;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.ConfigCenter.Services.Implementations;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ConfigDbContext>());
        services.AddScoped<IAppService, AppService>();
        services.AddScoped<IConfigItemService, ConfigItemService>();

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 注册核心服务
        services.AddSingleton<IConfigCacheService, MemoryCacheConfigService>();
        services.AddSingleton<IConfigChangeNotifier, SignalRConfigChangeNotifier>();
        services.AddScoped<IConfigNotificationService, ConfigNotificationService>();
        services.AddScoped<ConfigSeederService>();
        services.AddScoped<IConfigPublishHistoryService, ConfigPublishHistoryService>();
        
        // 注册客户端跟踪服务
        services.AddSingleton<IClientTrackingService, ClientTrackingService>();
        
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("config-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ConfigDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }

    public static IServiceCollection AddConfigCenter(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults("config");

        // Add services to the container
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        builder.Services.AddCustomServices();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.ConfigureDefaultControllers();

        // 添加 SignalR
        builder.Services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

        return builder.Services;
    }

    public static async Task<WebApplication> ConfigureAppAsync(this WebApplication app)
    {
        // 配置 SignalR 路由
        app.MapHub<ConfigHub>("/config-hub");

        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseAmis();
        app.UseCodeSpiritAuthorization();
        await app.UseCodeSpiritNavigationAsync();

        // 初始化数据库
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            try
            {
                ConfigDbContext dbContext = services.GetRequiredService<ConfigDbContext>();
                ConfigSeederService seeder = services.GetRequiredService<ConfigSeederService>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                ILogger<ConfigSeederService> logger = services.GetRequiredService<ILogger<ConfigSeederService>>();
                logger.LogError(ex, "数据库初始化过程中发生错误");
                throw;
            }
        }

        return app;
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

            //options.Events = new JwtBearerEvents
            //{
            //    OnTokenValidated = async context =>
            //    {
            //        ITokenBlacklistService blacklistService = context.HttpContext.RequestServices
            //            .GetRequiredService<ITokenBlacklistService>();

            //        // 获取原始令牌
            //        string token = context.SecurityToken.ToString();

            //        // 检查令牌是否在黑名单中
            //        if (await blacklistService.IsBlacklistedAsync(token))
            //        {
            //            context.Fail("令牌已被禁用！");
            //            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            //            await context.Response.WriteAsJsonAsync(
            //                new ApiResponse(401, "令牌已被禁用，请重新登录！"));
            //            return;
            //        }

            //        return;
            //    }
            //};
        });
    }
}