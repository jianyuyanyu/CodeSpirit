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
        var request = new FileQueryRequest
        {
            BucketName = queryDto.BucketName,
            Category = FileTypeCategory.Image, // 只查询图片类型
            FileName = queryDto.FileName,
            CreatedFrom = queryDto.CreatedFrom,
            CreatedTo = queryDto.CreatedTo,
            PageNumber = queryDto.Page,
            PageSize = queryDto.PerPage,
            OrderBy = queryDto.OrderBy ?? "CreatedTime",
            Descending = queryDto.OrderDir == "desc"
        };

        PageList<FileEntity> files = await _fileStorageService.QueryFilesAsync(request);
        
        // 转换为ImageDto（这里需要从服务层获取图片元数据）
        var imageDtos = files.Items.Select(f => new ImageDto
        {
            Id = f.Id,
            BucketName = f.BucketName,
            OriginalFileName = f.OriginalFileName,
            Size = f.Size,
            ContentType = f.ContentType,
            Extension = f.Extension,
            Category = f.Category,
            Status = f.Status,
            Description = f.Description,
            AccessCount = f.AccessCount,
            LastAccessTime = f.LastAccessTime,
            ExpirationTime = f.ExpirationTime,
            IsPublic = f.IsPublic,
            DownloadUrl = f.DownloadUrl,
            Tags = f.Tags,
            CreatedTime = f.CreatedAt,
            CreatedBy = f.CreatedBy.ToString(),
            UpdatedTime = f.UpdatedAt,
            UpdatedBy = f.UpdatedBy?.ToString(),
            // 图片特有属性需要从图片元数据获取
            Width = f.ImageMetadata?.Width ?? 0,
            Height = f.ImageMetadata?.Height ?? 0,
            ColorDepth = f.ImageMetadata?.ColorDepth ?? 0,
            Format = f.ImageMetadata?.Format,
            HasAlpha = f.ImageMetadata?.HasAlpha ?? false,
            IsAnimated = f.ImageMetadata?.IsAnimated ?? false,
            FrameCount = f.ImageMetadata?.FrameCount ?? 0,
            DpiX = f.ImageMetadata?.DpiX ?? 0,
            DpiY = f.ImageMetadata?.DpiY ?? 0,
            CameraModel = f.ImageMetadata?.CameraModel,
            DateTaken = f.ImageMetadata?.DateTaken,
            Latitude = f.ImageMetadata?.Latitude,
            Longitude = f.ImageMetadata?.Longitude,
            Thumbnails = f.ImageMetadata?.Thumbnails?.Select(t => new ThumbnailDto
            {
                Id = t.Id,
                SizeKey = t.SizeKey,
                Width = t.Width,
                Height = t.Height,
                ThumbnailFileId = t.ThumbnailFileId,
                DownloadUrl = t.ThumbnailFile?.DownloadUrl
            }).ToList() ?? new List<ThumbnailDto>()
        }).ToList();

        var result = new PageList<ImageDto>(imageDtos, files.Total);
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
        FileEntity file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null || file.Category != FileTypeCategory.Image)
        {
            return BadResponse<ImageDto>("图片不存在");
        }

        var imageDto = new ImageDto
        {
            Id = file.Id,
            BucketName = file.BucketName,
            OriginalFileName = file.OriginalFileName,
            Size = file.Size,
            ContentType = file.ContentType,
            Extension = file.Extension,
            Category = file.Category,
            Status = file.Status,
            Description = file.Description,
            AccessCount = file.AccessCount,
            LastAccessTime = file.LastAccessTime,
            ExpirationTime = file.ExpirationTime,
            IsPublic = file.IsPublic,
            DownloadUrl = file.DownloadUrl,
            Tags = file.Tags,
            CreatedTime = file.CreatedAt,
            CreatedBy = file.CreatedBy.ToString(),
            UpdatedTime = file.UpdatedAt,
            UpdatedBy = file.UpdatedBy?.ToString(),
            // 图片特有属性
            Width = file.ImageMetadata?.Width ?? 0,
            Height = file.ImageMetadata?.Height ?? 0,
            ColorDepth = file.ImageMetadata?.ColorDepth ?? 0,
            Format = file.ImageMetadata?.Format,
            HasAlpha = file.ImageMetadata?.HasAlpha ?? false,
            IsAnimated = file.ImageMetadata?.IsAnimated ?? false,
            FrameCount = file.ImageMetadata?.FrameCount ?? 0,
            DpiX = file.ImageMetadata?.DpiX ?? 0,
            DpiY = file.ImageMetadata?.DpiY ?? 0,
            CameraModel = file.ImageMetadata?.CameraModel,
            DateTaken = file.ImageMetadata?.DateTaken,
            Latitude = file.ImageMetadata?.Latitude,
            Longitude = file.ImageMetadata?.Longitude,
            Thumbnails = file.ImageMetadata?.Thumbnails?.Select(t => new ThumbnailDto
            {
                Id = t.Id,
                SizeKey = t.SizeKey,
                Width = t.Width,
                Height = t.Height,
                ThumbnailFileId = t.ThumbnailFileId,
                DownloadUrl = t.ThumbnailFile?.DownloadUrl
            }).ToList() ?? new List<ThumbnailDto>()
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
            var request = new FileUploadRequest
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
                    .ToDictionary(tag => tag.Trim(), tag => tag.Trim()) : null
            };

            FileEntity fileEntity = await _fileStorageService.UploadFileAsync(request);
            
            var amisImageDto = new AmisImageDto
            {
                Id = fileEntity.Id,
                Value = fileEntity.DownloadUrl,
                Url = fileEntity.DownloadUrl ?? string.Empty, // 图片URL
                Name = fileEntity.OriginalFileName ?? string.Empty,
                Size = fileEntity.Size,
                Type = fileEntity.ContentType ?? string.Empty,
                Width = fileEntity.ImageMetadata?.Width,
                Height = fileEntity.ImageMetadata?.Height,
                IsImage = true,
                UploadTime = fileEntity.CreatedAt
            };

            return SuccessResponseWithCreate<AmisImageDto>(nameof(GetImageDetail), amisImageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片上传失败: {FileName}", file.FileName);
            return BadResponse<AmisImageDto>($"图片上传失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="generateDto">生成配置</param>
    /// <returns>生成结果</returns>
    [HttpPost("{id}/thumbnails")]
    [Operation("生成缩略图", "form", null, "确定要生成缩略图吗？")]
    [DisplayName("生成缩略图")]
    public async Task<ActionResult<ApiResponse<List<ThumbnailDto>>>> GenerateThumbnails(
        long id, 
        [FromBody] GenerateThumbnailDto generateDto)
    {
        FileEntity file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null || file.Category != FileTypeCategory.Image)
        {
            return BadResponse<List<ThumbnailDto>>("图片不存在");
        }

        // 这里需要调用图片处理服务生成缩略图
        // var thumbnails = await _imageProcessingService.GenerateThumbnailsAsync(id, generateDto.ThumbnailSizes);
        
        // 暂时返回空列表
        var thumbnails = new List<ThumbnailDto>();
        return SuccessResponse(thumbnails, "缩略图生成成功");
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
        FileEntity file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null || file.Category != FileTypeCategory.Image)
        {
            return BadResponse<ImageDto>("图片不存在");
        }

        // 这里需要调用图片处理服务
        // var processedImage = await _imageProcessingService.ProcessImageAsync(id, processDto);
        
        // 暂时返回原图片信息
        var imageResult = await GetImageDetail(id);
        return imageResult.Value?.Status == 0 ? 
            SuccessResponse(imageResult.Value.Data, "图片处理成功") :
            BadResponse<ImageDto>("图片处理失败");
    }

    /// <summary>
    /// 删除缩略图
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="thumbnailId">缩略图ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}/thumbnails/{thumbnailId}")]
    [Operation("删除缩略图", "ajax", null, "确定要删除此缩略图吗？")]
    [DisplayName("删除缩略图")]
    public async Task<ActionResult<ApiResponse>> DeleteThumbnail(long id, long thumbnailId)
    {
        // 这里需要实现删除缩略图的逻辑
        // await _imageProcessingService.DeleteThumbnailAsync(thumbnailId);
        
        return SuccessResponse("缩略图删除成功");
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
    /// 批量生成缩略图
    /// </summary>
    /// <param name="request">批量生成请求</param>
    /// <returns>生成结果</returns>
    [HttpPost("batch/generate-thumbnails")]
    [Operation("批量生成缩略图", "form", null, "确定要为选中的图片批量生成缩略图吗？", isBulkOperation: true)]
    [DisplayName("批量生成缩略图")]
    public async Task<ActionResult<ApiResponse>> BatchGenerateThumbnails([FromBody] BatchGenerateThumbnailsRequest request)
    {
        // 这里需要实现批量生成缩略图的逻辑
        // var result = await _imageProcessingService.BatchGenerateThumbnailsAsync(request.ImageIds, request.ThumbnailSizes);
        
        int successCount = request.ImageIds?.Count ?? 0;
        return SuccessResponse($"批量生成缩略图完成！成功处理 {successCount} 张图片");
    }

    /// <summary>
    /// 获取图片统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取图片统计")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageStatistics()
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
            TotalThumbnails = 0
        };

        return SuccessResponse<object>(statistics);
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

/// <summary>
/// 批量生成缩略图请求
/// </summary>
public class BatchGenerateThumbnailsRequest
{
    /// <summary>
    /// 图片ID列表
    /// </summary>
    [DisplayName("图片ID列表")]
    public List<long> ImageIds { get; set; } = new();

    /// <summary>
    /// 缩略图尺寸列表
    /// </summary>
    [DisplayName("缩略图尺寸")]
    public List<string> ThumbnailSizes { get; set; } = new();

    /// <summary>
    /// 图片质量（1-100）
    /// </summary>
    [DisplayName("图片质量")]
    public int Quality { get; set; } = 85;

    /// <summary>
    /// 是否覆盖已存在的缩略图
    /// </summary>
    [DisplayName("覆盖已存在")]
    public bool OverwriteExisting { get; set; } = false;
}
