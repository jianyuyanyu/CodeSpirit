using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 图片元数据实体
/// 存储图片的详细信息和处理结果
/// </summary>
[Table("ImageMetadata")]
public class ImageMetadataEntity : LongKeyAuditableEntityBase
{
    /// <summary>
    /// 文件ID（一对一关系）
    /// </summary>
    [Required]
    public long FileId { get; set; }
    
    /// <summary>
    /// 图片宽度（像素）
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 图片高度（像素）
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 颜色深度（位）
    /// </summary>
    public int ColorDepth { get; set; }
    
    /// <summary>
    /// 图片格式
    /// </summary>
    [MaxLength(32)]
    public string Format { get; set; }
    
    /// <summary>
    /// 是否有透明通道
    /// </summary>
    public bool HasAlpha { get; set; }
    
    /// <summary>
    /// 是否为动画图片
    /// </summary>
    public bool IsAnimated { get; set; }
    
    /// <summary>
    /// 帧数（动画图片）
    /// </summary>
    public int FrameCount { get; set; }
    
    /// <summary>
    /// DPI水平分辨率
    /// </summary>
    public double DpiX { get; set; }
    
    /// <summary>
    /// DPI垂直分辨率
    /// </summary>
    public double DpiY { get; set; }
    
    /// <summary>
    /// 拍摄设备
    /// </summary>
    [MaxLength(256)]
    public string CameraModel { get; set; }
    
    /// <summary>
    /// 拍摄时间
    /// </summary>
    public DateTime? DateTaken { get; set; }
    
    /// <summary>
    /// GPS纬度
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// GPS经度
    /// </summary>
    public double? Longitude { get; set; }
    
    /// <summary>
    /// EXIF数据（JSON格式）
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string ExifData { get; set; }
    
    /// <summary>
    /// 主色调信息（JSON格式）
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string ColorPalette { get; set; }
    
    /// <summary>
    /// 关联的文件
    /// </summary>
    public virtual FileEntity File { get; set; }
    
    /// <summary>
    /// 缩略图
    /// </summary>
    public virtual ICollection<ThumbnailEntity> Thumbnails { get; set; } = new List<ThumbnailEntity>();
}
