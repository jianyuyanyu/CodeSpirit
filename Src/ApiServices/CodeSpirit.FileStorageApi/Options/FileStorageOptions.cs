using CodeSpirit.FileStorageApi.Abstractions;

namespace CodeSpirit.FileStorageApi.Options;

/// <summary>
/// 文件存储配置
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    
    /// <summary>
    /// 存储提供程序配置
    /// </summary>
    public Dictionary<string, StorageProviderOptions> StorageProviders { get; set; } = new();
    
    /// <summary>
    /// 存储桶配置
    /// </summary>
    public Dictionary<string, StorageBucketOptions> Buckets { get; set; } = new();
    
    /// <summary>
    /// 监控配置
    /// </summary>
    public MonitoringOptions Monitoring { get; set; } = new();
    
    /// <summary>
    /// 通过别名获取存储桶配置
    /// </summary>
    /// <param name="alias">存储桶别名</param>
    /// <returns>存储桶配置，如果未找到则返回null</returns>
    public StorageBucketOptions? GetBucketByAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return null;
            
        return Buckets.Values.FirstOrDefault(bucket => bucket.HasAlias(alias));
    }
    
    /// <summary>
    /// 通过名称或别名获取存储桶配置
    /// </summary>
    /// <param name="nameOrAlias">存储桶名称或别名</param>
    /// <returns>存储桶配置和对应的键名</returns>
    public (string Key, StorageBucketOptions Bucket)? GetBucket(string nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            return null;
            
        // 首先尝试通过键名获取
        if (Buckets.TryGetValue(nameOrAlias, out var bucketByKey))
        {
            return (nameOrAlias, bucketByKey);
        }
        
        // 然后尝试通过别名获取
        var bucketByAlias = Buckets.FirstOrDefault(kvp => kvp.Value.HasAlias(nameOrAlias));
            
        if (bucketByAlias.Key != null)
        {
            return (bucketByAlias.Key, bucketByAlias.Value);
        }
        
        return null;
    }
}

/// <summary>
/// 存储提供程序配置选项
/// </summary>
public class StorageProviderOptions
{
    /// <summary>
    /// 提供程序类型
    /// </summary>
    public StorageProviderType Type { get; set; }
    
    /// <summary>
    /// 配置属性（不同提供程序的配置参数）
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// 获取配置属性值
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
            catch
            {
                return defaultValue!;
            }
        }
        return defaultValue!;
    }
    
    /// <summary>
    /// 设置配置属性值
    /// </summary>
    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value!;
    }
}

/// <summary>
/// 存储桶配置选项
/// </summary>
public class StorageBucketOptions
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 别名，用于便捷引用存储桶（多个别名用逗号分割，如：avatar,logo）
    /// </summary>
    public string? Alias { get; set; }
    
    /// <summary>
    /// 获取所有别名列表
    /// </summary>
    /// <returns>别名列表</returns>
    public List<string> GetAliases()
    {
        if (string.IsNullOrWhiteSpace(Alias))
            return new List<string>();
            
        return Alias.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(alias => alias.Trim())
                   .Where(alias => !string.IsNullOrWhiteSpace(alias))
                   .ToList();
    }
    
    /// <summary>
    /// 检查是否包含指定别名
    /// </summary>
    /// <param name="alias">要检查的别名</param>
    /// <returns>是否包含该别名</returns>
    public bool HasAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return false;
            
        return GetAliases().Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 存储提供程序名称
    /// </summary>
    public string? Provider { get; set; }
    
    /// <summary>
    /// 访问策略
    /// </summary>
    public BucketAccessPolicy AccessPolicy { get; set; } = BucketAccessPolicy.Private;
    
    /// <summary>
    /// 存储配额（字节，null表示无限制）
    /// </summary>
    public long? StorageQuota { get; set; }
    
    /// <summary>
    /// 单文件大小限制（字节）
    /// </summary>
    public long? MaxFileSize { get; set; }
    
    /// <summary>
    /// 允许的文件类型
    /// </summary>
    public string? AllowedFileTypes { get; set; }
    
    /// <summary>
    /// 禁止的文件类型
    /// </summary>
    public string? ForbiddenFileTypes { get; set; }
    
    /// <summary>
    /// 文件保留天数
    /// </summary>
    public int? RetentionDays { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// 获取扩展属性值
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
            catch
            {
                return defaultValue!;
            }
        }
        return defaultValue!;
    }
    
    /// <summary>
    /// 设置扩展属性值
    /// </summary>
    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value!;
    }
}

/// <summary>
/// 监控配置选项
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// 是否启用指标监控
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
    
    /// <summary>
    /// 指标前缀
    /// </summary>
    public string MetricsPrefix { get; set; } = "filestorage";
    
    /// <summary>
    /// 是否启用详细指标
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;
    
    /// <summary>
    /// 采样率
    /// </summary>
    public double SampleRate { get; set; } = 1.0;
}
