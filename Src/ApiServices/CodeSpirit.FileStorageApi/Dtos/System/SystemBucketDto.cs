using System.ComponentModel;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Abstractions;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 系统存储桶DTO
/// </summary>
public class SystemBucketDto
{
    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶名称")]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [DisplayName("显示名称")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 存储提供程序
    /// </summary>
    [DisplayName("存储提供程序")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 访问策略
    /// </summary>
    [DisplayName("访问策略")]
    public BucketAccessPolicy AccessPolicy { get; set; }

    ///// <summary>
    ///// 存储配额（字节）
    ///// </summary>
    //[DisplayName("存储配额")]
    //public long? StorageQuota { get; set; }

    /// <summary>
    /// 存储配额（格式化显示）
    /// </summary>
    [DisplayName("存储配额")]
    public string? StorageQuotaFormatted { get; set; }

    ///// <summary>
    ///// 最大文件大小（字节）
    ///// </summary>
    //[DisplayName("最大文件大小")]
    //public long MaxFileSize { get; set; }

    /// <summary>
    /// 最大文件大小（格式化显示）
    /// </summary>
    [DisplayName("文件大小限制")]
    public string MaxFileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 允许的文件类型
    /// </summary>
    [DisplayName("允许的文件类型")]
    public string? AllowedFileTypes { get; set; }

    /// <summary>
    /// 禁止的文件类型
    /// </summary>
    [DisplayName("禁止的文件类型")]
    public string? ForbiddenFileTypes { get; set; }

    /// <summary>
    /// 保留天数
    /// </summary>
    [DisplayName("保留天数")]
    public int? RetentionDays { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 使用该存储桶的租户数量
    /// </summary>
    [DisplayName("使用租户数")]
    public int UsageTenantsCount { get; set; }

    /// <summary>
    /// 文件总数
    /// </summary>
    [DisplayName("文件总数")]
    public long TotalFiles { get; set; }

    /// <summary>
    /// 总存储大小（字节）
    /// </summary>
    [DisplayName("总存储大小")]
    public long TotalStorageSize { get; set; }

    /// <summary>
    /// 总存储大小（格式化显示）
    /// </summary>
    [DisplayName("存储大小")]
    public string TotalStorageSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 最后上传时间
    /// </summary>
    [DisplayName("最后上传时间")]
    public DateTime? LastUploadTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }
}
