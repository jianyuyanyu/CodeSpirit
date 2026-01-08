using CodeSpirit.ConfigCenter.Sdk.Cache;
using CodeSpirit.ConfigCenter.Sdk.Models;
using CodeSpirit.ConfigCenter.Sdk.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ConfigCenterOptions _options;
    private readonly ILogger<ConfigCenterConfigurationProvider> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCenterConfigurationProvider(
        InMemoryConfigCache memoryCache,
        ConfigCacheService redisCacheService,
        ConfigCenterClient client,
        AppRegistrationService registrationService,
        IOptions<ConfigCenterOptions> options,
        ILogger<ConfigCenterConfigurationProvider> logger)
    {
        _memoryCache = memoryCache;
        _redisCacheService = redisCacheService;
        _client = client;
        _registrationService = registrationService;
        _options = options.Value;
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

        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation("==== 配置加载到 IConfiguration ====");
            _logger.LogInformation("配置版本: {Version}", configs.Version);
            _logger.LogInformation("原始配置项数量: {Count}", configs.Configs.Count);
        }

        int loadedCount = 0;
        int skippedCount = 0;
        var loadedKeys = new List<string>();

        foreach (var kvp in configs.Configs)
        {
            var key = kvp.Key;
            var value = kvp.Value?.ToString() ?? string.Empty;

            // 支持嵌套配置（如果值是 JSON）
            if (kvp.Value is Newtonsoft.Json.Linq.JObject jsonObject)
            {
                var beforeCount = Data.Count;
                FlattenJsonObject(jsonObject, key, Data);
                var addedCount = Data.Count - beforeCount;
                loadedCount += addedCount;
                
                if (_options.EnableDetailedLogging && addedCount > 0)
                {
                    _logger.LogInformation("  [JSON] {Key} 展开为 {Count} 个子配置项", key, addedCount);
                }
            }
            else
            {
                // ⚠️ 保护 Aspire 服务发现的配置不被覆盖
                if (!IsProtectedConfig(key))
                {
                    Data[key] = value;
                    loadedCount++;
                    
                    if (_options.EnableDetailedLogging)
                    {
                        loadedKeys.Add(key);
                    }
                }
                else
                {
                    skippedCount++;
                    _logger.LogDebug("跳过受保护的配置（由 Aspire 服务发现提供）: {Key}", key);
                }
            }
        }

        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation("加载成功: {LoadedCount} 个配置项", loadedCount);
            _logger.LogInformation("跳过保护: {SkippedCount} 个配置项", skippedCount);
            
            if (loadedKeys.Any())
            {
                _logger.LogInformation("已加载的配置键:");
                foreach (var key in loadedKeys.OrderBy(k => k))
                {
                    var valueStr = Data.TryGetValue(key, out var val) ? val : "(null)";
                    if (valueStr != null && valueStr.Length > 100)
                    {
                        valueStr = valueStr.Substring(0, 100) + "... (已截断)";
                    }
                    _logger.LogInformation("  {Key} = {Value}", key, valueStr);
                }
            }
            
            _logger.LogInformation("==== 配置加载完成 ====");
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
    private void FlattenJsonObject(Newtonsoft.Json.Linq.JObject jsonObject, string prefix, IDictionary<string, string?> data)
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

