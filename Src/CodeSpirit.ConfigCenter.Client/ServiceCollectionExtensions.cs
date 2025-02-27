using System.Net.Http.Headers;
using CodeSpirit.ConfigCenter.Client.Cache;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加配置中心客户端
    /// </summary>
    public static IServiceCollection AddConfigCenterClient(
        this IServiceCollection services,
        Action<ConfigCenterClientOptions> configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions!=null)
        {
            // 配置选项
            services.Configure(configureOptions);
        }

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

    /// <summary>
    /// 配置配置中心客户端
    /// </summary>
    public static IApplicationBuilder UseConfigCenterClient(this IApplicationBuilder app)
    {
        // 从服务容器中获取Hub客户端并启动连接
        var hubClient = app.ApplicationServices.GetService<SignalR.ConfigCenterHubClient>();
        if (hubClient != null)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<ConfigCenterClientOptions>>().Value;
            if (options.UseSignalR)
            {
                // 在后台连接到Hub
                var _ = hubClient.ConnectAsync();
            }
        }

        return app;
    }

    /// <summary>
    /// 添加配置中心配置
    /// </summary>
    public static IHostBuilder ConfigureConfigCenterConfiguration(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, ConfigCenterClientOptions> configureOptions = null)
    {
        return hostBuilder.ConfigureAppConfiguration((context, builder) =>
        {
            var options = new ConfigCenterClientOptions();

            configureOptions?.Invoke(context, options);

            builder.AddConfigCenter(opt =>
            {
                opt.ServiceUrl = options.ServiceUrl;
                opt.AppId = options.AppId;
                opt.AppSecret = options.AppSecret;
                opt.Environment = options.Environment;
                opt.AutoRegisterApp = options.AutoRegisterApp;
                opt.AppName = options.AppName;
                opt.PollIntervalSeconds = options.PollIntervalSeconds;
                opt.UseSignalR = options.UseSignalR;
                opt.RequestTimeoutSeconds = options.RequestTimeoutSeconds;
                opt.EnableLocalCache = options.EnableLocalCache;
                opt.LocalCacheDirectory = options.LocalCacheDirectory;
                opt.CacheExpirationMinutes = options.CacheExpirationMinutes;
                opt.PreferCache = options.PreferCache;
            });
        });
    }
}