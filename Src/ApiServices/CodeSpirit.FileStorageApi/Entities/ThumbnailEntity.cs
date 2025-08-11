using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 缩略图实体
/// </summary>
[Table("Thumbnails")]
public class ThumbnailEntity : LongKeyAuditableEntityBase
{
    /// <summary>
    /// 图片元数据ID
    /// </summary>
    [Required]
    public long ImageMetadataId { get; set; }
    
    /// <summary>
    /// 缩略图文件ID
    /// </summary>
    [Required]
    public long ThumbnailFileId { get; set; }
    
    /// <summary>
    /// 缩略图尺寸标识
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string SizeKey { get; set; }
    
    /// <summary>
    /// 宽度
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 高度
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 关联的图片元数据
    /// </summary>
    public virtual ImageMetadataEntity ImageMetadata { get; set; }
    
    /// <summary>
    /// 缩略图文件
    /// </summary>
    public virtual FileEntity ThumbnailFile { get; set; }
}
