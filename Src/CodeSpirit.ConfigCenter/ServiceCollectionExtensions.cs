using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeSpirit.ConfigCenter.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigCenter(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加缓存
        services.AddMemoryCache();

        // 添加 SignalR
        services.AddSignalR();

        //// 注册服务
        //services.AddScoped<IConfigCacheService, ConfigCacheService>();
        //services.AddScoped<IConfigChangeNotifier, ConfigChangeNotifier>();

      
        return services;
    }
} 