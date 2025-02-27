using CodeSpirit.ConfigCenter.Client.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置扩展方法
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 添加配置中心配置源
    /// </summary>
    public static IConfigurationBuilder AddConfigCenter(
        this IConfigurationBuilder builder,
        Action<ConfigCenterClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        
        // 配置选项
        services.Configure(configureOptions);
        
        // 注册HttpClient
        services.AddHttpClient<ConfigCenterClient>();
        
        // 注册SignalR客户端
        services.AddSingleton<SignalR.ConfigCenterHubClient>();
        
        // 注册缓存服务
        services.AddSingleton<ConfigCacheService>();
        
        // 构建服务提供程序
        var serviceProvider = services.BuildServiceProvider();
        
        // 创建配置源并添加到配置构建器
        var client = serviceProvider.GetRequiredService<ConfigCenterClient>();
        var hubClient = serviceProvider.GetRequiredService<SignalR.ConfigCenterHubClient>();
        var cacheService = serviceProvider.GetRequiredService<ConfigCacheService>();
        var options = serviceProvider.GetRequiredService<IOptions<ConfigCenterClientOptions>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ConfigCenterConfigurationProvider>>();
        
        var source = new ConfigCenterConfigurationSource(client, hubClient, cacheService, options, logger);
        return builder.Add(source);
    }
} 