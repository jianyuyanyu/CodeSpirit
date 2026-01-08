using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Sdk.Configuration;

/// <summary>
/// 配置中心配置源
/// </summary>
public class ConfigCenterConfigurationSource : IConfigurationSource
{
    private readonly Func<IServiceProvider> _serviceProviderFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCenterConfigurationSource(Func<IServiceProvider> serviceProviderFactory)
    {
        _serviceProviderFactory = serviceProviderFactory;
    }

    /// <summary>
    /// 构建配置提供程序
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var serviceProvider = _serviceProviderFactory();
        return new ConfigCenterConfigurationProvider(
            serviceProvider.GetRequiredService<Cache.InMemoryConfigCache>(),
            serviceProvider.GetRequiredService<Cache.ConfigCacheService>(),
            serviceProvider.GetRequiredService<ConfigCenterClient>(),
            serviceProvider.GetRequiredService<Registration.AppRegistrationService>(),
            serviceProvider.GetRequiredService<IOptions<ConfigCenterOptions>>(),
            serviceProvider.GetRequiredService<ILogger<ConfigCenterConfigurationProvider>>());
    }
}

