using CodeSpirit.Core.Attributes;
using CodeSpirit.FileStorageApi.Entities;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 系统文件DTO
/// </summary>
public class SystemFileDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [DisplayName("文件名")]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名
    /// </summary>
    [DisplayName("存储文件名")]
    public string StorageFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    [DisplayName("文件路径")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [DisplayName("文件大小")]
    public long Size { get; set; }

    /// <summary>
    /// 文件大小（格式化显示）
    /// </summary>
    [DisplayName("大小")]
    public string SizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 内容类型
    /// </summary>
    [DisplayName("文件类型")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件哈希值
    /// </summary>
    [DisplayName("文件哈希")]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [DisplayName("扩展名")]
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 文件分类
    /// </summary>
    [DisplayName("分类")]
    public FileTypeCategory Category { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 文件状态
    /// </summary>
    [DisplayName("状态")]
    public FileStatus Status { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    [DisplayName("访问次数")]
    public long AccessCount { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [DisplayName("最后访问时间")]
    public DateTime? LastAccessTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// 下载URL
    /// </summary>
    [DisplayName("下载链接")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// ETag
    /// </summary>
    [DisplayName("ETag")]
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// 引用数量
    /// </summary>
    [DisplayName("引用数")]
    public int ReferenceCount { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    [DisplayName("上传者ID")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("上传时间")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [DisplayName("修改时间")]
    public DateTime? ModifiedTime { get; set; }
}
