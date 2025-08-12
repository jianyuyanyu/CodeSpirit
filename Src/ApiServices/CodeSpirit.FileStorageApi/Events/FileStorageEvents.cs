using CodeSpirit.Shared.EventBus.Events;

namespace CodeSpirit.FileStorageApi.Events;

/// <summary>
/// 文件上传事件
/// </summary>
public class FileUploadedEvent : TenantAwareEventBase
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long FileId { get; set; }
    
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string BucketName { get; set; } = default!;
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = default!;
    
    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; } = default!;
    
    /// <summary>
    /// 文件分类
    /// </summary>
    public string Category { get; set; } = default!;
    
    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// 上传用户
    /// </summary>
    public long? UploadedBy { get; set; }
}

/// <summary>
/// 文件删除事件
/// </summary>
public class FileDeletedEvent : TenantAwareEventBase
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long FileId { get; set; }
    
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string BucketName { get; set; } = default!;
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = default!;
    
    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; set; }
    
    /// <summary>
    /// 删除用户
    /// </summary>
    public long? DeletedBy { get; set; }
}

/// <summary>
/// 文件下载事件
/// </summary>
public class FileDownloadedEvent : TenantAwareEventBase
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long FileId { get; set; }
    
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string BucketName { get; set; } = default!;
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = default!;
    
    /// <summary>
    /// 下载时间
    /// </summary>
    public DateTime DownloadedAt { get; set; }
    
    /// <summary>
    /// 下载用户
    /// </summary>
    public long? DownloadedBy { get; set; }
    
    /// <summary>
    /// 下载方式（直接下载/URL下载）
    /// </summary>
    public string DownloadMethod { get; set; } = default!;
}