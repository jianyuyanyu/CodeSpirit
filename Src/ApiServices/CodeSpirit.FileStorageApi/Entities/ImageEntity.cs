namespace CodeSpirit.FileStorageApi.Entities;

/// <summary>
/// 图片实体（组合视图）
/// 包含文件信息和图片元数据的组合实体，用于简化图片操作
/// </summary>
public class ImageEntity
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; }
    
    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string BucketName { get; set; }
    
    /// <summary>
    /// 原始文件名
    /// </summary>
    public string OriginalFileName { get; set; }
    
    /// <summary>
    /// 存储文件名
    /// </summary>
    public string StorageFileName { get; set; }
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; }
    
    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string FileHash { get; set; }
    
    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus Status { get; set; }
    
    /// <summary>
    /// 下载URL
    /// </summary>
    public string DownloadUrl { get; set; }
    
    /// <summary>
    /// 文件标签
    /// </summary>
    public string Tags { get; set; }
    
    /// <summary>
    /// 图片宽度
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 图片高度
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 图片格式
    /// </summary>
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
    /// 拍摄时间
    /// </summary>
    public DateTime? DateTaken { get; set; }
    
    /// <summary>
    /// GPS位置
    /// </summary>
    public (double? Latitude, double? Longitude) GpsLocation { get; set; }
    
    /// <summary>
    /// 缩略图列表
    /// </summary>
    public List<ThumbnailInfo> Thumbnails { get; set; } = new();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedTime { get; set; }
}

/// <summary>
/// 缩略图信息
/// </summary>
public class ThumbnailInfo
{
    /// <summary>
    /// 缩略图ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 尺寸标识
    /// </summary>
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
    /// 缩略图文件ID
    /// </summary>
    public long ThumbnailFileId { get; set; }
    
    /// <summary>
    /// 缩略图下载URL
    /// </summary>
    public string DownloadUrl { get; set; }
}
