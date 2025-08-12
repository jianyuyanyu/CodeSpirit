using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;

namespace CodeSpirit.FileStorageApi.Dtos.System;

/// <summary>
/// 系统文件查询DTO
/// </summary>
public class SystemFileQueryDto : QueryDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    public string? BucketName { get; set; }

    /// <summary>
    /// 文件名关键词
    /// </summary>
    [DisplayName("文件名")]
    public string? FileName { get; set; }

    /// <summary>
    /// 文件类型分类
    /// </summary>
    [DisplayName("文件分类")]
    public FileTypeCategory? Category { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    [DisplayName("文件状态")]
    public FileStatus? Status { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    [DisplayName("内容类型")]
    public string? ContentType { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [DisplayName("扩展名")]
    public string? Extension { get; set; }

    /// <summary>
    /// 最小文件大小（字节）
    /// </summary>
    [DisplayName("最小大小")]
    public long? MinSize { get; set; }

    /// <summary>
    /// 最大文件大小（字节）
    /// </summary>
    [DisplayName("最大大小")]
    public long? MaxSize { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    public bool? IsPublic { get; set; }

    /// <summary>
    /// 上传开始时间
    /// </summary>
    [DisplayName("上传开始时间")]
    public DateTime? UploadStartTime { get; set; }

    /// <summary>
    /// 上传结束时间
    /// </summary>
    [DisplayName("上传结束时间")]
    public DateTime? UploadEndTime { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    [DisplayName("上传者ID")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 标签过滤
    /// </summary>
    [DisplayName("标签")]
    public string? Tags { get; set; }

    /// <summary>
    /// 是否包含已过期文件
    /// </summary>
    [DisplayName("包含过期文件")]
    public bool IncludeExpired { get; set; } = false;

    /// <summary>
    /// 是否只显示有引用的文件
    /// </summary>
    [DisplayName("仅显示有引用文件")]
    public bool OnlyReferenced { get; set; } = false;
}
