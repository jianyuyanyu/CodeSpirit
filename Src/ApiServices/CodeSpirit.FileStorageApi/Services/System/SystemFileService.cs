using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static CodeSpirit.FileStorageApi.Services.System.ISystemBucketService;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统文件服务实现
/// </summary>
public class SystemFileService : ISystemFileService
{
    private readonly FileStorageDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<SystemFileService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemFileService(
        FileStorageDbContext context,
        IFileStorageService fileStorageService,
        ILogger<SystemFileService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统文件列表
    /// </summary>
    public async Task<PageList<SystemFileDto>> GetSystemFilesAsync(SystemFileQueryDto queryDto)
    {
        try
        {
            var query = _context.Files.AsQueryable();

            // 应用筛选条件
            if (!string.IsNullOrEmpty(queryDto.TenantId))
                query = query.Where(f => f.TenantId == queryDto.TenantId);

            if (!string.IsNullOrEmpty(queryDto.BucketName))
                query = query.Where(f => f.BucketName == queryDto.BucketName);

            if (!string.IsNullOrEmpty(queryDto.FileName))
                query = query.Where(f => f.OriginalFileName.Contains(queryDto.FileName));

            if (queryDto.Category.HasValue)
                query = query.Where(f => f.Category == queryDto.Category.Value);

            if (queryDto.Status.HasValue)
                query = query.Where(f => f.Status == queryDto.Status.Value);

            if (!string.IsNullOrEmpty(queryDto.ContentType))
                query = query.Where(f => f.ContentType.Contains(queryDto.ContentType));

            if (!string.IsNullOrEmpty(queryDto.Extension))
                query = query.Where(f => f.Extension == queryDto.Extension);

            if (queryDto.MinSize.HasValue)
                query = query.Where(f => f.Size >= queryDto.MinSize.Value);

            if (queryDto.MaxSize.HasValue)
                query = query.Where(f => f.Size <= queryDto.MaxSize.Value);

            if (queryDto.IsPublic.HasValue)
                query = query.Where(f => f.IsPublic == queryDto.IsPublic.Value);

            if (queryDto.UploadStartTime.HasValue)
                query = query.Where(f => f.CreatedAt >= queryDto.UploadStartTime.Value);

            if (queryDto.UploadEndTime.HasValue)
                query = query.Where(f => f.CreatedAt <= queryDto.UploadEndTime.Value);

            if (queryDto.CreatedBy.HasValue)
                query = query.Where(f => f.CreatedBy == queryDto.CreatedBy.Value);

            if (!string.IsNullOrEmpty(queryDto.Tags))
                query = query.Where(f => f.Tags.Contains(queryDto.Tags));

            if (!queryDto.IncludeExpired)
                query = query.Where(f => !f.ExpirationTime.HasValue || f.ExpirationTime > DateTime.UtcNow);

            if (queryDto.OnlyReferenced)
            {
                var referencedFileIds = _context.FileReferences
                    .Where(fr => fr.Status == ReferenceStatus.Confirmed)
                    .Select(fr => fr.FileId)
                    .Distinct();
                query = query.Where(f => referencedFileIds.Contains(f.Id));
            }

            // 应用排序
            if (!string.IsNullOrEmpty(queryDto.OrderBy))
            {
                query = queryDto.OrderDir?.ToLower() == "desc"
                    ? query.OrderByDescending(x => EF.Property<object>(x, queryDto.OrderBy))
                    : query.OrderBy(x => EF.Property<object>(x, queryDto.OrderBy));
            }
            else
            {
                query = query.OrderByDescending(x => x.CreatedAt);
            }

            // 获取总数
            var totalCount = await query.CountAsync();

            // 分页查询
            var files = await query
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .ToListAsync();

            // 转换为DTO
            var items = new List<SystemFileDto>();
            foreach (var file in files)
            {
                var referenceCount = await _context.FileReferences
                    .CountAsync(fr => fr.FileId == file.Id && fr.Status == ReferenceStatus.Confirmed);

                items.Add(new SystemFileDto
                {
                    Id = file.Id,
                    TenantId = file.TenantId,
                    TenantName = file.TenantId, // 这里应该关联租户表获取名称
                    BucketName = file.BucketName,
                    OriginalFileName = file.OriginalFileName,
                    StorageFileName = file.StorageFileName,
                    FilePath = file.FilePath,
                    Size = file.Size,
                    SizeFormatted = FormatFileSize(file.Size),
                    ContentType = file.ContentType,
                    FileHash = file.FileHash,
                    Extension = file.Extension,
                    Category = file.Category,
                    Description = file.Description,
                    Status = file.Status,
                    AccessCount = file.AccessCount,
                    LastAccessTime = file.LastAccessTime,
                    ExpirationTime = file.ExpirationTime,
                    IsPublic = file.IsPublic,
                    DownloadUrl = file.DownloadUrl,
                    ETag = file.ETag,
                    Tags = file.Tags,
                    ReferenceCount = referenceCount,
                    CreatedBy = file.CreatedBy,
                    CreatedByName = "Unknown", // 这里应该关联用户表获取名称
                    CreatedTime = file.CreatedAt,
                    ModifiedTime = file.UpdatedAt
                });
            }

            return new PageList<SystemFileDto>(items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统文件列表失败");
            throw;
        }
    }

    /// <summary>
    /// 获取文件详情
    /// </summary>
    public async Task<SystemFileDto> GetFileDetailAsync(long id)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                throw new ArgumentException($"文件不存在: {id}");
            }

            var referenceCount = await _context.FileReferences
                .CountAsync(fr => fr.FileId == file.Id && fr.Status == ReferenceStatus.Confirmed);

            return new SystemFileDto
            {
                Id = file.Id,
                TenantId = file.TenantId,
                TenantName = file.TenantId,
                BucketName = file.BucketName,
                OriginalFileName = file.OriginalFileName,
                StorageFileName = file.StorageFileName,
                FilePath = file.FilePath,
                Size = file.Size,
                SizeFormatted = FormatFileSize(file.Size),
                ContentType = file.ContentType,
                FileHash = file.FileHash,
                Extension = file.Extension,
                Category = file.Category,
                Description = file.Description,
                Status = file.Status,
                AccessCount = file.AccessCount,
                LastAccessTime = file.LastAccessTime,
                ExpirationTime = file.ExpirationTime,
                IsPublic = file.IsPublic,
                DownloadUrl = file.DownloadUrl,
                ETag = file.ETag,
                Tags = file.Tags,
                ReferenceCount = referenceCount,
                CreatedBy = file.CreatedBy,
                CreatedByName = "Unknown",
                CreatedTime = file.CreatedAt,
                ModifiedTime = file.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件详情失败: {FileId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取文件引用信息
    /// </summary>
    public async Task<object> GetFileReferencesAsync(long id)
    {
        try
        {
            var references = await _context.FileReferences
                .Where(fr => fr.FileId == id)
                .Select(fr => new
                {
                    fr.Id,
                    fr.SourceService,
                    fr.SourceEntityType,
                    fr.SourceEntityId,
                    fr.FieldName,
                    fr.Status,
                    fr.CreatedAt,
                    fr.ConfirmedTime,
                    fr.Remarks
                })
                .ToListAsync();

            return new
            {
                FileId = id,
                TotalReferences = references.Count,
                ConfirmedReferences = references.Count(r => r.Status == ReferenceStatus.Confirmed),
                PendingReferences = references.Count(r => r.Status == ReferenceStatus.Pending),
                References = references
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件引用信息失败: {FileId}", id);
            throw;
        }
    }

    /// <summary>
    /// 强制删除文件
    /// </summary>
    public async Task ForceDeleteFileAsync(long id)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                throw new ArgumentException($"文件不存在: {id}");
            }

            // 删除所有引用
            var references = await _context.FileReferences
                .Where(fr => fr.FileId == id)
                .ToListAsync();
            
            _context.FileReferences.RemoveRange(references);

            // 删除文件记录
            _context.Files.Remove(file);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("强制删除文件成功: {FileId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制删除文件失败: {FileId}", id);
            throw;
        }
    }

    /// <summary>
    /// 恢复文件
    /// </summary>
    public async Task RestoreFileAsync(long id)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                throw new ArgumentException($"文件不存在: {id}");
            }

            file.Status = FileStatus.Active;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("恢复文件成功: {FileId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复文件失败: {FileId}", id);
            throw;
        }
    }

    /// <summary>
    /// 移动文件
    /// </summary>
    public async Task MoveFileAsync(long id, string targetBucketName)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                throw new ArgumentException($"文件不存在: {id}");
            }

            file.BucketName = targetBucketName;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("移动文件成功: {FileId} -> {TargetBucket}", id, targetBucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移动文件失败: {FileId} -> {TargetBucket}", id, targetBucketName);
            throw;
        }
    }

    /// <summary>
    /// 获取文件下载链接
    /// </summary>
    public async Task<object> GetFileDownloadUrlAsync(long id, int expirationMinutes)
    {
        try
        {
            var url = await _fileStorageService.GetDownloadUrlAsync(id, expirationMinutes);
            
            return new
            {
                FileId = id,
                DownloadUrl = url,
                ExpirationMinutes = expirationMinutes,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件下载链接失败: {FileId}", id);
            throw;
        }
    }

    /// <summary>
    /// 修复文件
    /// </summary>
    public async Task<FileRepairResult> RepairFileAsync(long id)
    {
        try
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                throw new ArgumentException($"文件不存在: {id}");
            }

            var result = new FileRepairResult
            {
                RepairTime = DateTime.UtcNow,
                RepairDetails = new List<string>()
            };

            // 检查文件状态
            if (file.Status == FileStatus.Deleted)
            {
                file.Status = FileStatus.Active;
                result.IsRepaired = true;
                result.RepairDetails.Add("修复文件状态：已删除 -> 活跃");
            }

            // 检查其他需要修复的项目
            if (string.IsNullOrEmpty(file.ETag))
            {
                file.ETag = Guid.NewGuid().ToString();
                result.IsRepaired = true;
                result.RepairDetails.Add("生成缺失的ETag");
            }

            if (result.IsRepaired)
            {
                file.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                result.RepairDetails.Add("保存修复结果");
            }
            else
            {
                result.RepairDetails.Add("文件无需修复");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修复文件失败: {FileId}", id);
            return new FileRepairResult
            {
                IsRepaired = false,
                ErrorMessage = ex.Message,
                RepairTime = DateTime.UtcNow,
                RepairDetails = new List<string> { $"修复失败: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// 获取文件统计摘要
    /// </summary>
    public async Task<object> GetFileStatisticsSummaryAsync()
    {
        try
        {
            var summary = await _context.Files
                .Where(f => f.Status == FileStatus.Active)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFiles = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    TotalSizeFormatted = FormatFileSize(g.Sum(f => f.Size)),
                    AverageSize = g.Average(f => f.Size),
                    AverageSizeFormatted = FormatFileSize((long)g.Average(f => f.Size)),
                    TotalAccessCount = g.Sum(f => f.AccessCount),
                    UniqueExtensions = g.Select(f => f.Extension).Distinct().Count(),
                    UniqueTenants = g.Select(f => f.TenantId).Distinct().Count(),
                    UniqueBuckets = g.Select(f => f.BucketName).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            return summary ?? new
            {
                TotalFiles = 0,
                TotalSize = 0L,
                TotalSizeFormatted = "0 B",
                AverageSize = 0.0,
                AverageSizeFormatted = "0 B",
                TotalAccessCount = 0L,
                UniqueExtensions = 0,
                UniqueTenants = 0,
                UniqueBuckets = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件统计摘要失败");
            throw;
        }
    }

    /// <summary>
    /// 获取文件类型分布
    /// </summary>
    public async Task<object> GetFileTypeDistributionAsync()
    {
        try
        {
            var distribution = await _context.Files
                .Where(f => f.Status == FileStatus.Active)
                .GroupBy(f => f.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    TotalSizeFormatted = FormatFileSize(g.Sum(f => f.Size)),
                    AverageSize = g.Average(f => f.Size),
                    Percentage = 0.0 // 将在后面计算
                })
                .ToListAsync();

            var totalFiles = distribution.Sum(d => d.Count);
            
            return distribution.Select(d => new
            {
                d.Category,
                d.Count,
                d.TotalSize,
                d.TotalSizeFormatted,
                AverageSize = (long)d.AverageSize,
                AverageSizeFormatted = FormatFileSize((long)d.AverageSize),
                Percentage = totalFiles > 0 ? (double)d.Count / totalFiles * 100 : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件类型分布失败");
            throw;
        }
    }

    /// <summary>
    /// 获取存储使用趋势
    /// </summary>
    public async Task<object> GetStorageUsageTrendAsync(int days)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            
            var trend = await _context.Files
                .Where(f => f.CreatedAt >= startDate)
                .GroupBy(f => f.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    FilesUploaded = g.Count(),
                    StorageAdded = g.Sum(f => f.Size),
                    StorageAddedFormatted = FormatFileSize(g.Sum(f => f.Size))
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new
            {
                Days = days,
                StartDate = startDate,
                EndDate = DateTime.UtcNow,
                TrendData = trend
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储使用趋势失败");
            throw;
        }
    }

    /// <summary>
    /// 清理过期文件
    /// </summary>
    public async Task<CleanupResult> CleanupExpiredFilesAsync()
    {
        try
        {
            var expiredFiles = await _context.Files
                .Where(f => f.ExpirationTime.HasValue && f.ExpirationTime < DateTime.UtcNow)
                .ToListAsync();

            var cleanedSize = expiredFiles.Sum(f => f.Size);
            var cleanedCount = expiredFiles.Count;

            _context.Files.RemoveRange(expiredFiles);
            await _context.SaveChangesAsync();

            return new CleanupResult
            {
                CleanedFilesCount = cleanedCount,
                ReleasedSpace = cleanedSize,
                ReleasedSpaceFormatted = FormatFileSize(cleanedSize),
                Details = new List<string>
                {
                    $"清理过期文件: {cleanedCount} 个",
                    $"释放存储空间: {FormatFileSize(cleanedSize)}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期文件失败");
            throw;
        }
    }

    /// <summary>
    /// 清理无引用文件
    /// </summary>
    public async Task<CleanupResult> CleanupUnreferencedFilesAsync()
    {
        try
        {
            var referencedFileIds = await _context.FileReferences
                .Where(fr => fr.Status == ReferenceStatus.Confirmed)
                .Select(fr => fr.FileId)
                .Distinct()
                .ToListAsync();

            var unreferencedFiles = await _context.Files
                .Where(f => !referencedFileIds.Contains(f.Id))
                .ToListAsync();

            var cleanedSize = unreferencedFiles.Sum(f => f.Size);
            var cleanedCount = unreferencedFiles.Count;

            _context.Files.RemoveRange(unreferencedFiles);
            await _context.SaveChangesAsync();

            return new CleanupResult
            {
                CleanedFilesCount = cleanedCount,
                ReleasedSpace = cleanedSize,
                ReleasedSpaceFormatted = FormatFileSize(cleanedSize),
                Details = new List<string>
                {
                    $"清理无引用文件: {cleanedCount} 个",
                    $"释放存储空间: {FormatFileSize(cleanedSize)}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理无引用文件失败");
            throw;
        }
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    public async Task<SystemBatchOperationResult> BatchDeleteFilesAsync(IEnumerable<long> fileIds)
    {
        var result = new SystemBatchOperationResult
        {
            Total = fileIds.Count(),
            StartTime = DateTime.UtcNow
        };

        foreach (var fileId in fileIds)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(fileId);
                result.Success++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Success
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// 批量恢复文件
    /// </summary>
    public async Task<SystemBatchOperationResult> BatchRestoreFilesAsync(IEnumerable<long> fileIds)
    {
        var result = new SystemBatchOperationResult
        {
            Total = fileIds.Count(),
            StartTime = DateTime.UtcNow
        };

        foreach (var fileId in fileIds)
        {
            try
            {
                await RestoreFileAsync(fileId);
                result.Success++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Success
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// 批量移动文件
    /// </summary>
    public async Task<SystemBatchOperationResult> BatchMoveFilesAsync(IEnumerable<long> fileIds, string targetBucketName)
    {
        var result = new SystemBatchOperationResult
        {
            Total = fileIds.Count(),
            StartTime = DateTime.UtcNow
        };

        foreach (var fileId in fileIds)
        {
            try
            {
                await MoveFileAsync(fileId, targetBucketName);
                result.Success++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Success
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = fileId.ToString(),
                    Status = SystemBatchOperationStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
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
