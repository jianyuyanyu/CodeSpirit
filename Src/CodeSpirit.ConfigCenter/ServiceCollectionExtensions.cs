using CodeSpirit.Amis;
using CodeSpirit.ConfigCenter.Data;
using CodeSpirit.ConfigCenter.Hubs;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using Newtonsoft.Json;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddSingleton<IConfigCacheService, MemoryCacheConfigService>();
        services.AddSingleton<IConfigChangeNotifier, SignalRConfigChangeNotifier>();
        return services;
    }

    public static IServiceCollection AddConfigCenter(this WebApplicationBuilder builder)
    {
        string appName = "config-api";
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults(appName);

        // Add services to the container
        builder.Services.AddDatabase<ConfigDbContext>(builder.Configuration, appName);
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

    public static WebApplication ConfigureApp(this WebApplication app)
    {
        // 配置 SignalR 路由
        app.MapHub<ConfigChangeHub>("/configHub");

        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseAmis();
        return app;
    }
}