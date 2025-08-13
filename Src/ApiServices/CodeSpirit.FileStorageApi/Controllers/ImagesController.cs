using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers;

/// <summary>
/// 租户平台图片管理控制器
/// </summary>
[DisplayName("图片管理")]
[Navigation(Icon = "fa-solid fa-image", PlatformType = PlatformType.Tenant)]
public class ImagesController : ApiControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<ImagesController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileStorageService">文件存储服务</param>
    /// <param name="imageProcessingService">图片处理服务</param>
    /// <param name="logger">日志服务</param>
    public ImagesController(
        IFileStorageService fileStorageService,
        IImageProcessingService imageProcessingService,
        ILogger<ImagesController> logger)
    {
        _fileStorageService = fileStorageService;
        _imageProcessingService = imageProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取图片列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片分页列表</returns>
    [HttpGet]
    [DisplayName("获取图片列表")]
    public async Task<ActionResult<ApiResponse<PageList<ImageDto>>>> GetImages([FromQuery] ImageQueryDto queryDto)
    {
        // 使用图片处理服务的专用查询方法，支持图片元数据查询
        PageList<ImageEntity> images = await _imageProcessingService.QueryImagesAsync(queryDto);
        
        // 转换为ImageDto
        var imageDtos = images.Items.Select(img => new ImageDto
        {
            Id = img.Id,
            BucketName = img.BucketName,
            OriginalFileName = img.OriginalFileName,
            Size = img.Size,
            ContentType = img.ContentType,
            Extension = Path.GetExtension(img.OriginalFileName),
            Category = FileTypeCategory.Image,
            Status = img.Status,
            Description = img.Description,
            AccessCount = img.AccessCount,
            LastAccessTime = img.LastAccessTime,
            ExpirationTime = img.ExpirationTime,
            IsPublic = img.IsPublic,
            DownloadUrl = img.DownloadUrl,
            Tags = img.Tags,
            CreatedTime = img.CreatedTime,
            CreatedBy = img.CreatedBy.ToString(),
            UpdatedTime = img.UpdatedTime,
            UpdatedBy = img.UpdatedBy?.ToString(),
            // 图片特有属性
            Width = img.Width,
            Height = img.Height,
            ColorDepth = img.ColorDepth,
            Format = img.Format,
            HasAlpha = img.HasAlpha,
            IsAnimated = img.IsAnimated,
            FrameCount = img.FrameCount,
            DpiX = img.DpiX,
            DpiY = img.DpiY,
            CameraModel = img.CameraModel,
            DateTaken = img.DateTaken,
            Latitude = img.GpsLocation.Item1,
            Longitude = img.GpsLocation.Item2,
        }).ToList();

        var result = new PageList<ImageDto>(imageDtos, images.Total);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 导出图片列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片数据</returns>
    [HttpGet("export")]
    [DisplayName("导出图片列表")]
    public async Task<ActionResult<ApiResponse<PageList<ImageDto>>>> ExportImages([FromQuery] ImageQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 5000; // 图片导出数量限制相对较小
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 重用获取图片列表的逻辑
        var result = await GetImages(queryDto);
        var images = result.Value?.Data;

        // 如果数据为空则返回错误信息
        return images?.Items?.Count == 0 ? BadResponse<PageList<ImageDto>>("没有数据可供导出") : result;
    }

    /// <summary>
    /// 获取图片详情
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取图片详情")]
    public async Task<ActionResult<ApiResponse<ImageDto>>> GetImageDetail(long id)
    {
        // 使用图片处理服务获取完整的图片信息
        ImageEntity? imageEntity = await _imageProcessingService.GetImageInfoAsync(id);
        if (imageEntity == null)
        {
            return BadResponse<ImageDto>("图片不存在");
        }

        var imageDto = new ImageDto
        {
            Id = imageEntity.Id,
            BucketName = imageEntity.BucketName,
            OriginalFileName = imageEntity.OriginalFileName,
            Size = imageEntity.Size,
            ContentType = imageEntity.ContentType,
            Extension = Path.GetExtension(imageEntity.OriginalFileName),
            Category = FileTypeCategory.Image,
            Status = imageEntity.Status,
            Description = "", // 需要从文件表获取
            AccessCount = 0, // 需要从文件表获取
            LastAccessTime = null, // 需要从文件表获取
            ExpirationTime = null, // 需要从文件表获取
            IsPublic = false, // 需要从文件表获取
            DownloadUrl = imageEntity.DownloadUrl,
            Tags = imageEntity.Tags,
            CreatedTime = imageEntity.CreatedTime,
            CreatedBy = "", // 需要从文件表获取
            UpdatedTime = imageEntity.UpdatedTime,
            UpdatedBy = "", // 需要从文件表获取
            // 图片特有属性
            Width = imageEntity.Width,
            Height = imageEntity.Height,
            ColorDepth = 0, // 需要从图片元数据获取
            Format = imageEntity.Format,
            HasAlpha = imageEntity.HasAlpha,
            IsAnimated = imageEntity.IsAnimated,
            FrameCount = 0, // 需要从图片元数据获取
            DpiX = 0, // 需要从图片元数据获取
            DpiY = 0, // 需要从图片元数据获取
            CameraModel = "", // 需要从图片元数据获取
            DateTaken = imageEntity.DateTaken,
            Latitude = imageEntity.GpsLocation.Latitude,
            Longitude = imageEntity.GpsLocation.Longitude,
        };

        return SuccessResponse(imageDto);
    }

    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <param name="createDto">上传配置</param>
    /// <returns>图片信息</returns>
    [HttpPost("upload")]
    [DisplayName("上传图片")]
    public async Task<ActionResult<ApiResponse<AmisImageDto>>> UploadImage(
        IFormFile file,
        [FromQuery] CreateImageDto createDto)
    {
        if (file == null || file.Length == 0)
        {
            return BadResponse<AmisImageDto>("图片文件不能为空");
        }

        // 验证是否为图片文件
        if (!IsImageFile(file.ContentType))
        {
            return BadResponse<AmisImageDto>("只能上传图片文件");
        }

        try
        {
            // 使用图片处理服务上传并自动提取元数据
            var imageUploadRequest = new ImageUploadRequest
            {
                BucketName = createDto.BucketName ?? "default",
                FileName = file.FileName,
                FileStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                Description = createDto.Description,
                ExpirationTime = createDto.ExpirationTime,
                IsPublic = createDto.IsPublic,
                OverwriteExisting = createDto.OverwriteExisting,
                Tags = createDto.Tags != null ? 
                    createDto.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .ToDictionary(tag => tag.Trim(), tag => tag.Trim()) : null,
                Quality = createDto.Quality,
                ExtractExifData = createDto.ExtractExifData
            };

            // 调用图片处理服务进行上传和元数据提取
            ImageEntity imageEntity = await _imageProcessingService.UploadImageAsync(imageUploadRequest);
            
            var amisImageDto = new AmisImageDto
            {
                Id = imageEntity.Id,
                Value = imageEntity.DownloadUrl,
                Url = imageEntity.DownloadUrl ?? string.Empty,
                Name = imageEntity.OriginalFileName ?? string.Empty,
                Size = imageEntity.Size,
                Type = imageEntity.ContentType ?? string.Empty,
                Width = imageEntity.Width,
                Height = imageEntity.Height,
                IsImage = true,
                UploadTime = imageEntity.CreatedTime
            };

            _logger.LogInformation("图片上传成功: {FileName}, ID: {ImageId}, 尺寸: {Width}x{Height}", 
                file.FileName, imageEntity.Id, imageEntity.Width, imageEntity.Height);

            return SuccessResponseWithCreate<AmisImageDto>(nameof(GetImageDetail), amisImageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片上传失败: {FileName}", file.FileName);
            return BadResponse<AmisImageDto>($"图片上传失败: {ex.Message}");
        }
    }



    /// <summary>
    /// 处理图片
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="processDto">处理配置</param>
    /// <returns>处理结果</returns>
    [HttpPost("{id}/process")]
    [Operation("处理图片", "form", null, "确定要处理图片吗？")]
    [DisplayName("处理图片")]
    public async Task<ActionResult<ApiResponse<ImageDto>>> ProcessImage(
        long id, 
        [FromBody] ImageProcessDto processDto)
    {
        FileEntity? file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null || file.Category != FileTypeCategory.Image)
        {
            return BadResponse<ImageDto>("图片不存在");
        }

        try
        {
            // 构建图片操作列表
            var operations = new List<ImageOperation>();
            
            // 如果指定了目标尺寸，添加缩放操作
            if (processDto.TargetWidth.HasValue || processDto.TargetHeight.HasValue)
            {
                var resizeOp = new ResizeOperation
                {
                    Width = processDto.TargetWidth ?? 0,
                    Height = processDto.TargetHeight ?? 0,
                    KeepAspectRatio = processDto.KeepAspectRatio
                };
                operations.Add(resizeOp);
            }
            
            // 如果没有任何操作，返回错误
            if (operations.Count == 0)
            {
                return BadResponse<ImageDto>("请指定至少一种图片处理操作");
            }
            
            // 调用图片处理服务
            ProcessedImageResult processedResult = await _imageProcessingService.ProcessImageAsync(id, operations);
            
            // 这里可以选择保存处理后的图片或直接返回处理结果
            // 为了演示，我们先返回原图片信息，表示处理成功
            var imageResult = await GetImageDetail(id);
            if (imageResult.Value?.Status == 0)
            {
                _logger.LogInformation("图片处理成功: {ImageId}, 输出尺寸: {Width}x{Height}, 大小: {Size}字节", 
                    id, processedResult.Width, processedResult.Height, processedResult.Size);
                
                return SuccessResponse(imageResult.Value.Data, "图片处理成功");
            }
            
            return BadResponse<ImageDto>("图片处理失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片处理失败: {ImageId}", id);
            return BadResponse<ImageDto>($"图片处理失败: {ex.Message}");
        }
    }



    /// <summary>
    /// 批量删除图片
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的图片吗？", isBulkOperation: true)]
    [DisplayName("批量删除图片")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteImages([FromBody] BatchOperationDto<long> request)
    {
        BatchOperationResult result = await _fileStorageService.BatchDeleteFilesAsync(request.Ids);

        if (result.Failed > 0)
        {
            string message = $"批量删除完成！成功删除 {result.Success} 张图片，失败 {result.Failed} 张";
            return SuccessResponse(message);
        }

        return SuccessResponse($"批量删除成功！共删除 {result.Success} 张图片");
    }



    /// <summary>
    /// 获取图片统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取图片统计")]
    public Task<ActionResult<ApiResponse<object>>> GetImageStatistics()
    {
        // 这里需要实现图片统计功能
        var statistics = new
        {
            TotalImages = 0,
            TotalSize = 0L,
            ImagesByFormat = new Dictionary<string, int>(),
            ImagesByResolution = new Dictionary<string, int>(),
            AverageFileSize = 0L,
            LargestImage = new { Width = 0, Height = 0, Size = 0L },

        };

        return Task.FromResult(SuccessResponse<object>(statistics));
    }

    /// <summary>
    /// 验证是否为图片文件
    /// </summary>
    /// <param name="contentType">文件类型</param>
    /// <returns>是否为图片</returns>
    private static bool IsImageFile(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var imageTypes = new[]
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", 
            "image/bmp", "image/webp", "image/svg+xml", "image/tiff"
        };

        return imageTypes.Contains(contentType.ToLowerInvariant());
    }
}


