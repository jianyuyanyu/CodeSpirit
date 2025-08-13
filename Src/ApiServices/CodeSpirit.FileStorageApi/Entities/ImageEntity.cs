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
    /// 颜色深度（位）
    /// </summary>
    public int ColorDepth { get; set; }
    
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
    public string? CameraModel { get; set; }
    
    /// <summary>
    /// 文件描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 访问次数
    /// </summary>
    public long AccessCount { get; set; }
    
    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessTime { get; set; }
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpirationTime { get; set; }
    
    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; }
    
    /// <summary>
    /// 创建人ID
    /// </summary>
    public long CreatedBy { get; set; }
    
    /// <summary>
    /// 更新人ID
    /// </summary>
    public long? UpdatedBy { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedTime { get; set; }
}


