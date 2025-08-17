using CodeSpirit.FileStorageApi.Options;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 存储桶配置服务接口
/// </summary>
public interface IBucketConfigurationService : IScopedDependency
{
    /// <summary>
    /// 获取所有存储桶配置
    /// </summary>
    IEnumerable<(string Name, StorageBucketOptions Options)> GetAllBuckets();
    
    /// <summary>
    /// 根据名称获取存储桶配置
    /// </summary>
    StorageBucketOptions? GetBucketByName(string bucketName);
    
    /// <summary>
    /// 根据名称或别名获取存储桶配置
    /// </summary>
    /// <param name="nameOrAlias">存储桶名称或别名</param>
    /// <returns>存储桶配置和对应的键名</returns>
    (string BucketName, StorageBucketOptions Options)? GetBucket(string nameOrAlias);
    
    /// <summary>
    /// 获取默认存储桶配置
    /// </summary>
    (string Name, StorageBucketOptions Options) GetDefaultBucket();
    
    /// <summary>
    /// 获取指定租户的可用存储桶
    /// </summary>
    IEnumerable<(string Name, StorageBucketOptions Options)> GetAvailableBuckets(string tenantId);
    
    /// <summary>
    /// 获取存储桶统计信息
    /// </summary>
    Task<BucketStatistics> GetBucketStatisticsAsync(string bucketName);
    
    /// <summary>
    /// 更新存储桶统计信息
    /// </summary>
    Task UpdateBucketStatisticsAsync(string bucketName, long fileCountDelta, long sizeCountDelta);
    
    /// <summary>
    /// 验证存储桶配置
    /// </summary>
    Task<bool> ValidateBucketConfigurationAsync(string bucketName);
    
    /// <summary>
    /// 检查存储桶配额
    /// </summary>
    Task<bool> CheckBucketQuotaAsync(string bucketName, long fileSize);
    
    /// <summary>
    /// 验证文件类型
    /// </summary>
    bool ValidateFileType(string bucketName, string contentType, string fileName);
    
    /// <summary>
    /// 获取存储提供程序
    /// </summary>
    IStorageProvider GetStorageProvider(string bucketName);
}

/// <summary>
/// 存储桶统计信息（缓存管理）
/// </summary>
public class BucketStatistics
{
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string BucketName { get; set; }
    
    /// <summary>
    /// 文件数量
    /// </summary>
    public long FileCount { get; set; }
    
    /// <summary>
    /// 存储大小（字节）
    /// </summary>
    public long StorageSize { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }
    
    /// <summary>
    /// 存储配额（字节，null表示无限制）
    /// </summary>
    public long? StorageQuota { get; set; }
    
    /// <summary>
    /// 配额使用率
    /// </summary>
    public double QuotaUsagePercentage => StorageQuota.HasValue && StorageQuota.Value > 0 
        ? (double)StorageSize / StorageQuota.Value * 100 
        : 0;
    
    /// <summary>
    /// 剩余存储空间
    /// </summary>
    public long? RemainingStorage => StorageQuota.HasValue 
        ? Math.Max(0, StorageQuota.Value - StorageSize) 
        : null;
}
