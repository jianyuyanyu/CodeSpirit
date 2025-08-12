using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Extensions;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Events;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.Core;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Events;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 文件存储服务实现
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly FileStorageDbContext _context;
    private readonly IBucketConfigurationService _bucketService;
    private readonly IStorageProviderFactory _storageProviderFactory;
    private readonly IFileStorageMetrics _metrics;
    private readonly IIdGenerator _idGenerator;
    private readonly ITenantAwareEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<FileStorageService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileStorageService(
        FileStorageDbContext context,
        IBucketConfigurationService bucketService,
        IStorageProviderFactory storageProviderFactory,
        IFileStorageMetrics metrics,
        IIdGenerator idGenerator,
        ITenantAwareEventBus eventBus,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<FileStorageService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _bucketService = bucketService ?? throw new ArgumentNullException(nameof(bucketService));
        _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    public async Task<FileEntity> UploadFileAsync(FileUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FileStream);
        ArgumentException.ThrowIfNullOrEmpty(request.FileName);

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("开始上传文件: {FileName} 到存储桶: {BucketName}", 
            request.FileName, request.BucketName);

        try
        {
            // 1. 验证和准备参数
            var bucketName = string.IsNullOrEmpty(request.BucketName) ? 
                _bucketService.GetDefaultBucket().Name : request.BucketName;

            var bucketConfig = _bucketService.GetBucketByName(bucketName);
            if (bucketConfig == null)
            {
                throw new AppServiceException(400, $"存储桶 '{bucketName}' 不存在或未配置");
            }

            if (!bucketConfig.IsEnabled)
            {
                throw new AppServiceException(400, $"存储桶 '{bucketName}' 已禁用");
            }

            // 2. 获取文件大小和验证
            var fileSize = request.FileStream.Length;
            
            // 验证文件类型
            if (!_bucketService.ValidateFileType(bucketName, request.ContentType, request.FileName))
            {
                throw new AppServiceException(400, "不支持的文件类型");
            }

            // 验证配额和文件大小
            if (!await _bucketService.CheckBucketQuotaAsync(bucketName, fileSize))
            {
                throw new AppServiceException(400, "超出存储桶配额或单文件大小限制");
            }

            // 3. 计算文件哈希
            var fileHash = await ComputeFileHashAsync(request.FileStream);
            
            // 检查是否已存在相同文件
            var existingFile = await _context.Files
                .FirstOrDefaultAsync(f => f.FileHash == fileHash && f.BucketName == bucketName);
            
            if (existingFile != null && !request.OverwriteExisting)
            {
                _logger.LogInformation("文件已存在，返回现有文件: {FileId}", existingFile.Id);
                return existingFile;
            }

            // 4. 预先生成文件ID
            var fileId = _idGenerator.NewId();
            
            // 5. 生成包含文件ID的存储文件名
            var storageFileName = GenerateStorageFileName(request.FileName, fileId, fileHash);
            var filePath = GenerateFilePath(storageFileName);

            // 6. 获取存储提供程序并上传
            var storageProvider = _storageProviderFactory.GetProvider(bucketConfig.Provider);
            request.FileStream.Position = 0; // 重置流位置

            var uploadResult = await storageProvider.UploadFileAsync(
                bucketName, 
                storageFileName, 
                request.FileStream, 
                request.ContentType, 
                CreateMetadata(request));

            if (!uploadResult.Success)
            {
                throw new AppServiceException(500, $"文件上传失败: {uploadResult.ErrorMessage}");
            }

            // 7. 创建数据库记录
            var fileEntity = _mapper.Map<FileEntity>(request);
            fileEntity.Id = fileId;
            fileEntity.TenantId = _currentUser.TenantId ?? "default";
            fileEntity.BucketName = bucketName;
            fileEntity.StorageFileName = storageFileName;
            fileEntity.FilePath = filePath;
            fileEntity.Size = fileSize;
            fileEntity.FileHash = fileHash;
            fileEntity.Extension = Path.GetExtension(request.FileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
            fileEntity.Category = FileTypeCategoryHelper.GetCategory(request.FileName, request.ContentType ?? "application/octet-stream");
            fileEntity.Status = FileStatus.Active;
            fileEntity.AccessCount = 0;
            fileEntity.DownloadUrl = uploadResult.FileUrl;
            fileEntity.ETag = uploadResult.ETag ?? string.Empty;
            fileEntity.Properties = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : string.Empty;

            // 如果是覆盖现有文件，则更新现有记录
            if (existingFile != null && request.OverwriteExisting)
            {
                // 保留原始ID和审计信息
                fileEntity.Id = existingFile.Id;
                fileEntity.CreatedAt = existingFile.CreatedAt;
                fileEntity.CreatedBy = existingFile.CreatedBy;
                
                _context.Entry(existingFile).CurrentValues.SetValues(fileEntity);
                fileEntity = existingFile;
            }
            else
            {
                await _context.Files.AddAsync(fileEntity);
            }

            await _context.SaveChangesAsync();

            // 7. 更新统计信息
            await _bucketService.UpdateBucketStatisticsAsync(bucketName, 1, fileSize);

            // 8. 记录性能指标
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordFileUpload(duration, fileSize, bucketName, fileEntity.Category, storageProvider.ProviderType);

            // 9. 发布事件
            await PublishFileUploadedEventAsync(fileEntity);

            _logger.LogInformation("文件上传成功: {FileId}, 文件名: {FileName}, 大小: {Size} 字节", 
                fileEntity.Id, fileEntity.OriginalFileName, fileEntity.Size);

            return fileEntity;
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "文件上传失败: {FileName}", request.FileName);
            _metrics.RecordError("upload", ex.GetType().Name, request.BucketName);
            throw new AppServiceException(500, "文件上传失败");
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    public async Task<(Stream Stream, FileEntity FileInfo)> DownloadFileAsync(long fileId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("开始下载文件: {FileId}", fileId);

        try
        {
            // 1. 获取文件信息
            var fileEntity = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Status == FileStatus.Active);

            if (fileEntity == null)
            {
                throw new AppServiceException(404, "文件不存在或已删除");
            }

            // 2. 验证文件是否过期
            if (fileEntity.ExpirationTime.HasValue && fileEntity.ExpirationTime.Value < DateTime.UtcNow)
            {
                throw new AppServiceException(410, "文件已过期");
            }

            // 3. 获取存储提供程序
            var bucketConfig = _bucketService.GetBucketByName(fileEntity.BucketName);
            if (bucketConfig == null)
            {
                throw new AppServiceException(500, "存储桶配置不存在");
            }

            var storageProvider = _storageProviderFactory.GetProvider(bucketConfig.Provider);

            // 4. 从存储提供程序下载文件
            var fileStream = await storageProvider.DownloadFileAsync(fileEntity.BucketName, fileEntity.StorageFileName);

            // 5. 更新访问统计
            await UpdateFileAccessAsync(fileEntity, startTime);

            // 6. 记录性能指标
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordFileDownload(duration, fileEntity.Size, fileEntity.BucketName, fileEntity.Category, false);

            _logger.LogInformation("文件下载成功: {FileId}, 文件名: {FileName}", 
                fileEntity.Id, fileEntity.OriginalFileName);

            return (fileStream, fileEntity);
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "文件下载失败: {FileId}", fileId);
            _metrics.RecordError("download", ex.GetType().Name);
            throw new AppServiceException(500, "文件下载失败");
        }
    }

    /// <summary>
    /// 获取文件下载URL
    /// </summary>
    public async Task<string> GetDownloadUrlAsync(long fileId, int expirationMinutes = 60)
    {
        _logger.LogInformation("获取文件下载URL: {FileId}, 过期时间: {ExpirationMinutes}分钟", fileId, expirationMinutes);

        try
        {
            // 1. 获取文件信息
            var fileEntity = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.Status == FileStatus.Active);

            if (fileEntity == null)
            {
                throw new AppServiceException(404, "文件不存在或已删除");
            }

            // 2. 验证文件是否过期
            if (fileEntity.ExpirationTime.HasValue && fileEntity.ExpirationTime.Value < DateTime.UtcNow)
            {
                throw new AppServiceException(410, "文件已过期");
            }

            // 3. 如果是公开文件且已有URL，直接返回
            if (fileEntity.IsPublic && !string.IsNullOrEmpty(fileEntity.DownloadUrl))
            {
                return fileEntity.DownloadUrl;
            }

            // 4. 获取存储提供程序
            var bucketConfig = _bucketService.GetBucketByName(fileEntity.BucketName);
            if (bucketConfig == null)
            {
                throw new AppServiceException(500, "存储桶配置不存在");
            }

            var storageProvider = _storageProviderFactory.GetProvider(bucketConfig.Provider);

            // 5. 生成预签名URL
            var expirationTime = TimeSpan.FromMinutes(expirationMinutes);
            var downloadUrl = await storageProvider.GeneratePresignedUrlAsync(
                fileEntity.BucketName, 
                fileEntity.StorageFileName, 
                expirationTime, 
                PresignedUrlOperation.Read);

            // 6. 更新访问统计（异步）
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateFileAccessAsync(fileEntity, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "更新文件访问统计失败: {FileId}", fileId);
                }
            });

            _logger.LogInformation("生成文件下载URL成功: {FileId}", fileId);
            return downloadUrl;
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "获取文件下载URL失败: {FileId}", fileId);
            _metrics.RecordError("get_download_url", ex.GetType().Name);
            throw new AppServiceException(500, "获取下载URL失败");
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public async Task<bool> DeleteFileAsync(long fileId)
    {
        _logger.LogInformation("开始删除文件: {FileId}", fileId);

        try
        {
            // 1. 获取文件信息
            var fileEntity = await _context.Files
                .Include(f => f.References)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileEntity == null)
            {
                _logger.LogWarning("文件不存在: {FileId}", fileId);
                return false;
            }

            // 2. 检查文件引用
            var activeReferences = fileEntity.References
                .Where(r => r.Status == ReferenceStatus.Confirmed)
                .ToList();

            if (activeReferences.Any())
            {
                _logger.LogWarning("文件仍有活跃引用，无法删除: {FileId}, 引用数: {ReferenceCount}", 
                    fileId, activeReferences.Count);
                throw new AppServiceException(400, "文件仍被引用，无法删除");
            }

            // 3. 获取存储提供程序
            var bucketConfig = _bucketService.GetBucketByName(fileEntity.BucketName);
            if (bucketConfig != null)
            {
                try
                {
                    var storageProvider = _storageProviderFactory.GetProvider(bucketConfig.Provider);
                    
                    // 4. 从存储提供程序删除物理文件
                    await storageProvider.DeleteFileAsync(fileEntity.BucketName, fileEntity.StorageFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从存储提供程序删除文件失败: {FileId}, 继续删除数据库记录", fileId);
                }
            }

            // 5. 删除数据库记录
            _context.Files.Remove(fileEntity);
            await _context.SaveChangesAsync();

            // 6. 更新统计信息
            await _bucketService.UpdateBucketStatisticsAsync(fileEntity.BucketName, -1, -fileEntity.Size);

            // 7. 记录性能指标
            _metrics.RecordFileDelete(fileEntity.BucketName, fileEntity.Category, true);

            // 8. 发布事件
            await PublishFileDeletedEventAsync(fileEntity);

            _logger.LogInformation("文件删除成功: {FileId}, 文件名: {FileName}", 
                fileEntity.Id, fileEntity.OriginalFileName);

            return true;
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "文件删除失败: {FileId}", fileId);
            _metrics.RecordFileDelete("unknown", FileTypeCategory.Unknown, false);
            _metrics.RecordError("delete", ex.GetType().Name);
            throw new AppServiceException(500, "文件删除失败");
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public async Task<FileEntity?> GetFileInfoAsync(long fileId)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId);
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    public async Task<BatchOperationResult> BatchDeleteFilesAsync(IEnumerable<long> fileIds)
    {
        var fileIdList = fileIds.ToList();
        var result = new BatchOperationResult
        {
            Total = fileIdList.Count,
            Success = 0,
            Failed = 0
        };

        _logger.LogInformation("开始批量删除文件: {Count} 个文件", fileIdList.Count);

        foreach (var fileId in fileIdList)
        {
            try
            {
                var success = await DeleteFileAsync(fileId);
                if (success)
                {
                    result.Success++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add($"文件 {fileId} 删除失败：文件不存在");
                }
            }
            catch (AppServiceException ex)
            {
                result.Failed++;
                result.Errors.Add($"文件 {fileId} 删除失败：{ex.Message}");
                _logger.LogWarning(ex, "批量删除中单个文件删除失败: {FileId}", fileId);
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"文件 {fileId} 删除失败：系统错误");
                _logger.LogError(ex, "批量删除中单个文件删除出现异常: {FileId}", fileId);
            }
        }

        _logger.LogInformation("批量删除文件完成: 总数 {Total}, 成功 {Success}, 失败 {Failed}", 
            result.Total, result.Success, result.Failed);

        return result;
    }

    /// <summary>
    /// 根据条件查询文件
    /// </summary>
    public async Task<PageList<FileEntity>> QueryFilesAsync(FileQueryRequest request)
    {
        var query = _context.Files.AsQueryable();

        // 应用过滤条件
        if (!string.IsNullOrEmpty(request.BucketName))
        {
            query = query.Where(f => f.BucketName == request.BucketName);
        }

        if (request.Category.HasValue)
        {
            query = query.Where(f => f.Category == request.Category.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(f => f.Status == request.Status.Value);
        }

        if (!string.IsNullOrEmpty(request.FileName))
        {
            query = query.Where(f => f.OriginalFileName.Contains(request.FileName));
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(f => f.CreatedAt <= request.CreatedTo.Value);
        }

        // 排序
        query = request.OrderBy?.ToLower() switch
        {
            "filename" => request.Descending ? query.OrderByDescending(f => f.OriginalFileName) : query.OrderBy(f => f.OriginalFileName),
            "size" => request.Descending ? query.OrderByDescending(f => f.Size) : query.OrderBy(f => f.Size),
            "category" => request.Descending ? query.OrderByDescending(f => f.Category) : query.OrderBy(f => f.Category),
            _ => request.Descending ? query.OrderByDescending(f => f.CreatedAt) : query.OrderBy(f => f.CreatedAt)
        };

        // 分页
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageList<FileEntity>(items, (int)totalCount);
    }

    /// <summary>
    /// 更新文件访问时间
    /// </summary>
    public async Task<bool> UpdateAccessTimeAsync(long fileId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null)
        {
            return false;
        }

        file.AccessCount++;
        file.LastAccessTime = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    #region 私有辅助方法

    /// <summary>
    /// 计算文件哈希值
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <returns>MD5哈希值</returns>
    private static async Task<string> ComputeFileHashAsync(Stream fileStream)
    {
        var originalPosition = fileStream.Position;
        fileStream.Position = 0;

        using var md5 = MD5.Create();
        var hashBytes = await md5.ComputeHashAsync(fileStream);
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        fileStream.Position = originalPosition;
        return hash;
    }

    /// <summary>
    /// 生成存储文件名
    /// </summary>
    /// <param name="originalFileName">原始文件名</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileHash">文件哈希</param>
    /// <returns>存储文件名</returns>
    private static string GenerateStorageFileName(string originalFileName, long fileId, string fileHash)
    {
        var extension = Path.GetExtension(originalFileName);
        
        return $"{fileId}_{fileHash}{extension}";
    }

    /// <summary>
    /// 生成文件路径
    /// </summary>
    /// <param name="storageFileName">存储文件名</param>
    /// <returns>文件路径</returns>
    private static string GenerateFilePath(string storageFileName)
    {
        var date = DateTime.UtcNow;
        return $"{date.Year:D4}/{date.Month:D2}/{date.Day:D2}/{storageFileName}";
    }

    /// <summary>
    /// 创建元数据
    /// </summary>
    /// <param name="request">上传请求</param>
    /// <returns>元数据字典</returns>
    private Dictionary<string, string> CreateMetadata(FileUploadRequest request)
    {
        var metadata = new Dictionary<string, string>
        {
            ["original_filename"] = request.FileName,
            ["upload_time"] = DateTime.UtcNow.ToString("O"),
            ["tenant_id"] = _currentUser.TenantId ?? "default",
            ["user_id"] = _currentUser.Id?.ToString() ?? "anonymous"
        };

        if (!string.IsNullOrEmpty(request.Description))
        {
            metadata["description"] = request.Description;
        }

        if (request.Tags != null)
        {
            foreach (var tag in request.Tags)
            {
                metadata[$"tag_{tag.Key}"] = tag.Value;
            }
        }

        return metadata;
    }

    /// <summary>
    /// 更新文件访问统计
    /// </summary>
    /// <param name="fileEntity">文件实体</param>
    /// <param name="accessTime">访问时间</param>
    private async Task UpdateFileAccessAsync(FileEntity fileEntity, DateTime accessTime)
    {
        try
        {
            fileEntity.AccessCount++;
            fileEntity.LastAccessTime = accessTime;
            
            _context.Entry(fileEntity).Property(f => f.AccessCount).IsModified = true;
            _context.Entry(fileEntity).Property(f => f.LastAccessTime).IsModified = true;
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新文件访问统计失败: {FileId}", fileEntity.Id);
        }
    }

    /// <summary>
    /// 发布文件上传事件
    /// </summary>
    /// <param name="fileEntity">文件实体</param>
    private async Task PublishFileUploadedEventAsync(FileEntity fileEntity)
    {
        try
        {
            var @event = new FileUploadedEvent
            {
                FileId = fileEntity.Id,
                TenantId = fileEntity.TenantId,
                BucketName = fileEntity.BucketName,
                FileName = fileEntity.OriginalFileName,
                FileSize = fileEntity.Size,
                ContentType = fileEntity.ContentType,
                Category = fileEntity.Category.ToString(),
                UploadedAt = fileEntity.CreatedAt,
                UploadedBy = fileEntity.CreatedBy
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogDebug("已发布文件上传事件: {FileId}", fileEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发布文件上传事件失败: {FileId}", fileEntity.Id);
        }
    }

    /// <summary>
    /// 发布文件删除事件
    /// </summary>
    /// <param name="fileEntity">文件实体</param>
    private async Task PublishFileDeletedEventAsync(FileEntity fileEntity)
    {
        try
        {
            var @event = new FileDeletedEvent
            {
                FileId = fileEntity.Id,
                TenantId = fileEntity.TenantId,
                BucketName = fileEntity.BucketName,
                FileName = fileEntity.OriginalFileName,
                FileSize = fileEntity.Size,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = _currentUser.Id
            };

            await _eventBus.PublishAsync(@event);
            _logger.LogDebug("已发布文件删除事件: {FileId}", fileEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发布文件删除事件失败: {FileId}", fileEntity.Id);
        }
    }

    #endregion
}
