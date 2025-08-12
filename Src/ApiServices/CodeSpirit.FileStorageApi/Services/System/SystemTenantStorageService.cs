using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统租户存储服务实现
/// </summary>
public class SystemTenantStorageService : ISystemTenantStorageService
{
    private readonly FileStorageDbContext _context;
    private readonly ILogger<SystemTenantStorageService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemTenantStorageService(
        FileStorageDbContext context,
        ILogger<SystemTenantStorageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取租户存储统计列表
    /// </summary>
    public async Task<PageList<TenantStorageStatisticsDto>> GetTenantStorageStatisticsAsync(QueryDtoBase queryDto)
    {
        try
        {
            // 按租户分组统计文件信息
            var query = _context.Files
                .Where(f => f.Status == FileStatus.Active)
                .GroupBy(f => f.TenantId)
                .Select(g => new TenantStorageStatisticsDto
                {
                    TenantId = g.Key,
                    TenantName = g.Key, // 这里应该关联租户表获取真实名称
                    TenantDisplayName = g.Key,
                    TotalFiles = g.Count(),
                    TotalStorageSize = g.Sum(f => f.Size),
                    TotalStorageSizeFormatted = FormatFileSize(g.Sum(f => f.Size)),
                    ImageFilesCount = g.Count(f => f.Category == FileTypeCategory.Image),
                    VideoFilesCount = g.Count(f => f.Category == FileTypeCategory.Video),
                    DocumentFilesCount = g.Count(f => f.Category == FileTypeCategory.Document),
                    OtherFilesCount = g.Count(f => f.Category == FileTypeCategory.Other),
                    LastUploadTime = g.Max(f => f.CreatedAt),
                    CreatedTime = g.Min(f => f.CreatedAt),
                    Status = "Active"
                });

            // 应用排序
            if (!string.IsNullOrEmpty(queryDto.OrderBy))
            {
                query = queryDto.OrderDir?.ToLower() == "desc"
                    ? query.OrderByDescending(x => EF.Property<object>(x, queryDto.OrderBy))
                    : query.OrderBy(x => EF.Property<object>(x, queryDto.OrderBy));
            }
            else
            {
                query = query.OrderByDescending(x => x.TotalStorageSize);
            }

            // 分页
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .ToListAsync();

            // 计算使用率等额外信息
            foreach (var item in items)
            {
                // 这里可以添加存储配额查询逻辑
                item.StorageQuota = null; // 从配置或数据库获取
                item.StorageQuotaFormatted = item.StorageQuota?.ToString() ?? "无限制";
                item.UsagePercentage = item.StorageQuota.HasValue && item.StorageQuota > 0
                    ? (decimal)item.TotalStorageSize / item.StorageQuota.Value * 100
                    : 0;
                
                // 统计活跃存储桶数量
                item.ActiveBucketsCount = await _context.Files
                    .Where(f => f.TenantId == item.TenantId && f.Status == FileStatus.Active)
                    .Select(f => f.BucketName)
                    .Distinct()
                    .CountAsync();
            }

            return new PageList<TenantStorageStatisticsDto>(items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户存储统计失败");
            throw;
        }
    }

    /// <summary>
    /// 获取租户存储详情
    /// </summary>
    public async Task<TenantStorageStatisticsDto> GetTenantStorageDetailAsync(string tenantId)
    {
        try
        {
            var files = await _context.Files
                .Where(f => f.TenantId == tenantId && f.Status == FileStatus.Active)
                .ToListAsync();

            if (!files.Any())
            {
                return new TenantStorageStatisticsDto
                {
                    TenantId = tenantId,
                    TenantName = tenantId,
                    TenantDisplayName = tenantId,
                    Status = "No Data"
                };
            }

            var result = new TenantStorageStatisticsDto
            {
                TenantId = tenantId,
                TenantName = tenantId,
                TenantDisplayName = tenantId,
                TotalFiles = files.Count,
                TotalStorageSize = files.Sum(f => f.Size),
                TotalStorageSizeFormatted = FormatFileSize(files.Sum(f => f.Size)),
                ImageFilesCount = files.Count(f => f.Category == FileTypeCategory.Image),
                VideoFilesCount = files.Count(f => f.Category == FileTypeCategory.Video),
                DocumentFilesCount = files.Count(f => f.Category == FileTypeCategory.Document),
                OtherFilesCount = files.Count(f => f.Category == FileTypeCategory.Other),
                LastUploadTime = files.Max(f => f.CreatedAt),
                CreatedTime = files.Min(f => f.CreatedAt),
                Status = "Active",
                ActiveBucketsCount = files.Select(f => f.BucketName).Distinct().Count()
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户存储详情失败: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// 获取系统总存储统计
    /// </summary>
    public async Task<object> GetSystemStorageSummaryAsync()
    {
        try
        {
            var summary = await _context.Files
                .Where(f => f.Status == FileStatus.Active)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFiles = g.Count(),
                    TotalStorageSize = g.Sum(f => f.Size),
                    TotalStorageSizeFormatted = FormatFileSize(g.Sum(f => f.Size)),
                    TotalTenants = g.Select(f => f.TenantId).Distinct().Count(),
                    TotalBuckets = g.Select(f => f.BucketName).Distinct().Count(),
                    ImageFiles = g.Count(f => f.Category == FileTypeCategory.Image),
                    VideoFiles = g.Count(f => f.Category == FileTypeCategory.Video),
                    DocumentFiles = g.Count(f => f.Category == FileTypeCategory.Document),
                    OtherFiles = g.Count(f => f.Category == FileTypeCategory.Other),
                    AverageFileSize = g.Average(f => f.Size),
                    AverageFileSizeFormatted = FormatFileSize((long)g.Average(f => f.Size))
                })
                .FirstOrDefaultAsync();

            return summary ?? new
            {
                TotalFiles = 0,
                TotalStorageSize = 0L,
                TotalStorageSizeFormatted = "0 B",
                TotalTenants = 0,
                TotalBuckets = 0,
                ImageFiles = 0,
                VideoFiles = 0,
                DocumentFiles = 0,
                OtherFiles = 0,
                AverageFileSize = 0.0,
                AverageFileSizeFormatted = "0 B"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统存储摘要失败");
            throw;
        }
    }

    /// <summary>
    /// 清理租户无效文件
    /// </summary>
    public async Task<CleanupResult> CleanupTenantInvalidFilesAsync(string tenantId)
    {
        try
        {
            // 查找无效文件（状态为删除、过期、或无引用的临时文件）
            var invalidFiles = await _context.Files
                .Where(f => f.TenantId == tenantId && 
                           (f.Status == FileStatus.Deleted || 
                            (f.ExpirationTime.HasValue && f.ExpirationTime < DateTime.UtcNow)))
                .ToListAsync();

            var cleanedSize = invalidFiles.Sum(f => f.Size);
            var cleanedCount = invalidFiles.Count;

            // 删除文件记录
            _context.Files.RemoveRange(invalidFiles);
            await _context.SaveChangesAsync();

            return new CleanupResult
            {
                CleanedFilesCount = cleanedCount,
                ReleasedSpace = cleanedSize,
                ReleasedSpaceFormatted = FormatFileSize(cleanedSize),
                Details = new List<string>
                {
                    $"删除了 {cleanedCount} 个无效文件",
                    $"释放存储空间 {FormatFileSize(cleanedSize)}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理租户无效文件失败: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// 刷新租户存储统计
    /// </summary>
    public async Task RefreshTenantStorageStatisticsAsync(string tenantId)
    {
        try
        {
            // 这里可以实现统计缓存的刷新逻辑
            _logger.LogInformation("刷新租户存储统计: {TenantId}", tenantId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新租户存储统计失败: {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// 批量清理无效文件
    /// </summary>
    public async Task<BatchCleanupResult> BatchCleanupInvalidFilesAsync(IEnumerable<string> tenantIds)
    {
        var result = new BatchCleanupResult
        {
            ProcessedTenantsCount = tenantIds.Count(),
            TenantDetails = new List<TenantCleanupDetail>()
        };

        foreach (var tenantId in tenantIds)
        {
            try
            {
                var cleanupResult = await CleanupTenantInvalidFilesAsync(tenantId);
                
                result.TotalCleanedFilesCount += cleanupResult.CleanedFilesCount;
                result.TotalReleasedSpace += cleanupResult.ReleasedSpace;
                
                result.TenantDetails.Add(new TenantCleanupDetail
                {
                    TenantId = tenantId,
                    CleanedFilesCount = cleanupResult.CleanedFilesCount,
                    ReleasedSpace = cleanupResult.ReleasedSpace,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量清理失败: {TenantId}", tenantId);
                result.TenantDetails.Add(new TenantCleanupDetail
                {
                    TenantId = tenantId,
                    CleanedFilesCount = 0,
                    ReleasedSpace = 0,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.TotalReleasedSpaceFormatted = FormatFileSize(result.TotalReleasedSpace);

        return result;
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
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
