using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 文件实体
/// 存储文件的基本信息和元数据
/// </summary>
[Table("Files")]
public class FileEntity : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string TenantId { get; set; }
    
    /// <summary>
    /// 存储桶名称（引用配置中的存储桶）
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string BucketName { get; set; }
    
    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string OriginalFileName { get; set; }
    
    /// <summary>
    /// 存储文件名（在存储系统中的唯一标识）
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string StorageFileName { get; set; }
    
    /// <summary>
    /// 文件路径（在存储桶内的路径）
    /// </summary>
    [MaxLength(1024)]
    public string FilePath { get; set; }
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [Required]
    public long Size { get; set; }
    
    /// <summary>
    /// 内容类型（MIME类型）
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ContentType { get; set; }
    
    /// <summary>
    /// 文件哈希值（MD5）
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string FileHash { get; set; }
    
    /// <summary>
    /// 文件扩展名
    /// </summary>
    [MaxLength(32)]
    public string Extension { get; set; }
    
    /// <summary>
    /// 文件类型分类
    /// </summary>
    public FileTypeCategory Category { get; set; }
    
    /// <summary>
    /// 文件描述
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; }
    
    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus Status { get; set; } = FileStatus.Active;
    
    /// <summary>
    /// 访问次数
    /// </summary>
    public long AccessCount { get; set; }
    
    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; set; }
    
    /// <summary>
    /// 过期时间（null表示永不过期）
    /// </summary>
    public DateTime? ExpirationTime { get; set; }
    
    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; }
    
    /// <summary>
    /// 下载URL（临时或永久）
    /// </summary>
    [MaxLength(2048)]
    public string DownloadUrl { get; set; }
    
    /// <summary>
    /// ETag（用于缓存控制）
    /// </summary>
    [MaxLength(256)]
    public string ETag { get; set; }
    
    /// <summary>
    /// 文件标签（多个标签用逗号分隔）
    /// </summary>
    [MaxLength(2000)]
    public string Tags { get; set; }
    
    /// <summary>
    /// 自定义属性（JSON格式）
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Properties { get; set; }
    
    /// <summary>
    /// 文件引用
    /// </summary>
    public virtual ICollection<FileReferenceEntity> References { get; set; } = new List<FileReferenceEntity>();
    
    /// <summary>
    /// 图片元数据（如果是图片文件）
    /// </summary>
    public virtual ImageMetadataEntity ImageMetadata { get; set; }
    
    /// <summary>
    /// 视频元数据（如果是视频文件）
    /// </summary>
    public virtual VideoMetadataEntity VideoMetadata { get; set; }
}
