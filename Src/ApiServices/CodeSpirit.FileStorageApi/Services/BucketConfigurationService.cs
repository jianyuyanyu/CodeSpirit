using Microsoft.Extensions.Options;
using CodeSpirit.FileStorageApi.Options;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 存储桶配置服务实现
/// </summary>
public class BucketConfigurationService : IBucketConfigurationService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<BucketConfigurationService> _logger;

    public BucketConfigurationService(
        IOptions<FileStorageOptions> options,
        ILogger<BucketConfigurationService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取所有存储桶配置
    /// </summary>
    public IEnumerable<(string Name, StorageBucketOptions Options)> GetAllBuckets()
    {
        return _options.Buckets?.Select(kvp => (kvp.Key, kvp.Value)) ?? 
               Enumerable.Empty<(string, StorageBucketOptions)>();
    }

    /// <summary>
    /// 根据名称获取存储桶配置
    /// </summary>
    public StorageBucketOptions? GetBucketByName(string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName))
            return null;

        return _options.Buckets?.TryGetValue(bucketName, out var bucket) == true ? bucket : null;
    }

    /// <summary>
    /// 获取默认存储桶配置
    /// </summary>
    public (string Name, StorageBucketOptions Options) GetDefaultBucket()
    {
        // 尝试获取名为 "default" 的存储桶
        if (_options.Buckets?.TryGetValue("default", out var defaultBucket) == true)
        {
            return ("default", defaultBucket);
        }

        // 如果没有默认存储桶，返回第一个可用的
        var firstBucket = _options.Buckets?.FirstOrDefault();
        if (firstBucket.HasValue)
        {
            return (firstBucket.Value.Key, firstBucket.Value.Value);
        }

        throw new InvalidOperationException("未找到可用的存储桶配置");
    }

    /// <summary>
    /// 获取指定租户的可用存储桶
    /// </summary>
    public IEnumerable<(string Name, StorageBucketOptions Options)> GetAvailableBuckets(string tenantId)
    {
        // 目前返回所有启用的存储桶，后续可以根据租户权限进行过滤
        return GetAllBuckets().Where(bucket => bucket.Options.IsEnabled);
    }

    /// <summary>
    /// 获取存储桶统计信息
    /// </summary>
    public Task<BucketStatistics> GetBucketStatisticsAsync(string bucketName)
    {
        // TODO: 实现从缓存或数据库获取统计信息
        var bucket = GetBucketByName(bucketName);
        if (bucket == null)
        {
            throw new ArgumentException($"存储桶 '{bucketName}' 不存在", nameof(bucketName));
        }

        var statistics = new BucketStatistics
        {
            BucketName = bucketName,
            FileCount = 0,
            StorageSize = 0,
            StorageQuota = bucket.StorageQuota,
            LastUpdateTime = DateTime.UtcNow
        };

        return Task.FromResult(statistics);
    }

    /// <summary>
    /// 更新存储桶统计信息
    /// </summary>
    public Task UpdateBucketStatisticsAsync(string bucketName, long fileCountDelta, long sizeCountDelta)
    {
        // TODO: 实现统计信息更新逻辑
        _logger.LogDebug("更新存储桶 {BucketName} 统计信息: 文件数变化 {FileCountDelta}, 大小变化 {SizeCountDelta}",
            bucketName, fileCountDelta, sizeCountDelta);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 验证存储桶配置
    /// </summary>
    public Task<bool> ValidateBucketConfigurationAsync(string bucketName)
    {
        var bucket = GetBucketByName(bucketName);
        return Task.FromResult(bucket != null && bucket.IsEnabled);
    }

    /// <summary>
    /// 检查存储桶配额
    /// </summary>
    public async Task<bool> CheckBucketQuotaAsync(string bucketName, long fileSize)
    {
        var bucket = GetBucketByName(bucketName);
        if (bucket == null)
            return false;

        // 检查单文件大小限制
        if (bucket.MaxFileSize.HasValue && fileSize > bucket.MaxFileSize.Value)
        {
            return false;
        }

        // 检查存储配额
        if (bucket.StorageQuota.HasValue)
        {
            var statistics = await GetBucketStatisticsAsync(bucketName);
            if (statistics.StorageSize + fileSize > bucket.StorageQuota.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 验证文件类型
    /// </summary>
    public bool ValidateFileType(string bucketName, string? contentType, string? fileName)
    {
        var bucket = GetBucketByName(bucketName);
        if (bucket == null)
            return false;

        return Extensions.FileTypeCategoryHelper.IsFileTypeAllowed(
            contentType, fileName, bucket.AllowedFileTypes, bucket.ForbiddenFileTypes);
    }

    /// <summary>
    /// 获取存储提供程序
    /// </summary>
    public IStorageProvider GetStorageProvider(string bucketName)
    {
        // TODO: 实现根据存储桶获取对应的存储提供程序
        throw new NotImplementedException("GetStorageProvider 方法尚未实现");
    }
}
