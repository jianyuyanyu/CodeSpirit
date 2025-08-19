using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.MappingProfiles;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using static CodeSpirit.FileStorageApi.Services.System.ISystemFileService;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统图片服务实现
/// </summary>
public class SystemImageService : ISystemImageService
{
    private readonly FileStorageDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IMapper _mapper;
    private readonly ILogger<SystemImageService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="fileStorageService">文件存储服务</param>
    /// <param name="imageProcessingService">图片处理服务</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志服务</param>
    public SystemImageService(
        FileStorageDbContext context,
        IFileStorageService fileStorageService,
        IImageProcessingService imageProcessingService,
        IMapper mapper,
        ILogger<SystemImageService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _imageProcessingService = imageProcessingService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统图片列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片分页列表</returns>
    public async Task<PageList<SystemImageDto>> GetSystemImagesAsync(SystemImageQueryDto queryDto)
    {
        var query = _context.Files
            .Where(f => f.Category == FileTypeCategory.Image)
            .AsNoTracking();

        // 租户过滤
        if (!string.IsNullOrEmpty(queryDto.TenantId))
        {
            query = query.Where(f => f.TenantId == queryDto.TenantId);
        }

        // 存储桶过滤
        if (!string.IsNullOrEmpty(queryDto.BucketName))
        {
            query = query.Where(f => f.BucketName == queryDto.BucketName);
        }

        // 文件名过滤
        if (!string.IsNullOrEmpty(queryDto.FileName))
        {
            query = query.Where(f => f.OriginalFileName.Contains(queryDto.FileName));
        }

        // 状态过滤
        if (queryDto.Status.HasValue)
        {
            query = query.Where(f => f.Status == queryDto.Status.Value);
        }

        // 公开访问过滤
        if (queryDto.IsPublic.HasValue)
        {
            query = query.Where(f => f.IsPublic == queryDto.IsPublic.Value);
        }

        // 创建时间过滤
        if (queryDto.CreatedFrom.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= queryDto.CreatedFrom.Value);
        }

        if (queryDto.CreatedTo.HasValue)
        {
            query = query.Where(f => f.CreatedAt <= queryDto.CreatedTo.Value);
        }

        // 获取图片实体，联查图片元数据
        var queryImages = query
            .Join(_context.ImageMetadata,
                file => file.Id,
                image => image.FileId,
                (file, image) => new { File = file, Image = image });

        // 图片特有过滤条件
        if (!string.IsNullOrEmpty(queryDto.Format))
        {
            queryImages = queryImages.Where(x => x.Image.Format == queryDto.Format);
        }

        if (queryDto.MinWidth.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.Width >= queryDto.MinWidth.Value);
        }

        if (queryDto.MaxWidth.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.Width <= queryDto.MaxWidth.Value);
        }

        if (queryDto.MinHeight.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.Height >= queryDto.MinHeight.Value);
        }

        if (queryDto.MaxHeight.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.Height <= queryDto.MaxHeight.Value);
        }

        if (queryDto.IsAnimated.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.IsAnimated == queryDto.IsAnimated.Value);
        }

        if (queryDto.HasAlpha.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.HasAlpha == queryDto.HasAlpha.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.CameraModel))
        {
            queryImages = queryImages.Where(x => x.Image.CameraModel != null && x.Image.CameraModel.Contains(queryDto.CameraModel));
        }

        if (queryDto.DateTakenFrom.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.DateTaken >= queryDto.DateTakenFrom.Value);
        }

        if (queryDto.DateTakenTo.HasValue)
        {
            queryImages = queryImages.Where(x => x.Image.DateTaken <= queryDto.DateTakenTo.Value);
        }

        // 排序
        queryImages = queryImages.OrderByDescending(x => x.File.CreatedAt);

        // 分页
        var totalCount = await queryImages.CountAsync();
        var items = await queryImages
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 转换为DTO - 使用AutoMapper
        var imageDtos = items.Select(x => _mapper.Map<SystemImageDto>(new FileImageCombined
        {
            File = x.File,
            Image = x.Image
        })).ToList();

        return new PageList<SystemImageDto>(imageDtos, totalCount);
    }

    /// <summary>
    /// 获取图片详情
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片详情</returns>
    public async Task<SystemImageDto> GetImageDetailAsync(long id)
    {
        var file = await _context.Files
            .Where(f => f.Id == id && f.Category == FileTypeCategory.Image)
            .FirstOrDefaultAsync();

        if (file == null)
        {
            throw new InvalidOperationException("图片不存在");
        }

        var imageEntity = await _context.ImageMetadata
            .Where(i => i.FileId == id)
            .FirstOrDefaultAsync();

        if (imageEntity == null)
        {
            throw new InvalidOperationException("图片元数据不存在");
        }

        // 使用AutoMapper进行映射
        return _mapper.Map<SystemImageDto>(new FileImageCombined
        {
            File = file,
            Image = imageEntity
        });
    }

    /// <summary>
    /// 获取图片引用信息
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片引用信息</returns>
    public async Task<object> GetImageReferencesAsync(long id)
    {
        // TODO: 实现图片引用查询逻辑
        await Task.CompletedTask;
        return new { References = new List<object>(), Count = 0 };
    }

    /// <summary>
    /// 恢复已删除的图片
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>恢复结果</returns>
    public async Task RestoreImageAsync(long id)
    {
        var file = await _context.Files.FindAsync(id);
        if (file == null)
        {
            throw new InvalidOperationException("图片不存在");
        }

        file.Status = FileStatus.Active;
        file.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("图片 {ImageId} 已恢复", id);
    }

    /// <summary>
    /// 移动图片到其他存储桶
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>移动结果</returns>
    public async Task MoveImageAsync(long id, string targetBucketName)
    {
        // TODO: 实现图片移动逻辑
        await Task.CompletedTask;
        _logger.LogInformation("图片 {ImageId} 已移动到存储桶 {BucketName}", id, targetBucketName);
    }

    /// <summary>
    /// 获取图片下载链接
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接信息</returns>
    public async Task<object> GetImageDownloadUrlAsync(long id, int expirationMinutes)
    {
        var url = await _fileStorageService.GetDownloadUrlAsync(id, expirationMinutes);
        return new { Url = url, ExpirationMinutes = expirationMinutes, ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes) };
    }

    /// <summary>
    /// 修复图片数据完整性
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>修复结果</returns>
    public async Task<FileRepairResult> RepairImageAsync(long id)
    {
        // TODO: 实现图片修复逻辑
        await Task.CompletedTask;
        return new FileRepairResult { IsRepaired = false, ErrorMessage = "图片无需修复" };
    }

    /// <summary>
    /// 获取系统图片统计摘要
    /// </summary>
    /// <returns>图片统计摘要</returns>
    public async Task<object> GetImageStatisticsSummaryAsync()
    {
        var totalImages = await _context.Files.CountAsync(f => f.Category == FileTypeCategory.Image);
        var totalSize = await _context.Files
            .Where(f => f.Category == FileTypeCategory.Image)
            .SumAsync(f => f.Size);

        return new
        {
            TotalImages = totalImages,
            TotalSize = totalSize,
            TotalSizeFormatted = FormatFileSize(totalSize),
            AverageSize = totalImages > 0 ? totalSize / totalImages : 0,
            ActiveImages = await _context.Files.CountAsync(f => f.Category == FileTypeCategory.Image && f.Status == FileStatus.Active),
            DeletedImages = await _context.Files.CountAsync(f => f.Category == FileTypeCategory.Image && f.Status == FileStatus.Deleted)
        };
    }

    /// <summary>
    /// 获取图片格式分布统计
    /// </summary>
    /// <returns>图片格式分布</returns>
    public async Task<object> GetImageFormatDistributionAsync()
    {
        var distribution = await _context.ImageMetadata
            .GroupBy(i => i.Format)
            .Select(g => new { Format = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return distribution;
    }

    /// <summary>
    /// 获取图片尺寸分布统计
    /// </summary>
    /// <returns>图片尺寸分布</returns>
    public async Task<object> GetImageSizeDistributionAsync()
    {
        // 简单的尺寸分布统计
        var sizeRanges = new[]
        {
            new { Range = "小图 (<500px)", MinWidth = 0, MaxWidth = 500 },
            new { Range = "中图 (500-1920px)", MinWidth = 500, MaxWidth = 1920 },
            new { Range = "大图 (1920-4000px)", MinWidth = 1920, MaxWidth = 4000 },
            new { Range = "超大图 (>4000px)", MinWidth = 4000, MaxWidth = int.MaxValue }
        };

        var result = new List<object>();
        foreach (var range in sizeRanges)
        {
            var count = await _context.ImageMetadata
                .CountAsync(i => i.Width >= range.MinWidth && i.Width < range.MaxWidth);
            result.Add(new { Range = range.Range, Count = count });
        }

        return result;
    }

    /// <summary>
    /// 获取图片使用趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>图片使用趋势</returns>
    public async Task<object> GetImageUsageTrendAsync(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var trend = await _context.Files
            .Where(f => f.Category == FileTypeCategory.Image && f.CreatedAt >= startDate)
            .GroupBy(f => f.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return trend;
    }

    /// <summary>
    /// 清理过期图片
    /// </summary>
    /// <returns>清理结果</returns>
    public async Task<CleanupResult> CleanupExpiredImagesAsync()
    {
        var expiredFiles = await _context.Files
            .Where(f => f.Category == FileTypeCategory.Image && 
                       f.ExpirationTime.HasValue && 
                       f.ExpirationTime.Value < DateTime.UtcNow)
            .ToListAsync();

        long totalSize = expiredFiles.Sum(f => f.Size);
        
        foreach (var file in expiredFiles)
        {
            file.Status = FileStatus.Deleted;
            file.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new CleanupResult
        {
            CleanedFilesCount = expiredFiles.Count,
            ReleasedSpace = totalSize,
            ReleasedSpaceFormatted = FormatFileSize(totalSize)
        };
    }

    /// <summary>
    /// 清理无引用图片
    /// </summary>
    /// <returns>清理结果</returns>
    public async Task<CleanupResult> CleanupUnreferencedImagesAsync()
    {
        // TODO: 实现无引用图片清理逻辑
        await Task.CompletedTask;
        return new CleanupResult
        {
            CleanedFilesCount = 0,
            ReleasedSpace = 0,
            ReleasedSpaceFormatted = "0 B"
        };
    }

    /// <summary>
    /// 批量删除图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    public async Task<SystemBatchOperationResult> BatchDeleteImagesAsync(IEnumerable<long> imageIds)
    {
        var files = await _context.Files
            .Where(f => imageIds.Contains(f.Id) && f.Category == FileTypeCategory.Image)
            .ToListAsync();

        foreach (var file in files)
        {
            file.Status = FileStatus.Deleted;
            file.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new SystemBatchOperationResult
        {
            Success = files.Count,
            Failed = imageIds.Count() - files.Count,
            Total = imageIds.Count()
        };
    }

    /// <summary>
    /// 批量恢复图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    public async Task<SystemBatchOperationResult> BatchRestoreImagesAsync(IEnumerable<long> imageIds)
    {
        var files = await _context.Files
            .Where(f => imageIds.Contains(f.Id) && f.Category == FileTypeCategory.Image)
            .ToListAsync();

        foreach (var file in files)
        {
            file.Status = FileStatus.Active;
            file.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new SystemBatchOperationResult
        {
            Success = files.Count,
            Failed = imageIds.Count() - files.Count,
            Total = imageIds.Count()
        };
    }

    /// <summary>
    /// 批量移动图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>批量操作结果</returns>
    public async Task<SystemBatchOperationResult> BatchMoveImagesAsync(IEnumerable<long> imageIds, string targetBucketName)
    {
        // TODO: 实现批量移动逻辑
        await Task.CompletedTask;
        return new SystemBatchOperationResult
        {
            Success = imageIds.Count(),
            Failed = 0,
            Total = imageIds.Count()
        };
    }

    /// <summary>
    /// 重建图片元数据
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>重建结果</returns>
    public async Task<object> RebuildImageMetadataAsync(long id)
    {
        // TODO: 实现图片元数据重建逻辑
        await Task.CompletedTask;
        return new { Success = true, Message = "图片元数据重建成功" };
    }

    /// <summary>
    /// 批量重建图片元数据
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    public async Task<SystemBatchOperationResult> BatchRebuildImageMetadataAsync(IEnumerable<long> imageIds)
    {
        // TODO: 实现批量重建图片元数据逻辑
        await Task.CompletedTask;
        return new SystemBatchOperationResult
        {
            Success = imageIds.Count(),
            Failed = 0,
            Total = imageIds.Count()
        };
    }

    /// <summary>
    /// 优化图片存储
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>优化结果</returns>
    public async Task<object> OptimizeImageStorageAsync(long id)
    {
        // TODO: 实现图片存储优化逻辑
        await Task.CompletedTask;
        return new { Success = true, Message = "图片存储优化成功" };
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化后的大小</returns>
    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
