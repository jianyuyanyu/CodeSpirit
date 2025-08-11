using CodeSpirit.FileStorageApi.Entities;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 文件存储性能监控接口
/// </summary>
public interface IFileStorageMetrics
{
    /// <summary>
    /// 记录文件上传指标
    /// </summary>
    void RecordFileUpload(TimeSpan duration, long fileSize, string bucketName, 
                         FileTypeCategory category, StorageProviderType providerType);
    
    /// <summary>
    /// 记录文件下载指标
    /// </summary>
    void RecordFileDownload(TimeSpan duration, long fileSize, string bucketName, 
                           FileTypeCategory category, bool fromCdn);
    
    /// <summary>
    /// 记录文件删除指标
    /// </summary>
    void RecordFileDelete(string bucketName, FileTypeCategory category, bool success);
    
    /// <summary>
    /// 记录存储使用量
    /// </summary>
    void RecordStorageUsage(string bucketName, long totalSize, long fileCount);
    
    /// <summary>
    /// 记录错误数
    /// </summary>
    void RecordError(string operation, string errorType, string? bucketName = null);
    
    /// <summary>
    /// 记录并发请求数
    /// </summary>
    void RecordConcurrency(string operation, int currentConcurrency);
    
    /// <summary>
    /// 记录缩略图生成指标
    /// </summary>
    void RecordThumbnailGeneration(TimeSpan duration, long originalSize, long thumbnailSize, string sizeKey);
    
    /// <summary>
    /// 记录CDN操作指标
    /// </summary>
    void RecordCdnOperation(string operation, TimeSpan duration, int urlCount, bool success);
    
    /// <summary>
    /// 记录文件引用操作
    /// </summary>
    void RecordFileReference(string operation, string sourceService, bool success);
    
    /// <summary>
    /// 记录文件生命周期操作
    /// </summary>
    void RecordLifecycleOperation(string operation, int fileCount, bool success);
}
