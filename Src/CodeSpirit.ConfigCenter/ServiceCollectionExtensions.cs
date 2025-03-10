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
using CodeSpirit.Aggregator;
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
        
        // 使用共享项目中的JWT认证扩展方法
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

        app.UseCodeSpiritAggregator();

        // 初始化数据库
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ConfigDbContext>();
                await context.Database.MigrateAsync();
                // 初始化种子数据
                await services.GetRequiredService<ConfigSeederService>().SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "应用数据库迁移时发生错误。");
            }
        }

        return app;
    }
}