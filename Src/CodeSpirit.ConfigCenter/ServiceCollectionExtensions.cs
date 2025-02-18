using CodeSpirit.Amis;
using CodeSpirit.ConfigCenter.Data;
using CodeSpirit.ConfigCenter.Hubs;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ConfigDbContext>());
        services.AddScoped<IAppService, AppService>();
        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 注册核心服务
        services.AddSingleton<IConfigCacheService, MemoryCacheConfigService>();
        services.AddSingleton<IConfigChangeNotifier, SignalRConfigChangeNotifier>();
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("config-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ConfigDbContext>(options =>
        {
            options.UseSqlServer(connectionString);

            // 仅在开发环境下启用敏感数据日志和控制台日志
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging()
                       .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            }
        });

        return services;
    }

    public static IServiceCollection AddConfigCenter(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults();

        // Add services to the container
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program));
        builder.Services.AddCustomServices();
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
        app.MapHub<ConfigChangeHub>("/configHub");

        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseAmis();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            ConfigDbContext dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
            // 调用数据初始化方法
            await dbContext.Database.MigrateAsync();
        }

        return app;
    }
}