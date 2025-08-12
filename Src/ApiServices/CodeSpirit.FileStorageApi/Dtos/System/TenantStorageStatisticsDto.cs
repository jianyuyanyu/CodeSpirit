using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 租户存储统计DTO
/// </summary>
public class TenantStorageStatisticsDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [DisplayName("租户名称")]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户显示名称
    /// </summary>
    [DisplayName("租户显示名称")]
    public string TenantDisplayName { get; set; } = string.Empty;

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
    /// 存储配额（字节）
    /// </summary>
    [DisplayName("存储配额")]
    public long? StorageQuota { get; set; }

    /// <summary>
    /// 存储配额（格式化显示）
    /// </summary>
    [DisplayName("配额大小")]
    public string? StorageQuotaFormatted { get; set; }

    /// <summary>
    /// 存储使用率（百分比）
    /// </summary>
    [DisplayName("使用率")]
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// 最后上传时间
    /// </summary>
    [DisplayName("最后上传时间")]
    public DateTime? LastUploadTime { get; set; }

    /// <summary>
    /// 激活的存储桶数量
    /// </summary>
    [DisplayName("存储桶数量")]
    public int ActiveBucketsCount { get; set; }

    /// <summary>
    /// 图片文件数量
    /// </summary>
    [DisplayName("图片文件")]
    public long ImageFilesCount { get; set; }

    /// <summary>
    /// 视频文件数量
    /// </summary>
    [DisplayName("视频文件")]
    public long VideoFilesCount { get; set; }

    /// <summary>
    /// 文档文件数量
    /// </summary>
    [DisplayName("文档文件")]
    public long DocumentFilesCount { get; set; }

    /// <summary>
    /// 其他文件数量
    /// </summary>
    [DisplayName("其他文件")]
    public long OtherFilesCount { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    [DisplayName("状态")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }
}
