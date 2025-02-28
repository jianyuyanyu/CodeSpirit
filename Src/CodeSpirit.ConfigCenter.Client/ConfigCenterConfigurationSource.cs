using CodeSpirit.ConfigCenter.Client.Cache;
using CodeSpirit.ConfigCenter.Client.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置中心配置源
/// </summary>
public class ConfigCenterConfigurationSource : IConfigurationSource, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _ownsServiceProvider;

    public ConfigCenterConfigurationSource(IServiceProvider serviceProvider, bool ownsServiceProvider)
    {
        _serviceProvider = serviceProvider;
        _ownsServiceProvider = ownsServiceProvider;
    }

    /// <summary>
    /// 创建配置提供程序
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var client = _serviceProvider.GetRequiredService<ConfigCenterClient>();
        var hubClient = _serviceProvider.GetRequiredService<SignalR.ConfigCenterHubClient>();
        var cacheService = _serviceProvider.GetRequiredService<ConfigCacheService>();
        var options = _serviceProvider.GetRequiredService<IOptions<ConfigCenterClientOptions>>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ConfigCenterConfigurationProvider>>();
        
        return new ConfigCenterConfigurationProvider(client, hubClient, cacheService, options, logger, _serviceProvider);
    }

    public void Dispose()
    {
        if (_ownsServiceProvider && _serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
} 