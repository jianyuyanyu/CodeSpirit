using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using Newtonsoft.Json;

namespace CodeSpirit.FileStorageApi.Dtos;

/// <summary>
/// 文件信息DTO
/// </summary>
public class FileDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [DisplayName("文件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    public string? BucketName { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    [DisplayName("文件名")]
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [DisplayName("文件大小")]
    public long Size { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    [DisplayName("文件类型")]
    public string? ContentType { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [DisplayName("扩展名")]
    public string? Extension { get; set; }

    /// <summary>
    /// 文件类型分类
    /// </summary>
    [DisplayName("分类")]
    public FileTypeCategory Category { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    [DisplayName("状态")]
    public FileStatus Status { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    [DisplayName("访问次数")]
    public long AccessCount { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [DisplayName("最后访问")]
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
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// 文件标签
    /// </summary>
    [DisplayName("标签")]
    public string? Tags { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [DisplayName("创建者")]
    [AggregateField("/identity/users/{value}.userName")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 更新者
    /// </summary>
    [DisplayName("更新者")]
    [AggregateField("/identity/users/{value}.userName")]
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 文件查询DTO
/// </summary>
public class FileQueryDto : QueryDtoBase
{
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
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    public bool? IsPublic { get; set; }

    /// <summary>
    /// 标签过滤
    /// </summary>
    [DisplayName("标签")]
    public string? Tags { get; set; }

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
    /// 创建开始时间
    /// </summary>
    [DisplayName("创建时间从")]
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// 创建结束时间
    /// </summary>
    [DisplayName("创建时间到")]
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// 过期开始时间
    /// </summary>
    [DisplayName("过期时间从")]
    public DateTime? ExpirationFrom { get; set; }

    /// <summary>
    /// 过期结束时间
    /// </summary>
    [DisplayName("过期时间到")]
    public DateTime? ExpirationTo { get; set; }
}

/// <summary>
/// 创建文件DTO
/// </summary>
public class CreateFileDto
{
    /// <summary>
    /// 存储桶名称
    /// </summary>
    [Required]
    [DisplayName("存储桶")]
    public string BucketName { get; set; } = "default";

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("文件描述")]
    [MaxLength(1000)]
    public string? Description { get; set; }

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
    /// 文件标签
    /// </summary>
    [DisplayName("标签")]
    [MaxLength(2000)]
    public string? Tags { get; set; }

    /// <summary>
    /// 是否覆盖已存在文件
    /// </summary>
    [DisplayName("覆盖已存在文件")]
    public bool OverwriteExisting { get; set; }
}

/// <summary>
/// 更新文件DTO
/// </summary>
public class UpdateFileDto
{
    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("文件描述")]
    [MaxLength(1000)]
    public string? Description { get; set; }

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
    /// 文件标签
    /// </summary>
    [DisplayName("标签")]
    [MaxLength(2000)]
    public string? Tags { get; set; }
}

/// <summary>
/// 文件批量导入项DTO
/// </summary>
public class FileBatchImportItemDto
{
    /// <summary>
    /// 文件URL或路径
    /// </summary>
    [Required]
    [DisplayName("文件地址")]
    [JsonProperty("文件地址")]
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required]
    [DisplayName("文件名")]
    [JsonProperty("文件名")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [DisplayName("存储桶")]
    [JsonProperty("存储桶")]
    public string BucketName { get; set; } = "default";

    /// <summary>
    /// 文件描述
    /// </summary>
    [DisplayName("描述")]
    [JsonProperty("描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [DisplayName("公开访问")]
    [JsonProperty("公开访问")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// 文件标签
    /// </summary>
    [DisplayName("标签")]
    [JsonProperty("标签")]
    public string? Tags { get; set; }
}
