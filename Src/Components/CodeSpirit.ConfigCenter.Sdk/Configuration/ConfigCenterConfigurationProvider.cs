using CodeSpirit.ConfigCenter.Sdk.Cache;
using CodeSpirit.ConfigCenter.Sdk.Models;
using CodeSpirit.ConfigCenter.Sdk.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ConfigCenter.Sdk.Configuration;

/// <summary>
/// 配置中心配置提供程序
/// </summary>
public class ConfigCenterConfigurationProvider : ConfigurationProvider
{
    private readonly InMemoryConfigCache _memoryCache;
    private readonly ConfigCacheService _redisCacheService;
    private readonly ConfigCenterClient _client;
    private readonly AppRegistrationService _registrationService;
    private readonly ILogger<ConfigCenterConfigurationProvider> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCenterConfigurationProvider(
        InMemoryConfigCache memoryCache,
        ConfigCacheService redisCacheService,
        ConfigCenterClient client,
        AppRegistrationService registrationService,
        ILogger<ConfigCenterConfigurationProvider> logger)
    {
        _memoryCache = memoryCache;
        _redisCacheService = redisCacheService;
        _client = client;
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public override void Load()
    {
        try
        {
            var appId = _registrationService.GetCurrentAppId();
            
            // 1. 优先从内存缓存获取
            var memoryCached = _memoryCache.GetFromCache(appId);
            if (memoryCached != null && memoryCached.Configs != null)
            {
                _logger.LogInformation("从内存缓存加载配置: {AppId}", appId);
                LoadConfigsIntoDictionary(memoryCached);
                return;
            }

            // 2. 从 Redis 缓存获取（可选）
            var redisCached = _redisCacheService.GetFromCacheAsync(appId).GetAwaiter().GetResult();
            if (redisCached != null && redisCached.Configs != null)
            {
                _logger.LogInformation("从 Redis 缓存加载配置: {AppId}", appId);
                LoadConfigsIntoDictionary(redisCached);
                // 同时保存到内存缓存
                _memoryCache.SaveToCache(appId, redisCached);
                return;
            }

            // 3. 缓存未命中，从 API 获取
            _logger.LogInformation("缓存未命中，从 API 获取配置: {AppId}", appId);
            var configs = _client.GetConfigsAsync(appId).GetAwaiter().GetResult();
            
            // 保存到内存缓存和Redis缓存
            _memoryCache.SaveToCache(appId, configs);
            _redisCacheService.SaveToCacheAsync(appId, configs).GetAwaiter().GetResult();
            
            // 加载到配置字典
            LoadConfigsIntoDictionary(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置中心配置失败");
            // 失败时使用空配置，避免应用启动失败
        }
    }

    /// <summary>
    /// 应该由 Aspire 服务发现提供的配置前缀（不应被配置中心覆盖）
    /// </summary>
    private static readonly string[] ProtectedConfigPrefixes = new[]
    {
        "ConnectionStrings:",  // Aspire 服务发现提供的连接字符串
        "Services:",           // Aspire 服务发现配置
    };

    /// <summary>
    /// 将配置加载到字典中
    /// </summary>
    private void LoadConfigsIntoDictionary(ConfigItemsExportDto configs)
    {
        Data.Clear();

        if (configs?.Configs == null)
        {
            return;
        }

        foreach (var kvp in configs.Configs)
        {
            var key = kvp.Key;
            var value = kvp.Value?.ToString() ?? string.Empty;

            // 支持嵌套配置（如果值是 JSON）
            if (kvp.Value is Newtonsoft.Json.Linq.JObject jsonObject)
            {
                FlattenJsonObject(jsonObject, key, Data);
            }
            else
            {
                // ⚠️ 保护 Aspire 服务发现的配置不被覆盖
                if (!IsProtectedConfig(key))
                {
                    Data[key] = value;
                }
                else
                {
                    _logger.LogDebug("跳过受保护的配置（由 Aspire 服务发现提供）: {Key}", key);
                }
            }
        }

        OnReload();
    }

    /// <summary>
    /// 检查配置是否受保护（不应被配置中心覆盖）
    /// </summary>
    private static bool IsProtectedConfig(string key)
    {
        foreach (var prefix in ProtectedConfigPrefixes)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 展平 JSON 对象为配置键值对
    /// </summary>
    private void FlattenJsonObject(Newtonsoft.Json.Linq.JObject jsonObject, string prefix, IDictionary<string, string> data)
    {
        foreach (var property in jsonObject.Properties())
        {
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";

            if (property.Value is Newtonsoft.Json.Linq.JObject nestedObject)
            {
                FlattenJsonObject(nestedObject, key, data);
            }
            else
            {
                // ⚠️ 保护 Aspire 服务发现的配置不被覆盖
                if (!IsProtectedConfig(key))
                {
                    data[key] = property.Value?.ToString() ?? string.Empty;
                }
                else
                {
                    _logger.LogDebug("跳过受保护的配置（由 Aspire 服务发现提供）: {Key}", key);
                }
            }
        }
    }
}

