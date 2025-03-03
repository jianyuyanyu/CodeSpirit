using System.IO;
using System.Security.Cryptography;
using System.Text;
using CodeSpirit.ConfigCenter.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Client.Cache;

/// <summary>
/// 配置缓存服务
/// </summary>
public class ConfigCacheService
{
    private readonly ConfigCenterClientOptions _options;
    private readonly ILogger<ConfigCacheService> _logger;
    private readonly string _cacheFilePath;
    private readonly string _cacheMetadataFilePath;

    public ConfigCacheService(
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;

        string cacheDirectory;

        // 首先检查配置的绝对路径
        if (!string.IsNullOrEmpty(_options.LocalCacheDirectory) && Path.IsPathRooted(_options.LocalCacheDirectory))
        {
            cacheDirectory = _options.LocalCacheDirectory;
        }
        // 检查环境变量
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONFIG_CENTER_CACHE_DIR")))
        {
            cacheDirectory = Environment.GetEnvironmentVariable("CONFIG_CENTER_CACHE_DIR");
        }
        // 默认使用应用程序目录
        else
        {
            cacheDirectory = Path.Combine(AppContext.BaseDirectory, _options.LocalCacheDirectory ?? "");
        }

        _logger.LogDebug("将使用缓存目录: {CacheDirectory}", cacheDirectory);

        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        // 缓存文件路径
        var cacheFileName = $"config-{_options.AppId}-{_options.Environment}.json";
        _cacheFilePath = Path.Combine(cacheDirectory, cacheFileName);
        _cacheMetadataFilePath = Path.Combine(cacheDirectory, $"{cacheFileName}.metadata");

        _logger.LogDebug("配置缓存文件路径: {CacheFilePath}", _cacheFilePath);
    }

    /// <summary>
    /// 保存配置到缓存
    /// </summary>
    public async Task SaveToCacheAsync(ConfigItemsExportDto configData)
    {
        ArgumentNullException.ThrowIfNull(configData);

        try
        {
            var json = JsonConvert.SerializeObject(configData, Formatting.Indented);

            // 写入配置数据
            await File.WriteAllTextAsync(_cacheFilePath, json, Encoding.UTF8);

            // 写入元数据
            var metadata = new CacheMetadata
            {
                CachedAt = DateTime.UtcNow,
                ConfigDataHash = ComputeHash(json),
                AppId = _options.AppId,
                Environment = _options.Environment
            };

            await File.WriteAllTextAsync(
                _cacheMetadataFilePath,
                JsonConvert.SerializeObject(metadata, Formatting.Indented),
                Encoding.UTF8);

            _logger.LogInformation("已将配置保存到本地缓存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置到缓存时发生错误");
        }
    }

    /// <summary>
    /// 从缓存加载配置
    /// </summary>
    public async Task<ConfigItemsExportDto> LoadFromCacheAsync()
    {
        try
        {
            if (!File.Exists(_cacheFilePath) || !File.Exists(_cacheMetadataFilePath))
            {
                _logger.LogDebug("缓存文件不存在");
                return null;
            }

            // 读取元数据
            var metadataJson = await File.ReadAllTextAsync(_cacheMetadataFilePath, Encoding.UTF8);
            var metadata = JsonConvert.DeserializeObject<CacheMetadata>(metadataJson);

            // 检查缓存是否有效
            if (!IsValidCache(metadata))
            {
                return null;
            }

            // 验证缓存数据完整性
            var dataJson = await File.ReadAllTextAsync(_cacheFilePath, Encoding.UTF8);
            if (ComputeHash(dataJson) != metadata.ConfigDataHash)
            {
                _logger.LogWarning("缓存数据已被修改，哈希值不匹配");
                return null;
            }

            var configData = JsonConvert.DeserializeObject<ConfigItemsExportDto>(dataJson);
            return configData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存加载配置时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 检查缓存是否有效
    /// </summary>
    private bool IsValidCache(CacheMetadata metadata)
    {
        if (metadata == null ||
            string.IsNullOrEmpty(metadata.AppId) ||
            string.IsNullOrEmpty(metadata.Environment) ||
            string.IsNullOrEmpty(metadata.ConfigDataHash))
        {
            _logger.LogWarning("缓存元数据不完整");
            return false;
        }

        // 检查缓存是否过期
        var cacheAge = DateTime.UtcNow - metadata.CachedAt;
        if (cacheAge.TotalMinutes > _options.CacheExpirationMinutes)
        {
            _logger.LogDebug("缓存已过期，缓存时间: {CachedAt}, 当前时间: {CurrentTime}",
                metadata.CachedAt, DateTime.UtcNow);
            return false;
        }

        // 检查缓存的应用ID和环境是否与当前一致
        if (metadata.AppId != _options.AppId || metadata.Environment != _options.Environment)
        {
            _logger.LogWarning("缓存的应用ID或环境与当前不一致");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 计算数据的哈希值
    /// </summary>
    private string ComputeHash(string data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }

            if (File.Exists(_cacheMetadataFilePath))
            {
                File.Delete(_cacheMetadataFilePath);
            }

            _logger.LogInformation("已清除缓存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除缓存时发生错误");
        }
    }
}

/// <summary>
/// 缓存元数据
/// </summary>
internal class CacheMetadata
{
    /// <summary>
    /// 缓存时间
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// 配置数据的哈希值
    /// </summary>
    public string ConfigDataHash { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    public string Environment { get; set; }
}