using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 存储提供程序接口
/// 定义统一的文件存储操作抽象
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// 提供程序类型
    /// </summary>
    StorageProviderType ProviderType { get; }
    
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="stream">文件流</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="metadata">自定义元数据</param>
    /// <returns>存储结果</returns>
    Task<StorageResult> UploadFileAsync(string bucketName, string fileName, 
        Stream stream, string? contentType = null, 
        IDictionary<string, string>? metadata = null);
    
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>文件流</returns>
    Task<Stream> DownloadFileAsync(string bucketName, string fileName);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteFileAsync(string bucketName, string fileName);
    
    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>文件信息</returns>
    Task<FileInfo> GetFileInfoAsync(string bucketName, string fileName);
    
    /// <summary>
    /// 生成预签名URL
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="expirationTime">过期时间</param>
    /// <param name="operation">操作类型</param>
    /// <returns>预签名URL</returns>
    Task<string> GeneratePresignedUrlAsync(string bucketName, string fileName,
        TimeSpan expirationTime, PresignedUrlOperation operation = PresignedUrlOperation.Read);
    
    /// <summary>
    /// 创建存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="options">创建选项</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateBucketAsync(string bucketName, BucketCreationOptions? options = null);
    
    /// <summary>
    /// 删除存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteBucketAsync(string bucketName);
}

/// <summary>
/// 存储提供程序类型
/// </summary>
public enum StorageProviderType
{
    /// <summary>
    /// 本地存储
    /// </summary>
    [Display(Name = "本地存储")]
    Local = 1,
    
    /// <summary>
    /// 腾讯云COS
    /// </summary>
    [Display(Name = "腾讯云对象存储")]
    TencentCOS = 2,
    
    /// <summary>
    /// 阿里云OSS
    /// </summary>
    [Display(Name = "阿里云对象存储")]
    AlibabaOSS = 3
}

/// <summary>
/// 预签名URL操作类型
/// </summary>
public enum PresignedUrlOperation
{
    /// <summary>
    /// 读取操作
    /// </summary>
    Read = 1,
    
    /// <summary>
    /// 写入操作
    /// </summary>
    Write = 2
}

/// <summary>
/// 存储结果
/// </summary>
public class StorageResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 文件URL
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// 文件ETag
    /// </summary>
    public string? ETag { get; set; }
    
    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 扩展属性
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="etag">文件ETag</param>
    /// <param name="fileUrl">文件URL</param>
    /// <param name="size">文件大小</param>
    /// <returns>成功结果</returns>
    public static StorageResult CreateSuccess(string etag, string? fileUrl = null, long size = 0)
    {
        return new StorageResult
        {
            Success = true,
            ETag = etag,
            FileUrl = fileUrl,
            Size = size
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>失败结果</returns>
    public static StorageResult CreateFailed(string errorMessage)
    {
        return new StorageResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}