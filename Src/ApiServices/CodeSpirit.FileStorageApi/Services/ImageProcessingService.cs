using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Dtos;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Newtonsoft.Json;
using AutoMapper;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 图片处理服务实现
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly FileStorageDbContext _context;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    /// <param name="fileStorageService">文件存储服务</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="idGenerator">ID生成器</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="mapper">对象映射器</param>
    public ImageProcessingService(
        ILogger<ImageProcessingService> logger,
        IFileStorageService fileStorageService,
        FileStorageDbContext context,
        IIdGenerator idGenerator,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 上传图片并提取元数据
    /// </summary>
    /// <param name="request">图片上传请求</param>
    /// <returns>图片实体信息</returns>
    public async Task<ImageEntity> UploadImageAsync(ImageUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FileStream);

        _logger.LogInformation("开始上传图片: {FileName}", request.FileName);

        try
        {
            // 1. 验证图片格式
            var originalPosition = request.FileStream.Position;
            var metadata = await ExtractImageMetadataAsync(request.FileStream);
            request.FileStream.Position = originalPosition;

            // 2. 上传文件到存储服务
            var fileUploadRequest = new FileUploadRequest
            {
                BucketName = request.BucketName,
                FileName = request.FileName,
                FileStream = request.FileStream,
                ContentType = request.ContentType,
                Description = request.Description,
                ExpirationTime = request.ExpirationTime,
                Tags = request.Tags,
                OverwriteExisting = request.OverwriteExisting,
                IsPublic = request.IsPublic
            };

            var fileEntity = await _fileStorageService.UploadFileAsync(fileUploadRequest);

            // 3. 保存图片元数据
            var imageMetadataEntity = new ImageMetadataEntity
            {
                Id = _idGenerator.NewId(),
                FileId = fileEntity.Id,
                Width = metadata.Width,
                Height = metadata.Height,
                ColorDepth = metadata.ColorDepth,
                Format = metadata.Format,
                HasAlpha = metadata.HasAlpha,
                IsAnimated = IsAnimatedFormat(metadata.Format),
                FrameCount = IsAnimatedFormat(metadata.Format) ? GetFrameCount(request.FileStream) : 1,
                DpiX = metadata.Dpi.X,
                DpiY = metadata.Dpi.Y,
                DateTaken = metadata.DateTaken,
                Latitude = metadata.GpsLocation.Latitude,
                Longitude = metadata.GpsLocation.Longitude,
                ExifData = metadata.ExifData.Any() ? JsonConvert.SerializeObject(metadata.ExifData) : null,
                CameraModel = ExtractCameraModel(metadata.ExifData),
                TenantId = _currentUser.TenantId!,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Id ?? 0,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = _currentUser.Id ?? 0
            };

            _context.ImageMetadata.Add(imageMetadataEntity);
            await _context.SaveChangesAsync();

            // 4. 构造并返回ImageEntity
            var imageEntity = new ImageEntity
            {
                Id = fileEntity.Id,
                TenantId = fileEntity.TenantId,
                BucketName = fileEntity.BucketName,
                OriginalFileName = fileEntity.OriginalFileName,
                StorageFileName = fileEntity.StorageFileName,
                Size = fileEntity.Size,
                ContentType = fileEntity.ContentType,
                FileHash = fileEntity.FileHash,
                Status = fileEntity.Status,
                DownloadUrl = fileEntity.DownloadUrl,
                Tags = fileEntity.Tags,
                Width = metadata.Width,
                Height = metadata.Height,
                Format = metadata.Format,
                HasAlpha = metadata.HasAlpha,
                IsAnimated = IsAnimatedFormat(metadata.Format),
                DateTaken = metadata.DateTaken,
                GpsLocation = metadata.GpsLocation,
                CreatedTime = fileEntity.CreatedAt,
                UpdatedTime = fileEntity.UpdatedAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("图片上传成功: {FileId}, 尺寸: {Width}x{Height}", 
                fileEntity.Id, metadata.Width, metadata.Height);

            return imageEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传图片失败: {FileName}", request.FileName);
            throw;
        }
    }

    /// <summary>
    /// 获取图片信息
    /// </summary>
    /// <param name="imageId">图片ID</param>
    /// <returns>图片信息</returns>
    public async Task<ImageEntity?> GetImageInfoAsync(long imageId)
    {
        _logger.LogDebug("获取图片信息: {ImageId}", imageId);

        try
        {
            // 联查文件和图片元数据
            var result = await _context.Files
                .Include(f => f.ImageMetadata)
                .Where(f => f.Id == imageId && f.TenantId == _currentUser.TenantId)
                .Select(f => new ImageEntity
                {
                    Id = f.Id,
                    TenantId = f.TenantId,
                    BucketName = f.BucketName,
                    OriginalFileName = f.OriginalFileName,
                    StorageFileName = f.StorageFileName,
                    Size = f.Size,
                    ContentType = f.ContentType,
                    FileHash = f.FileHash,
                    Status = f.Status,
                    DownloadUrl = f.DownloadUrl,
                    Tags = f.Tags,
                    Width = f.ImageMetadata != null ? f.ImageMetadata.Width : 0,
                    Height = f.ImageMetadata != null ? f.ImageMetadata.Height : 0,
                    Format = f.ImageMetadata != null ? f.ImageMetadata.Format ?? string.Empty : string.Empty,
                    HasAlpha = f.ImageMetadata != null && f.ImageMetadata.HasAlpha,
                    IsAnimated = f.ImageMetadata != null && f.ImageMetadata.IsAnimated,
                    DateTaken = f.ImageMetadata != null ? f.ImageMetadata.DateTaken : null,
                    GpsLocation = f.ImageMetadata != null ? 
                        new ValueTuple<double?, double?>(f.ImageMetadata.Latitude, f.ImageMetadata.Longitude) : 
                        new ValueTuple<double?, double?>(null, null),
                    CreatedTime = f.CreatedAt,
                    UpdatedTime = f.UpdatedAt ?? DateTime.UtcNow
                })
                .FirstOrDefaultAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取图片信息失败: {ImageId}", imageId);
            throw;
        }
    }

    /// <summary>
    /// 处理图片
    /// </summary>
    /// <param name="imageId">图片ID</param>
    /// <param name="operations">处理操作列表</param>
    /// <returns>处理后的图片结果</returns>
    public async Task<ProcessedImageResult> ProcessImageAsync(long imageId, IEnumerable<ImageOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        _logger.LogInformation("开始处理图片: {ImageId}, 操作数量: {OperationCount}", 
            imageId, operations.Count());

        try
        {
            // 1. 获取原始图片文件
            var (stream, fileInfo) = await _fileStorageService.DownloadFileAsync(imageId);
            
            using var image = await Image.LoadAsync(stream);
            stream.Dispose();

            // 2. 应用所有操作
            foreach (var operation in operations)
            {
                ApplyImageOperation(image, operation);
            }

            // 3. 输出处理后的图片
            var outputStream = new MemoryStream();
            var format = GetImageFormat(fileInfo.ContentType);
            
            await image.SaveAsync(outputStream, format);
            outputStream.Position = 0;

            var result = new ProcessedImageResult
            {
                ImageStream = outputStream,
                ContentType = fileInfo.ContentType,
                Size = outputStream.Length,
                Width = image.Width,
                Height = image.Height
            };

            _logger.LogInformation("图片处理完成: {ImageId}, 输出尺寸: {Width}x{Height}, 大小: {Size}字节", 
                imageId, result.Width, result.Height, result.Size);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片处理失败: {ImageId}", imageId);
            throw;
        }
    }

    /// <summary>
    /// 查询图片列表（包含图片元数据）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片分页列表</returns>
    public async Task<PageList<ImageEntity>> QueryImagesAsync(ImageQueryDto queryDto)
    {
        ArgumentNullException.ThrowIfNull(queryDto);

        _logger.LogDebug("开始查询图片列表: {QueryDto}", JsonConvert.SerializeObject(queryDto));

        try
        {
            // 基于Files表和ImageMetadata表进行联查
            var query = _context.Files
                .Include(f => f.ImageMetadata)
                .Where(f => f.Category == FileTypeCategory.Image && f.TenantId == _currentUser.TenantId);

            // 应用查询条件
            if (!string.IsNullOrEmpty(queryDto.BucketName))
            {
                query = query.Where(f => f.BucketName == queryDto.BucketName);
            }

            if (!string.IsNullOrEmpty(queryDto.FileName))
            {
                query = query.Where(f => f.OriginalFileName.Contains(queryDto.FileName));
            }

            if (queryDto.CreatedFrom.HasValue)
            {
                query = query.Where(f => f.CreatedAt >= queryDto.CreatedFrom.Value);
            }

            if (queryDto.CreatedTo.HasValue)
            {
                query = query.Where(f => f.CreatedAt <= queryDto.CreatedTo.Value);
            }

            // 基于图片元数据的查询条件
            if (!string.IsNullOrEmpty(queryDto.Format))
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.Format == queryDto.Format);
            }

            if (queryDto.MinWidth.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.Width >= queryDto.MinWidth.Value);
            }

            if (queryDto.MaxWidth.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.Width <= queryDto.MaxWidth.Value);
            }

            if (queryDto.MinHeight.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.Height >= queryDto.MinHeight.Value);
            }

            if (queryDto.MaxHeight.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.Height <= queryDto.MaxHeight.Value);
            }

            if (queryDto.IsAnimated.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.IsAnimated == queryDto.IsAnimated.Value);
            }

            if (queryDto.HasAlpha.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && f.ImageMetadata.HasAlpha == queryDto.HasAlpha.Value);
            }

            if (!string.IsNullOrEmpty(queryDto.CameraModel))
            {
                query = query.Where(f => f.ImageMetadata != null && 
                    f.ImageMetadata.CameraModel != null && 
                    f.ImageMetadata.CameraModel.Contains(queryDto.CameraModel));
            }

            if (queryDto.DateTakenFrom.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && 
                    f.ImageMetadata.DateTaken.HasValue && 
                    f.ImageMetadata.DateTaken.Value >= queryDto.DateTakenFrom.Value);
            }

            if (queryDto.DateTakenTo.HasValue)
            {
                query = query.Where(f => f.ImageMetadata != null && 
                    f.ImageMetadata.DateTaken.HasValue && 
                    f.ImageMetadata.DateTaken.Value <= queryDto.DateTakenTo.Value);
            }

            // 排序
            query = queryDto.OrderBy?.ToLower() switch
            {
                "filename" => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.OriginalFileName) : 
                    query.OrderBy(f => f.OriginalFileName),
                "size" => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.Size) : 
                    query.OrderBy(f => f.Size),
                "width" => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.ImageMetadata != null ? f.ImageMetadata.Width : 0) : 
                    query.OrderBy(f => f.ImageMetadata != null ? f.ImageMetadata.Width : 0),
                "height" => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.ImageMetadata != null ? f.ImageMetadata.Height : 0) : 
                    query.OrderBy(f => f.ImageMetadata != null ? f.ImageMetadata.Height : 0),
                "format" => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.ImageMetadata != null ? f.ImageMetadata.Format : string.Empty) : 
                    query.OrderBy(f => f.ImageMetadata != null ? f.ImageMetadata.Format : string.Empty),
                _ => queryDto.OrderDir == "desc" ? 
                    query.OrderByDescending(f => f.CreatedAt) : 
                    query.OrderBy(f => f.CreatedAt)
            };

            // 分页查询
            var totalCount = await query.CountAsync();
            var pageSize = queryDto.PerPage > 0 ? queryDto.PerPage : 20;
            var pageNumber = queryDto.Page > 0 ? queryDto.Page : 1;

            var fileEntities = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 转换为ImageEntity列表
            var imageEntities = fileEntities.Select(f => new ImageEntity
            {
                Id = f.Id,
                TenantId = f.TenantId,
                BucketName = f.BucketName,
                OriginalFileName = f.OriginalFileName,
                StorageFileName = f.StorageFileName,
                Size = f.Size,
                ContentType = f.ContentType,
                FileHash = f.FileHash,
                Status = f.Status,
                DownloadUrl = f.DownloadUrl,
                Tags = f.Tags,
                // 文件基本属性
                Description = f.Description,
                AccessCount = f.AccessCount,
                LastAccessTime = f.LastAccessTime,
                ExpirationTime = f.ExpirationTime,
                IsPublic = f.IsPublic,
                CreatedBy = f.CreatedBy,
                UpdatedBy = f.UpdatedBy,
                CreatedTime = f.CreatedAt,
                UpdatedTime = f.UpdatedAt ?? DateTime.UtcNow,
                // 图片特有属性
                Width = f.ImageMetadata?.Width ?? 0,
                Height = f.ImageMetadata?.Height ?? 0,
                ColorDepth = f.ImageMetadata?.ColorDepth ?? 0,
                Format = f.ImageMetadata?.Format ?? string.Empty,
                HasAlpha = f.ImageMetadata?.HasAlpha ?? false,
                IsAnimated = f.ImageMetadata?.IsAnimated ?? false,
                FrameCount = f.ImageMetadata?.FrameCount ?? 0,
                DpiX = f.ImageMetadata?.DpiX ?? 0,
                DpiY = f.ImageMetadata?.DpiY ?? 0,
                CameraModel = f.ImageMetadata?.CameraModel,
                DateTaken = f.ImageMetadata?.DateTaken,
                GpsLocation = f.ImageMetadata != null ? 
                    new ValueTuple<double?, double?>(f.ImageMetadata.Latitude, f.ImageMetadata.Longitude) : 
                    new ValueTuple<double?, double?>(null, null)
            }).ToList();

            var result = new PageList<ImageEntity>(imageEntities, totalCount);

            _logger.LogDebug("图片查询完成: 总数={TotalCount}, 当前页={PageNumber}, 页大小={PageSize}", 
                totalCount, pageNumber, pageSize);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询图片列表失败");
            throw;
        }
    }

    /// <summary>
    /// 提取图片元数据
    /// </summary>
    /// <param name="stream">图片流</param>
    /// <returns>图片元数据</returns>
    public async Task<Abstractions.ImageMetadata> ExtractImageMetadataAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _logger.LogDebug("开始提取图片元数据");

        try
        {
            var originalPosition = stream.Position;
            
            // 使用ImageSharp读取基本信息
            using var image = await Image.LoadAsync(stream);
            stream.Position = originalPosition;

            var metadata = new Abstractions.ImageMetadata
            {
                Width = image.Width,
                Height = image.Height,
                Format = GetImageFormatName(image.Metadata.DecodedImageFormat),
                HasAlpha = HasAlphaChannel(image),
                ColorDepth = GetColorDepth(image),
                Dpi = (image.Metadata.HorizontalResolution, image.Metadata.VerticalResolution),
                ExifData = new Dictionary<string, object>()
            };

            // 使用MetadataExtractor提取EXIF数据
            try
            {
                stream.Position = originalPosition;
                var directories = ImageMetadataReader.ReadMetadata(stream);
                
                foreach (var directory in directories)
                {
                    if (directory is ExifIfd0Directory exifIfd0)
                    {
                        // 提取拍摄时间
                        if (exifIfd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
                        {
                            metadata.DateTaken = dateTime;
                        }
                        
                        // 提取相机型号
                        if (exifIfd0.HasTagName(ExifDirectoryBase.TagModel))
                        {
                            var model = exifIfd0.GetDescription(ExifDirectoryBase.TagModel);
                            if (!string.IsNullOrEmpty(model))
                            {
                                metadata.ExifData["CameraModel"] = model;
                            }
                        }
                        
                        // 提取制造商
                        if (exifIfd0.HasTagName(ExifDirectoryBase.TagMake))
                        {
                            var make = exifIfd0.GetDescription(ExifDirectoryBase.TagMake);
                            if (!string.IsNullOrEmpty(make))
                            {
                                metadata.ExifData["CameraMake"] = make;
                            }
                        }
                    }
                    
                    if (directory is ExifSubIfdDirectory exifSubIfd)
                    {
                        // 提取更多EXIF信息
                        ExtractExifSubIfdData(exifSubIfd, metadata.ExifData);
                    }
                    
                    if (directory is MetadataExtractor.Formats.Exif.GpsDirectory gps)
                    {
                        // 提取GPS信息
                        var location = ExtractGpsLocation(gps);
                        if (location.HasValue)
                        {
                            metadata.GpsLocation = location.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取EXIF数据时出现警告，继续处理基本元数据");
            }

            stream.Position = originalPosition;
            
            _logger.LogDebug("图片元数据提取完成: {Width}x{Height}, 格式: {Format}", 
                metadata.Width, metadata.Height, metadata.Format);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取图片元数据失败");
            throw;
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 应用图片操作
    /// </summary>
    private static void ApplyImageOperation(Image image, ImageOperation operation)
    {
        switch (operation)
        {
            case ResizeOperation resize:
                ApplyResizeOperation(image, resize);
                break;
            case CropOperation crop:
                ApplyCropOperation(image, crop);
                break;
            case RotateOperation rotate:
                ApplyRotateOperation(image, rotate);
                break;
            default:
                throw new NotSupportedException($"不支持的图片操作类型: {operation.GetType().Name}");
        }
    }

    /// <summary>
    /// 应用缩放操作
    /// </summary>
    private static void ApplyResizeOperation(Image image, ResizeOperation resize)
    {
        var options = new ResizeOptions
        {
            Size = new Size(resize.Width, resize.Height),
            Mode = resize.KeepAspectRatio ? ResizeMode.Max : ResizeMode.Stretch
        };
        
        image.Mutate(x => x.Resize(options));
    }

    /// <summary>
    /// 应用裁剪操作
    /// </summary>
    private static void ApplyCropOperation(Image image, CropOperation crop)
    {
        var rectangle = new Rectangle(crop.X, crop.Y, crop.Width, crop.Height);
        image.Mutate(x => x.Crop(rectangle));
    }

    /// <summary>
    /// 应用旋转操作
    /// </summary>
    private static void ApplyRotateOperation(Image image, RotateOperation rotate)
    {
        image.Mutate(x => x.Rotate(rotate.Degrees));
    }

    /// <summary>
    /// 获取图片格式
    /// </summary>
    private static IImageFormat GetImageFormat(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => JpegFormat.Instance,
            "image/png" => PngFormat.Instance,
            "image/gif" => GifFormat.Instance,
            "image/webp" => WebpFormat.Instance,
            _ => JpegFormat.Instance // 默认使用JPEG
        };
    }

    /// <summary>
    /// 获取图片格式名称
    /// </summary>
    private static string GetImageFormatName(IImageFormat? format)
    {
        if (format == null) return "Unknown";
        
        return format.Name.ToUpperInvariant();
    }

    /// <summary>
    /// 检查是否有透明通道
    /// </summary>
    private static bool HasAlphaChannel(Image image)
    {
        return image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None;
    }

    /// <summary>
    /// 获取颜色深度
    /// </summary>
    private static int GetColorDepth(Image image)
    {
        return image.PixelType.BitsPerPixel;
    }

    /// <summary>
    /// 判断是否为动画格式
    /// </summary>
    private static bool IsAnimatedFormat(string format)
    {
        return format.Equals("GIF", StringComparison.OrdinalIgnoreCase) ||
               format.Equals("WEBP", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取动画帧数
    /// </summary>
    private int GetFrameCount(Stream stream)
    {
        try
        {
            var originalPosition = stream.Position;
            using var image = Image.Load(stream);
            stream.Position = originalPosition;
            return image.Frames.Count;
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// 提取相机型号
    /// </summary>
    private static string? ExtractCameraModel(Dictionary<string, object> exifData)
    {
        if (exifData.TryGetValue("CameraModel", out var model))
        {
            return model.ToString();
        }
        return null;
    }

    /// <summary>
    /// 提取EXIF子目录数据
    /// </summary>
    private static void ExtractExifSubIfdData(ExifSubIfdDirectory exifSubIfd, Dictionary<string, object> exifData)
    {
        try
        {
            // ISO感光度
            if (exifSubIfd.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out var iso))
            {
                exifData["ISO"] = iso;
            }
            
            // 快门速度
            if (exifSubIfd.HasTagName(ExifDirectoryBase.TagShutterSpeed))
            {
                var shutterSpeed = exifSubIfd.GetDescription(ExifDirectoryBase.TagShutterSpeed);
                if (!string.IsNullOrEmpty(shutterSpeed))
                {
                    exifData["ShutterSpeed"] = shutterSpeed;
                }
            }
            
            // 光圈值
            if (exifSubIfd.HasTagName(ExifDirectoryBase.TagAperture))
            {
                var aperture = exifSubIfd.GetDescription(ExifDirectoryBase.TagAperture);
                if (!string.IsNullOrEmpty(aperture))
                {
                    exifData["Aperture"] = aperture;
                }
            }
            
            // 焦距
            if (exifSubIfd.HasTagName(ExifDirectoryBase.TagFocalLength))
            {
                var focalLength = exifSubIfd.GetDescription(ExifDirectoryBase.TagFocalLength);
                if (!string.IsNullOrEmpty(focalLength))
                {
                    exifData["FocalLength"] = focalLength;
                }
            }
        }
        catch (Exception)
        {
            // 忽略EXIF提取错误
        }
    }

    /// <summary>
    /// 提取GPS位置信息
    /// </summary>
    private static (double? Latitude, double? Longitude)? ExtractGpsLocation(MetadataExtractor.Formats.Exif.GpsDirectory gps)
    {
        try
        {
            var location = gps.GetGeoLocation();
            if (location != null && !location.IsZero)
            {
                return (location.Latitude, location.Longitude);
            }
        }
        catch (Exception)
        {
            // 忽略GPS提取错误
        }
        
        return null;
    }

    #endregion
}
