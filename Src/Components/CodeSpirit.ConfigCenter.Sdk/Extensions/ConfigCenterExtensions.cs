using CodeSpirit.ConfigCenter.Sdk.Cache;
using CodeSpirit.ConfigCenter.Sdk.Configuration;
using CodeSpirit.ConfigCenter.Sdk.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Sdk.Extensions;

/// <summary>
/// 配置中心扩展方法
/// </summary>
public static class ConfigCenterExtensions
{
    /// <summary>
    /// 添加配置中心服务（WebApplicationBuilder 版本）
    /// </summary>
    public static WebApplicationBuilder AddCodeSpiritConfigCenter(
        this WebApplicationBuilder builder,
        Action<ConfigCenterOptions>? configureOptions = null)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // 配置选项
        var optionsSection = configuration.GetSection("ConfigCenter");
        services.Configure<ConfigCenterOptions>(optionsSection);
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // 从 Aspire 服务发现获取配置中心地址
        // 优先使用 Aspire 服务发现的连接字符串（格式：http://config 或 https://config:port）
        var serviceUrl = configuration.GetConnectionString("config")  // Aspire 服务发现
            ?? configuration["ConfigCenter:ServiceUrl"]                // 手动配置
            ?? "http://config";                                         // 默认值
        
        Console.WriteLine($"[ConfigCenter SDK] 配置中心地址: {serviceUrl}");

        // 注册 HTTP 客户端（Transient）
        services.AddHttpClient<ConfigCenterClient>(client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // 为 AppRegistrationService 注册 HTTP 客户端
        services.AddHttpClient<AppRegistrationService>(client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // 为 SSE 监听器注册命名 HttpClient
        services.AddHttpClient("ConfigCenter", client =>
        {
            client.BaseAddress = new Uri(serviceUrl);
            client.Timeout = Timeout.InfiniteTimeSpan; // SSE 连接需要长时间保持
        });

        // ⚠️ 注意：AddHttpClient<T> 已经注册了 T，不需要再单独注册
        // ConfigCenterClient 和 AppRegistrationService 会被自动注册为 Transient

        // 注册内存缓存（必需）
        services.AddMemoryCache();
        services.AddSingleton<InMemoryConfigCache>();

        // 注册Redis缓存服务（可选）
        services.AddSingleton<ConfigCacheService>();

        // 注册SSE监听器（后台服务）
        services.AddHostedService<SseEventListener>();

        // 添加配置源（延迟构建，在配置源构建时再获取服务）
        var serviceProviderFactory = () => services.BuildServiceProvider();
        ((IConfigurationBuilder)builder.Configuration).Add(new ConfigCenterConfigurationSource(serviceProviderFactory));

        // 应用启动时自动注册
        services.AddHostedService<ConfigCenterStartupService>();

        return builder;
    }
}

/// <summary>
/// 配置中心启动服务
/// </summary>
public class ConfigCenterStartupService : IHostedService
{
    private readonly AppRegistrationService _registrationService;
    private readonly ILogger<ConfigCenterStartupService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCenterStartupService(
        AppRegistrationService registrationService,
        ILogger<ConfigCenterStartupService> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// 启动时执行
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _registrationService.RegisterAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置中心启动服务执行失败");
        }
    }

    /// <summary>
    /// 停止时执行
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

