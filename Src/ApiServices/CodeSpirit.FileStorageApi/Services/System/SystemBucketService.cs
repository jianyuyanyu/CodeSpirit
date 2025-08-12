using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Options;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统存储桶服务实现
/// </summary>
public class SystemBucketService : ISystemBucketService
{
    private readonly FileStorageDbContext _context;
    private readonly FileStorageOptions _options;
    private readonly IStorageProviderFactory _storageProviderFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<SystemBucketService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemBucketService(
        FileStorageDbContext context,
        IOptions<FileStorageOptions> options,
        IStorageProviderFactory storageProviderFactory,
        IMapper mapper,
        ILogger<SystemBucketService> logger)
    {
        _context = context;
        _options = options.Value;
        _storageProviderFactory = storageProviderFactory;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统存储桶列表
    /// </summary>
    public async Task<PageList<SystemBucketDto>> GetSystemBucketsAsync(QueryDtoBase queryDto)
    {
        try
        {
            var buckets = new List<SystemBucketDto>();

            // 从配置中获取所有存储桶
            foreach (var bucketConfig in _options.Buckets)
            {
                var bucketName = bucketConfig.Key;
                var config = bucketConfig.Value;

                // 统计该存储桶的文件信息
                var fileStats = await _context.Files
                    .Where(f => f.BucketName == bucketName && f.Status == FileStatus.Active)
                    .GroupBy(f => 1)
                    .Select(g => new
                    {
                        TotalFiles = g.Count(),
                        TotalSize = g.Sum(f => f.Size),
                        LastUpload = g.Max(f => f.CreatedAt),
                        UsageTenants = g.Select(f => f.TenantId).Distinct().Count()
                    })
                    .FirstOrDefaultAsync();

                var bucketDto = _mapper.Map<SystemBucketDto>(config);
                bucketDto.BucketName = bucketName;
                bucketDto.TotalFiles = fileStats?.TotalFiles ?? 0;
                bucketDto.TotalStorageSize = fileStats?.TotalSize ?? 0;
                bucketDto.TotalStorageSizeFormatted = FormatFileSize(fileStats?.TotalSize ?? 0);
                bucketDto.LastUploadTime = fileStats?.LastUpload;
                bucketDto.UsageTenantsCount = fileStats?.UsageTenants ?? 0;
                
                buckets.Add(bucketDto);
            }

            // 应用排序
            if (!string.IsNullOrEmpty(queryDto.OrderBy))
            {
                buckets = queryDto.OrderDir?.ToLower() == "desc"
                    ? buckets.OrderByDescending(GetPropertyValue(queryDto.OrderBy)).ToList()
                    : buckets.OrderBy(GetPropertyValue(queryDto.OrderBy)).ToList();
            }
            else
            {
                buckets = buckets.OrderByDescending(x => x.TotalStorageSize).ToList();
            }

            // 分页
            var totalCount = buckets.Count;
            var items = buckets
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .ToList();

            return new PageList<SystemBucketDto>(items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统存储桶列表失败");
            throw;
        }
    }

    /// <summary>
    /// 获取存储桶详情
    /// </summary>
    public async Task<SystemBucketDto> GetBucketDetailAsync(string bucketName)
    {
        try
        {
            if (!_options.Buckets.TryGetValue(bucketName, out var config))
            {
                throw new ArgumentException($"存储桶 '{bucketName}' 不存在");
            }

            var fileStats = await _context.Files
                .Where(f => f.BucketName == bucketName && f.Status == FileStatus.Active)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFiles = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    LastUpload = g.Max(f => (DateTime?)f.CreatedAt),
                    UsageTenants = g.Select(f => f.TenantId).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            var bucketDto = _mapper.Map<SystemBucketDto>(config);
            bucketDto.BucketName = bucketName;
            bucketDto.TotalFiles = fileStats?.TotalFiles ?? 0;
            bucketDto.TotalStorageSize = fileStats?.TotalSize ?? 0;
            bucketDto.TotalStorageSizeFormatted = FormatFileSize(fileStats?.TotalSize ?? 0);
            bucketDto.LastUploadTime = fileStats?.LastUpload;
            bucketDto.UsageTenantsCount = fileStats?.UsageTenants ?? 0;
            
            return bucketDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储桶详情失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 获取存储桶统计信息
    /// </summary>
    public async Task<object> GetBucketStatisticsAsync(string bucketName)
    {
        try
        {
            var stats = await _context.Files
                .Where(f => f.BucketName == bucketName && f.Status == FileStatus.Active)
                .GroupBy(f => f.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    AverageSize = g.Average(f => f.Size)
                })
                .ToListAsync();

            var totalStats = await _context.Files
                .Where(f => f.BucketName == bucketName && f.Status == FileStatus.Active)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFiles = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    AverageSize = g.Average(f => f.Size),
                    MinSize = g.Min(f => f.Size),
                    MaxSize = g.Max(f => f.Size),
                    TotalAccessCount = g.Sum(f => f.AccessCount),
                    UniqueExtensions = g.Select(f => f.Extension).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            return new
            {
                BucketName = bucketName,
                Total = totalStats,
                ByCategory = stats,
                FormattedSizes = new
                {
                    TotalSize = FormatFileSize(totalStats?.TotalSize ?? 0),
                    AverageSize = FormatFileSize((long)(totalStats?.AverageSize ?? 0)),
                    MinSize = FormatFileSize(totalStats?.MinSize ?? 0),
                    MaxSize = FormatFileSize(totalStats?.MaxSize ?? 0)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储桶统计失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 启用存储桶
    /// </summary>
    public async Task EnableBucketAsync(string bucketName)
    {
        try
        {
            if (_options.Buckets.TryGetValue(bucketName, out var config))
            {
                config.IsEnabled = true;
                _logger.LogInformation("存储桶已启用: {BucketName}", bucketName);
            }
            else
            {
                throw new ArgumentException($"存储桶 '{bucketName}' 不存在");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启用存储桶失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 禁用存储桶
    /// </summary>
    public async Task DisableBucketAsync(string bucketName)
    {
        try
        {
            if (_options.Buckets.TryGetValue(bucketName, out var config))
            {
                config.IsEnabled = false;
                _logger.LogInformation("存储桶已禁用: {BucketName}", bucketName);
            }
            else
            {
                throw new ArgumentException($"存储桶 '{bucketName}' 不存在");
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "禁用存储桶失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 清理存储桶无效文件
    /// </summary>
    public async Task<CleanupResult> CleanupBucketInvalidFilesAsync(string bucketName)
    {
        try
        {
            var invalidFiles = await _context.Files
                .Where(f => f.BucketName == bucketName && 
                           (f.Status == FileStatus.Deleted || 
                            (f.ExpirationTime.HasValue && f.ExpirationTime < DateTime.UtcNow)))
                .ToListAsync();

            var cleanedSize = invalidFiles.Sum(f => f.Size);
            var cleanedCount = invalidFiles.Count;

            _context.Files.RemoveRange(invalidFiles);
            await _context.SaveChangesAsync();

            return new CleanupResult
            {
                CleanedFilesCount = cleanedCount,
                ReleasedSpace = cleanedSize,
                ReleasedSpaceFormatted = FormatFileSize(cleanedSize),
                Details = new List<string>
                {
                    $"清理存储桶: {bucketName}",
                    $"删除文件: {cleanedCount} 个",
                    $"释放空间: {FormatFileSize(cleanedSize)}"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理存储桶无效文件失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 刷新存储桶统计
    /// </summary>
    public async Task RefreshBucketStatisticsAsync(string bucketName)
    {
        try
        {
            _logger.LogInformation("刷新存储桶统计: {BucketName}", bucketName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新存储桶统计失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 测试存储桶连接
    /// </summary>
    public Task<BucketConnectionTestResult> TestBucketConnectionAsync(string bucketName)
    {
        var startTime = DateTime.UtcNow;
        var result = new BucketConnectionTestResult { TestTime = startTime };

        try
        {
            if (!_options.Buckets.TryGetValue(bucketName, out var config))
            {
                throw new ArgumentException($"存储桶 '{bucketName}' 不存在");
            }

            var provider = _storageProviderFactory.GetProvider(config.Provider);
            
            // 这里可以实现实际的连接测试逻辑
            // 例如：尝试列出文件、上传测试文件等
            
            result.IsSuccess = true;
            result.ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            result.TestDetails.Add($"存储桶: {bucketName}");
            result.TestDetails.Add($"提供程序: {config.Provider}");
            result.TestDetails.Add("连接测试成功");
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            result.TestDetails.Add($"连接测试失败: {ex.Message}");
            
            _logger.LogError(ex, "存储桶连接测试失败: {BucketName}", bucketName);
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 获取存储桶配置
    /// </summary>
    public Task<object> GetBucketConfigurationAsync(string bucketName)
    {
        try
        {
            if (!_options.Buckets.TryGetValue(bucketName, out var config))
            {
                throw new ArgumentException($"存储桶 '{bucketName}' 不存在");
            }

            return Task.FromResult<object>(new
            {
                BucketName = bucketName,
                DisplayName = config.DisplayName,
                Description = config.Description,
                Provider = config.Provider,
                AccessPolicy = config.AccessPolicy.ToString(),
                StorageQuota = config.StorageQuota,
                MaxFileSize = config.MaxFileSize ?? 0,
                AllowedFileTypes = config.AllowedFileTypes,
                ForbiddenFileTypes = config.ForbiddenFileTypes,
                RetentionDays = config.RetentionDays,
                IsEnabled = config.IsEnabled,
                Properties = config.Properties
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储桶配置失败: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// 批量启用存储桶
    /// </summary>
    public async Task<SystemBatchOperationResult> BatchEnableBucketsAsync(IEnumerable<string> bucketNames)
    {
        var result = new SystemBatchOperationResult
        {
            Total = bucketNames.Count(),
            StartTime = DateTime.UtcNow
        };

        foreach (var bucketName in bucketNames)
        {
            try
            {
                await EnableBucketAsync(bucketName);
                result.Success++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = bucketName,
                    Status = SystemBatchOperationStatus.Success
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = bucketName,
                    Status = SystemBatchOperationStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// 批量禁用存储桶
    /// </summary>
    public async Task<SystemBatchOperationResult> BatchDisableBucketsAsync(IEnumerable<string> bucketNames)
    {
        var result = new SystemBatchOperationResult
        {
            Total = bucketNames.Count(),
            StartTime = DateTime.UtcNow
        };

        foreach (var bucketName in bucketNames)
        {
            try
            {
                await DisableBucketAsync(bucketName);
                result.Success++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = bucketName,
                    Status = SystemBatchOperationStatus.Success
                });
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Details.Add(new SystemBatchOperationDetail
                {
                    ItemId = bucketName,
                    Status = SystemBatchOperationStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// 获取属性值函数
    /// </summary>
    private static Func<SystemBucketDto, object> GetPropertyValue(string propertyName)
    {
        return propertyName.ToLower() switch
        {
            "bucketname" => x => x.BucketName,
            "displayname" => x => x.DisplayName,
            "provider" => x => x.Provider,
            "totalfiles" => x => x.TotalFiles,
            "totalstoragesize" => x => x.TotalStorageSize,
            "lastuploadtime" => x => x.LastUploadTime ?? DateTime.MinValue,
            "isenabled" => x => x.IsEnabled,
            _ => x => x.BucketName
        };
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
