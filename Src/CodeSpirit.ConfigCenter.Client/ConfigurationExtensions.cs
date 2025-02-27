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
        Action<ConfigCenterClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // 创建一个专用的 ServiceCollection 来托管配置中心客户端服务
        var services = new ServiceCollection();
        
        // 添加核心服务
        services.AddLogging();
        services.AddOptions();
        services.Configure(configureOptions);
        
        // 注册客户端服务（使用扩展方法简化）
        services.AddConfigCenterClient();
        
        // 构建服务提供程序
        var serviceProvider = services.BuildServiceProvider();
        
        // 创建配置源并添加到配置构建器
        var source = ActivatorUtilities.CreateInstance<ConfigCenterConfigurationSource>(serviceProvider);
        return builder.Add(source);
    }

    /// <summary>
    /// 注册配置中心客户端服务
    /// </summary>
    private static IServiceCollection AddConfigCenterClient(this IServiceCollection services)
    {
        // 注册HttpClient
        //services.AddHttpClient<ConfigCenterClient>();

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