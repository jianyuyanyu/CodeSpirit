using AutoMapper;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.Core;
using CodeSpirit.Shared.EventBus.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 文件引用服务实现
/// </summary>
public class FileReferenceService : IFileReferenceService
{
    private readonly FileStorageDbContext _context;
    private readonly IIdGenerator _idGenerator;
    private readonly ITenantAwareEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<FileReferenceService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileReferenceService(
        FileStorageDbContext context,
        IIdGenerator idGenerator,
        ITenantAwareEventBus eventBus,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<FileReferenceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 创建文件引用
    /// </summary>
    public async Task<long> CreateReferenceAsync(FileReferenceCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.SourceService);
        ArgumentException.ThrowIfNullOrEmpty(request.SourceEntityType);
        ArgumentException.ThrowIfNullOrEmpty(request.SourceEntityId);

        _logger.LogInformation("创建文件引用: 文件ID={FileId}, 来源={SourceService}.{SourceEntityType}.{SourceEntityId}", 
            request.FileId, request.SourceService, request.SourceEntityType, request.SourceEntityId);

        try
        {
            // 1. 验证文件是否存在
            var fileExists = await _context.Files
                .AnyAsync(f => f.Id == request.FileId && f.Status == FileStatus.Active);

            if (!fileExists)
            {
                throw new AppServiceException(404, "文件不存在或已删除");
            }

            // 2. 检查是否已存在相同的引用
            var existingReference = await _context.FileReferences
                .FirstOrDefaultAsync(fr => 
                    fr.FileId == request.FileId &&
                    fr.SourceService == request.SourceService &&
                    fr.SourceEntityType == request.SourceEntityType &&
                    fr.SourceEntityId == request.SourceEntityId &&
                    fr.FieldName == request.FieldName &&
                    fr.Status != ReferenceStatus.Cancelled);

            if (existingReference != null)
            {
                _logger.LogInformation("文件引用已存在，返回现有引用: {ReferenceId}", existingReference.Id);
                return existingReference.Id;
            }

            // 3. 创建新的引用
            var reference = _mapper.Map<FileReferenceEntity>(request);
            reference.Id = _idGenerator.NewId();
            reference.TenantId = _currentUser.TenantId ?? "default";
            reference.FileId = request.FileId;
            reference.SourceService = request.SourceService;
            reference.SourceEntityType = request.SourceEntityType;
            reference.SourceEntityId = request.SourceEntityId;
            reference.FieldName = request.FieldName;
            reference.ReferenceType = request.ReferenceType;
            reference.Remarks = request.Remarks;

            await _context.FileReferences.AddAsync(reference);
            await _context.SaveChangesAsync();

            _logger.LogInformation("文件引用创建成功: {ReferenceId}", reference.Id);
            return reference.Id;
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "创建文件引用失败: 文件ID={FileId}", request.FileId);
            throw new AppServiceException(500, "创建文件引用失败");
        }
    }

    /// <summary>
    /// 确认文件引用
    /// </summary>
    public async Task<bool> ConfirmReferenceAsync(long referenceId)
    {
        _logger.LogInformation("确认文件引用: {ReferenceId}", referenceId);

        try
        {
            var reference = await _context.FileReferences
                .FirstOrDefaultAsync(fr => fr.Id == referenceId);

            if (reference == null)
            {
                _logger.LogWarning("文件引用不存在: {ReferenceId}", referenceId);
                return false;
            }

            if (reference.Status == ReferenceStatus.Confirmed)
            {
                _logger.LogInformation("文件引用已确认: {ReferenceId}", referenceId);
                return true;
            }

            if (reference.Status == ReferenceStatus.Cancelled || reference.Status == ReferenceStatus.Expired)
            {
                throw new AppServiceException(400, "无法确认已取消或已过期的引用");
            }

            // 更新引用状态
            reference.Status = ReferenceStatus.Confirmed;
            reference.ConfirmedTime = DateTime.UtcNow;
            reference.IsTemporary = false;
            reference.ExpirationTime = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("文件引用确认成功: {ReferenceId}", referenceId);
            return true;
        }
        catch (Exception ex) when (!(ex is AppServiceException))
        {
            _logger.LogError(ex, "确认文件引用失败: {ReferenceId}", referenceId);
            throw new AppServiceException(500, "确认文件引用失败");
        }
    }

    /// <summary>
    /// 取消文件引用
    /// </summary>
    public async Task<bool> CancelReferenceAsync(long referenceId)
    {
        _logger.LogInformation("取消文件引用: {ReferenceId}", referenceId);

        try
        {
            var reference = await _context.FileReferences
                .FirstOrDefaultAsync(fr => fr.Id == referenceId);

            if (reference == null)
            {
                _logger.LogWarning("文件引用不存在: {ReferenceId}", referenceId);
                return false;
            }

            if (reference.Status == ReferenceStatus.Cancelled)
            {
                _logger.LogInformation("文件引用已取消: {ReferenceId}", referenceId);
                return true;
            }

            // 更新引用状态
            reference.Status = ReferenceStatus.Cancelled;

            await _context.SaveChangesAsync();

            _logger.LogInformation("文件引用取消成功: {ReferenceId}", referenceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消文件引用失败: {ReferenceId}", referenceId);
            throw new AppServiceException(500, "取消文件引用失败");
        }
    }

    /// <summary>
    /// 批量确认文件引用
    /// </summary>
    public async Task<BatchOperationResult> BatchConfirmReferencesAsync(IEnumerable<long> referenceIds)
    {
        var referenceIdList = referenceIds.ToList();
        var result = new BatchOperationResult
        {
            Total = referenceIdList.Count,
            Success = 0,
            Failed = 0
        };

        _logger.LogInformation("开始批量确认文件引用: {Count} 个引用", referenceIdList.Count);

        foreach (var referenceId in referenceIdList)
        {
            try
            {
                var success = await ConfirmReferenceAsync(referenceId);
                if (success)
                {
                    result.Success++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add($"引用 {referenceId} 确认失败：引用不存在");
                }
            }
            catch (AppServiceException ex)
            {
                result.Failed++;
                result.Errors.Add($"引用 {referenceId} 确认失败：{ex.Message}");
                _logger.LogWarning(ex, "批量确认中单个引用确认失败: {ReferenceId}", referenceId);
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"引用 {referenceId} 确认失败：系统错误");
                _logger.LogError(ex, "批量确认中单个引用确认出现异常: {ReferenceId}", referenceId);
            }
        }

        _logger.LogInformation("批量确认文件引用完成: 总数 {Total}, 成功 {Success}, 失败 {Failed}", 
            result.Total, result.Success, result.Failed);

        return result;
    }

    /// <summary>
    /// 按来源删除文件引用
    /// </summary>
    public async Task<bool> DeleteReferencesBySourceAsync(string sourceService, string sourceEntityType, string sourceEntityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceService);
        ArgumentException.ThrowIfNullOrEmpty(sourceEntityType);
        ArgumentException.ThrowIfNullOrEmpty(sourceEntityId);

        _logger.LogInformation("按来源删除文件引用: {SourceService}.{SourceEntityType}.{SourceEntityId}", 
            sourceService, sourceEntityType, sourceEntityId);

        try
        {
            var references = await _context.FileReferences
                .Where(fr => 
                    fr.SourceService == sourceService &&
                    fr.SourceEntityType == sourceEntityType &&
                    fr.SourceEntityId == sourceEntityId)
                .ToListAsync();

            if (!references.Any())
            {
                _logger.LogInformation("未找到匹配的文件引用");
                return true;
            }

            _context.FileReferences.RemoveRange(references);
            await _context.SaveChangesAsync();

            _logger.LogInformation("按来源删除文件引用成功: 删除了 {Count} 个引用", references.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按来源删除文件引用失败: {SourceService}.{SourceEntityType}.{SourceEntityId}", 
                sourceService, sourceEntityType, sourceEntityId);
            throw new AppServiceException(500, "删除文件引用失败");
        }
    }

    public async Task<IEnumerable<FileReferenceEntity>> GetFileReferencesAsync(long fileId)
    {
        return await _context.FileReferences
            .Where(fr => fr.FileId == fileId)
            .ToListAsync();
    }

    public async Task<FileReferenceEntity?> GetReferenceAsync(long referenceId)
    {
        return await _context.FileReferences
            .FirstOrDefaultAsync(fr => fr.Id == referenceId);
    }

    public async Task<PageList<FileReferenceEntity>> QueryReferencesAsync(ReferenceQueryRequest request)
    {
        var query = _context.FileReferences.AsQueryable();

        // 应用过滤条件
        if (request.FileId.HasValue)
        {
            query = query.Where(fr => fr.FileId == request.FileId.Value);
        }

        if (!string.IsNullOrEmpty(request.SourceService))
        {
            query = query.Where(fr => fr.SourceService == request.SourceService);
        }

        if (!string.IsNullOrEmpty(request.SourceEntityType))
        {
            query = query.Where(fr => fr.SourceEntityType == request.SourceEntityType);
        }

        if (!string.IsNullOrEmpty(request.SourceEntityId))
        {
            query = query.Where(fr => fr.SourceEntityId == request.SourceEntityId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(fr => fr.Status == request.Status.Value);
        }

        if (request.ReferenceType.HasValue)
        {
            query = query.Where(fr => fr.ReferenceType == request.ReferenceType.Value);
        }

        if (request.IsTemporary.HasValue)
        {
            query = query.Where(fr => fr.IsTemporary == request.IsTemporary.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(fr => fr.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(fr => fr.CreatedAt <= request.CreatedTo.Value);
        }

        // 排序
        query = request.OrderBy?.ToLower() switch
        {
            "status" => request.Descending ? query.OrderByDescending(fr => fr.Status) : query.OrderBy(fr => fr.Status),
            "type" => request.Descending ? query.OrderByDescending(fr => fr.ReferenceType) : query.OrderBy(fr => fr.ReferenceType),
            _ => request.Descending ? query.OrderByDescending(fr => fr.CreatedAt) : query.OrderBy(fr => fr.CreatedAt)
        };

        // 分页
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageList<FileReferenceEntity>(items, (int)totalCount);
    }

    public async Task<int> CleanupExpiredReferencesAsync()
    {
        var expiredReferences = await _context.FileReferences
            .Where(fr => fr.IsTemporary && fr.ExpirationTime.HasValue && fr.ExpirationTime.Value < DateTime.UtcNow)
            .ToListAsync();

        if (expiredReferences.Any())
        {
            _context.FileReferences.RemoveRange(expiredReferences);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("清理了 {Count} 个过期的临时文件引用", expiredReferences.Count);
        }

        return expiredReferences.Count;
    }

    public async Task<IEnumerable<long>> GetUnreferencedFilesAsync(DateTime olderThan)
    {
        return await _context.Files
            .Where(f => f.CreatedAt < olderThan && !f.References.Any(r => r.Status == ReferenceStatus.Confirmed))
            .Select(f => f.Id)
            .ToListAsync();
    }
}
