using CodeSpirit.ConfigCenter.Client.Cache;
using CodeSpirit.ConfigCenter.Client.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置中心配置源
/// </summary>
public class ConfigCenterConfigurationSource : IConfigurationSource
{
    private readonly ConfigCenterClient _client;
    private readonly ConfigCenterHubClient _hubClient;
    private readonly ConfigCacheService _cacheService;
    private readonly IOptions<ConfigCenterClientOptions> _options;
    private readonly ILogger<ConfigCenterConfigurationProvider> _logger;

    public ConfigCenterConfigurationSource(
        ConfigCenterClient client,
        ConfigCenterHubClient hubClient,
        ConfigCacheService cacheService,
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCenterConfigurationProvider> logger)
    {
        _client = client;
        _hubClient = hubClient;
        _cacheService = cacheService;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 创建配置提供程序
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ConfigCenterConfigurationProvider(_client, _hubClient, _cacheService, _options, _logger);
    }
} 