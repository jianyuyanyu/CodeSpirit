using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 文件存储服务接口
/// 提供高级文件管理功能
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="request">上传请求</param>
    /// <returns>文件信息</returns>
    Task<FileEntity> UploadFileAsync(FileUploadRequest request);
    
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件流和信息</returns>
    Task<(Stream Stream, FileEntity FileInfo)> DownloadFileAsync(long fileId);
    
    /// <summary>
    /// 获取文件下载URL
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载URL</returns>
    Task<string> GetDownloadUrlAsync(long fileId, int expirationMinutes = 60);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteFileAsync(long fileId);
    
    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    Task<FileEntity?> GetFileInfoAsync(long fileId);
    
    /// <summary>
    /// 批量删除文件
    /// </summary>
    /// <param name="fileIds">文件ID列表</param>
    /// <returns>删除结果</returns>
    Task<BatchOperationResult> BatchDeleteFilesAsync(IEnumerable<long> fileIds);
    
    /// <summary>
    /// 根据条件查询文件
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>文件列表</returns>
    Task<PageList<FileEntity>> QueryFilesAsync(FileQueryRequest request);
    
    /// <summary>
    /// 更新文件访问时间
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateAccessTimeAsync(long fileId);
}

/// <summary>
/// 文件上传请求
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = default!;
    
    /// <summary>
    /// 文件流
    /// </summary>
    public Stream FileStream { get; set; } = default!;
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpirationTime { get; set; }
    
    /// <summary>
    /// 自定义标签
    /// </summary>
    public IDictionary<string, string>? Tags { get; set; }
    
    /// <summary>
    /// 是否覆盖已存在文件
    /// </summary>
    public bool OverwriteExisting { get; set; }
    
    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; }
}

/// <summary>
/// 文件查询请求
/// </summary>
public class FileQueryRequest
{
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }
    
    /// <summary>
    /// 文件类型分类
    /// </summary>
    public FileTypeCategory? Category { get; set; }
    
    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus? Status { get; set; }
    
    /// <summary>
    /// 标签过滤
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// 文件名关键词
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// 创建开始时间
    /// </summary>
    public DateTime? CreatedFrom { get; set; }
    
    /// <summary>
    /// 创建结束时间
    /// </summary>
    public DateTime? CreatedTo { get; set; }
    
    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// 排序字段
    /// </summary>
    public string OrderBy { get; set; } = "CreatedTime";
    
    /// <summary>
    /// 是否降序
    /// </summary>
    public bool Descending { get; set; } = true;
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BatchOperationResult
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// 成功数量
    /// </summary>
    public int Success { get; set; }
    
    /// <summary>
    /// 失败数量
    /// </summary>
    public int Failed { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool IsAllSuccess => Failed == 0;
}
