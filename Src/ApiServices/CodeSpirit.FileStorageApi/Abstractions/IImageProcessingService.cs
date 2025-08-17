using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Dtos;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 图片处理服务接口
/// </summary>
public interface IImageProcessingService : IScopedDependency
{
    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="request">上传请求</param>
    /// <returns>图片信息</returns>
    Task<ImageEntity> UploadImageAsync(ImageUploadRequest request);
    

    
    /// <summary>
    /// 获取图片信息
    /// </summary>
    /// <param name="imageId">图片ID</param>
    /// <returns>图片信息</returns>
    Task<ImageEntity?> GetImageInfoAsync(long imageId);
    
    /// <summary>
    /// 处理图片
    /// </summary>
    /// <param name="imageId">图片ID</param>
    /// <param name="operations">处理操作</param>
    /// <returns>处理后的图片</returns>
    Task<ProcessedImageResult> ProcessImageAsync(long imageId, 
        IEnumerable<ImageOperation> operations);
    
    /// <summary>
    /// 提取图片元数据
    /// </summary>
    /// <param name="stream">图片流</param>
    /// <returns>图片元数据</returns>
    Task<ImageMetadata> ExtractImageMetadataAsync(Stream stream);

    /// <summary>
    /// 查询图片列表（包含图片元数据）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片分页列表</returns>
    Task<PageList<ImageEntity>> QueryImagesAsync(ImageQueryDto queryDto);
}

/// <summary>
/// 图片上传请求
/// </summary>
public class ImageUploadRequest : FileUploadRequest
{

    
    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    public int Quality { get; set; } = 85;
    
    /// <summary>
    /// 是否提取EXIF信息
    /// </summary>
    public bool ExtractExifData { get; set; } = true;
}



/// <summary>
/// 图片操作
/// </summary>
public abstract class ImageOperation
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; }
}

/// <summary>
/// 调整大小操作
/// </summary>
public class ResizeOperation : ImageOperation
{
    public int Width { get; set; }
    public int Height { get; set; }
    public bool KeepAspectRatio { get; set; } = true;
    
    public ResizeOperation()
    {
        OperationType = "Resize";
    }
}

/// <summary>
/// 裁剪操作
/// </summary>
public class CropOperation : ImageOperation
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public CropOperation()
    {
        OperationType = "Crop";
    }
}

/// <summary>
/// 旋转操作
/// </summary>
public class RotateOperation : ImageOperation
{
    public int Degrees { get; set; }
    
    public RotateOperation()
    {
        OperationType = "Rotate";
    }
}

/// <summary>
/// 处理后的图片结果
/// </summary>
public class ProcessedImageResult
{
    /// <summary>
    /// 处理后的图片流
    /// </summary>
    public Stream ImageStream { get; set; }
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; }
    
    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// 宽度
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 高度
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// 图片元数据
/// </summary>
public class ImageMetadata
{
    /// <summary>
    /// 宽度
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// 高度
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// 颜色深度
    /// </summary>
    public int ColorDepth { get; set; }
    
    /// <summary>
    /// 格式
    /// </summary>
    public string Format { get; set; }
    
    /// <summary>
    /// 是否有透明通道
    /// </summary>
    public bool HasAlpha { get; set; }
    
    /// <summary>
    /// EXIF数据
    /// </summary>
    public Dictionary<string, object> ExifData { get; set; } = new();
    
    /// <summary>
    /// DPI信息
    /// </summary>
    public (double X, double Y) Dpi { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? DateTaken { get; set; }
    
    /// <summary>
    /// GPS信息
    /// </summary>
    public (double? Latitude, double? Longitude) GpsLocation { get; set; }
}
