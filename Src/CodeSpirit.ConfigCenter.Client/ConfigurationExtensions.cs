using CodeSpirit.ConfigCenter.Client.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

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
        Action<ConfigCenterClientOptions> configureOptions,
        IServiceProvider? serviceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // 如果没有提供 ServiceProvider，创建一个临时的并确保在应用程序结束时释放资源
        var externalServiceProvider = serviceProvider != null;
        if (serviceProvider == null)
        {
            var services = new ServiceCollection();
            
            // 添加核心服务
            services.AddLogging();
            services.AddOptions();
            services.Configure(configureOptions);
            
            // 注册客户端服务
            services.AddConfigCenterClient();
            
            // 构建服务提供程序
            serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions 
            { 
                ValidateScopes = true,
                ValidateOnBuild = true 
            });
        }
        else
        {
            // 使用现有的服务提供程序，只配置选项
            var services = new ServiceCollection();
            services.Configure(configureOptions);
            var tempProvider = services.BuildServiceProvider();
            
            // 将选项配置复制到主服务提供程序
            var options = tempProvider.GetRequiredService<IOptions<ConfigCenterClientOptions>>().Value;
            var optionsCache = serviceProvider.GetService<IOptionsMonitor<ConfigCenterClientOptions>>();
            if (optionsCache == null)
            {
                // 如果主容器中没有注册客户端服务，则抛出异常
                throw new InvalidOperationException(
                    "配置中心客户端服务未在主应用程序中注册。请先调用 services.AddConfigCenterServices() 方法。");
            }
        }
        
        // 创建配置源并添加到配置构建器
        var source = new ConfigCenterConfigurationSource(serviceProvider, !externalServiceProvider);
        return builder.Add(source);
    }

    /// <summary>
    /// 在主应用程序中注册配置中心客户端服务
    /// </summary>
    public static IServiceCollection AddConfigCenterServices(
        this IServiceCollection services,
        Action<ConfigCenterClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        
        // 配置选项
        services.Configure(configureOptions);
        
        // 注册客户端服务
        services.AddConfigCenterClient();
        
        return services;
    }

    /// <summary>
    /// 注册配置中心客户端服务
    /// </summary>
    private static IServiceCollection AddConfigCenterClient(this IServiceCollection services)
    {
        // 注册HttpClient
        services.AddHttpClient<ConfigCenterClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ConfigCenterClientOptions>>().Value;

            client.BaseAddress = new Uri(options.ServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(options.AppSecret))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.AppSecret);
            }
        });


        // 注册SignalR客户端
        services.AddSingleton<SignalR.ConfigCenterHubClient>();
        
        // 注册缓存服务
        services.AddSingleton<ConfigCacheService>();
        
        return services;
    }
}