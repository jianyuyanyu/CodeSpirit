# CodeSpirit.ImageProcessingService 图片处理服务集成指南

## 概述

ImageProcessingService 是 CodeSpirit 文件存储系统的核心组件，提供完整的图片上传、元数据提取、处理和管理功能。本服务基于 SixLabors.ImageSharp 和 MetadataExtractor 库，支持多种图片格式和高级处理操作。

## 主要功能

### 1. 智能图片上传
- **自动格式检测**: 支持 JPEG、PNG、GIF、WebP 等主流格式
- **元数据提取**: 自动提取图片尺寸、颜色深度、DPI 等基本信息
- **EXIF 信息解析**: 提取拍摄时间、相机型号、GPS 位置等详细信息
- **多租户支持**: 自动处理租户隔离和权限控制

### 2. 图片处理操作
- **缩放操作**: 支持按比例缩放和强制拉伸
- **裁剪操作**: 精确的矩形区域裁剪
- **旋转操作**: 任意角度旋转
- **格式转换**: 支持多种输出格式

### 3. 高级元数据管理
- **EXIF 数据**: 完整的相机设置信息
- **GPS 坐标**: 拍摄地理位置信息
- **动画检测**: 自动识别 GIF 动画和帧数
- **透明通道**: Alpha 通道支持检测

## 核心接口

### IImageProcessingService

```csharp
public interface IImageProcessingService
{
    /// <summary>
    /// 上传图片并提取元数据
    /// </summary>
    Task<ImageEntity> UploadImageAsync(ImageUploadRequest request);
    
    /// <summary>
    /// 获取图片信息
    /// </summary>
    Task<ImageEntity?> GetImageInfoAsync(long imageId);
    
    /// <summary>
    /// 处理图片
    /// </summary>
    Task<ProcessedImageResult> ProcessImageAsync(long imageId, 
        IEnumerable<ImageOperation> operations);
    
    /// <summary>
    /// 提取图片元数据
    /// </summary>
    Task<ImageMetadata> ExtractImageMetadataAsync(Stream stream);
}
```

## 使用示例

### 1. 图片上传

```csharp
// 在控制器中注入服务
private readonly IImageProcessingService _imageProcessingService;

// 上传图片
[HttpPost("upload")]
public async Task<ActionResult<ApiResponse<AmisImageDto>>> UploadImage(
    IFormFile file,
    [FromQuery] CreateImageDto createDto)
{
    var imageUploadRequest = new ImageUploadRequest
    {
        BucketName = createDto.BucketName ?? "default",
        FileName = file.FileName,
        FileStream = file.OpenReadStream(),
        ContentType = file.ContentType,
        Description = createDto.Description,
        Quality = createDto.Quality,
        ExtractExifData = createDto.ExtractExifData
    };

    ImageEntity imageEntity = await _imageProcessingService.UploadImageAsync(imageUploadRequest);
    
    // 返回 Amis 组件兼容的数据格式
    var result = new AmisImageDto
    {
        Id = imageEntity.Id,
        Value = imageEntity.DownloadUrl,
        Url = imageEntity.DownloadUrl,
        Name = imageEntity.OriginalFileName,
        Width = imageEntity.Width,
        Height = imageEntity.Height
    };
    
    return SuccessResponse(result);
}
```

### 2. 图片处理

```csharp
[HttpPost("{id}/process")]
public async Task<ActionResult<ApiResponse<ImageDto>>> ProcessImage(
    long id, 
    [FromBody] ImageProcessDto processDto)
{
    var operations = new List<ImageOperation>();
    
    // 添加缩放操作
    if (processDto.TargetWidth.HasValue || processDto.TargetHeight.HasValue)
    {
        operations.Add(new ResizeOperation
        {
            Width = processDto.TargetWidth ?? 0,
            Height = processDto.TargetHeight ?? 0,
            KeepAspectRatio = processDto.KeepAspectRatio
        });
    }
    
    // 执行处理
    ProcessedImageResult result = await _imageProcessingService.ProcessImageAsync(id, operations);
    
    return SuccessResponse($"处理完成，输出尺寸: {result.Width}x{result.Height}");
}
```

### 3. 获取图片信息

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<ImageDto>>> GetImageDetail(long id)
{
    ImageEntity? imageEntity = await _imageProcessingService.GetImageInfoAsync(id);
    if (imageEntity == null)
    {
        return BadResponse<ImageDto>("图片不存在");
    }

    var imageDto = new ImageDto
    {
        Id = imageEntity.Id,
        Width = imageEntity.Width,
        Height = imageEntity.Height,
        Format = imageEntity.Format,
        HasAlpha = imageEntity.HasAlpha,
        IsAnimated = imageEntity.IsAnimated,
        DateTaken = imageEntity.DateTaken,
        Latitude = imageEntity.GpsLocation.Latitude,
        Longitude = imageEntity.GpsLocation.Longitude
    };

    return SuccessResponse(imageDto);
}
```

## 数据模型

### ImageUploadRequest
```csharp
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
```

### ImageEntity
```csharp
public class ImageEntity
{
    public long Id { get; set; }
    public string OriginalFileName { get; set; }
    public long Size { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; }
    public bool HasAlpha { get; set; }
    public bool IsAnimated { get; set; }
    public DateTime? DateTaken { get; set; }
    public (double? Latitude, double? Longitude) GpsLocation { get; set; }
    // ... 其他属性
}
```

### 图片操作类型

#### 缩放操作
```csharp
var resizeOp = new ResizeOperation
{
    Width = 800,
    Height = 600,
    KeepAspectRatio = true  // 保持长宽比
};
```

#### 裁剪操作
```csharp
var cropOp = new CropOperation
{
    X = 100,      // 起始X坐标
    Y = 100,      // 起始Y坐标
    Width = 400,  // 裁剪宽度
    Height = 300  // 裁剪高度
};
```

#### 旋转操作
```csharp
var rotateOp = new RotateOperation
{
    Degrees = 90  // 顺时针旋转90度
};
```

## 支持的图片格式

| 格式 | 读取 | 写入 | 动画支持 | 透明通道 |
|------|------|------|----------|----------|
| JPEG | ✅ | ✅ | ❌ | ❌ |
| PNG | ✅ | ✅ | ❌ | ✅ |
| GIF | ✅ | ✅ | ✅ | ✅ |
| WebP | ✅ | ✅ | ✅ | ✅ |
| BMP | ✅ | ❌ | ❌ | ❌ |
| TIFF | ✅ | ❌ | ❌ | ✅ |

## EXIF 数据提取

服务自动提取以下 EXIF 信息：

### 基本信息
- **拍摄时间**: DateTaken
- **相机制造商**: CameraMake
- **相机型号**: CameraModel
- **GPS坐标**: Latitude, Longitude

### 相机设置
- **ISO感光度**: ISO
- **快门速度**: ShutterSpeed
- **光圈值**: Aperture
- **焦距**: FocalLength

## 数据库结构

### ImageMetadataEntity
```csharp
[Table("ImageMetadata")]
public class ImageMetadataEntity : LongKeyAuditableEntityBase, IMultiTenant
{
    public string TenantId { get; set; }        // 租户ID
    public long FileId { get; set; }            // 关联的文件ID
    public int Width { get; set; }              // 图片宽度
    public int Height { get; set; }             // 图片高度
    public int ColorDepth { get; set; }         // 颜色深度
    public string Format { get; set; }          // 图片格式
    public bool HasAlpha { get; set; }          // 是否有透明通道
    public bool IsAnimated { get; set; }        // 是否为动画
    public int FrameCount { get; set; }         // 帧数
    public double DpiX { get; set; }            // 水平DPI
    public double DpiY { get; set; }            // 垂直DPI
    public string CameraModel { get; set; }     // 相机型号
    public DateTime? DateTaken { get; set; }    // 拍摄时间
    public double? Latitude { get; set; }       // GPS纬度
    public double? Longitude { get; set; }      // GPS经度
    public string ExifData { get; set; }        // EXIF数据（JSON）
    public string ColorPalette { get; set; }    // 主色调信息
}
```

## 配置和注册

### 服务注册
服务已在 `ServiceCollectionExtensions.cs` 中自动注册：

```csharp
// 注册图片处理服务
services.AddScoped<IImageProcessingService, Services.ImageProcessingService>();
```

### 依赖注入
ImageProcessingService 依赖以下服务：
- `IFileStorageService`: 文件存储服务
- `FileStorageDbContext`: 数据库上下文
- `IIdGenerator`: ID生成器
- `ICurrentUser`: 当前用户信息
- `IMapper`: AutoMapper 映射器

## 性能优化

### 1. 流处理优化
- 使用流位置管理，避免重复读取
- 支持大文件处理，内存占用可控

### 2. 异步处理
- 所有操作均为异步，不阻塞主线程
- 支持并发处理多个图片

### 3. 错误处理
- 优雅的异常处理，不会因EXIF提取失败而中断流程
- 详细的错误日志记录

## 最佳实践

### 1. 图片上传
```csharp
// 推荐的上传配置
var uploadRequest = new ImageUploadRequest
{
    Quality = 85,           // 平衡质量和文件大小
    ExtractExifData = true, // 启用EXIF提取
    BucketName = "images",  // 使用专门的图片存储桶
};
```

### 2. 批量处理
```csharp
// 对于批量处理，建议使用Task.WhenAll
var tasks = imageIds.Select(id => 
    _imageProcessingService.ProcessImageAsync(id, operations));
var results = await Task.WhenAll(tasks);
```

### 3. 错误处理
```csharp
try
{
    var result = await _imageProcessingService.UploadImageAsync(request);
    _logger.LogInformation("图片上传成功: {FileName}", request.FileName);
    return result;
}
catch (Exception ex)
{
    _logger.LogError(ex, "图片处理失败: {FileName}", request.FileName);
    throw; // 重新抛出，让上层处理
}
```

## 集成检查清单

- [ ] 确认 NuGet 包已安装（SixLabors.ImageSharp, MetadataExtractor）
- [ ] 验证服务已注册到 DI 容器
- [ ] 检查数据库迁移是否已应用
- [ ] 确认多租户配置正确
- [ ] 测试各种图片格式的上传
- [ ] 验证 EXIF 数据提取功能
- [ ] 测试图片处理操作
- [ ] 检查错误处理和日志记录

## 故障排除

### 常见问题

1. **图片上传失败**
   - 检查文件格式是否支持
   - 验证存储桶配置
   - 确认权限设置

2. **EXIF数据丢失**
   - 确认 ExtractExifData = true
   - 检查图片是否包含EXIF信息
   - 验证MetadataExtractor库版本

3. **处理性能问题**
   - 考虑图片大小限制
   - 使用异步处理
   - 监控内存使用情况

本指南提供了 ImageProcessingService 的完整集成说明，确保开发团队能够高效地使用图片处理功能。
