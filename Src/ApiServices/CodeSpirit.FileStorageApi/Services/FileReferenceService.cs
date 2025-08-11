using CodeSpirit.Core.Dtos;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 文件引用服务实现
/// </summary>
public class FileReferenceService : IFileReferenceService
{
    private readonly FileStorageDbContext _context;
    private readonly ILogger<FileReferenceService> _logger;

    public FileReferenceService(
        FileStorageDbContext context,
        ILogger<FileReferenceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<long> CreateReferenceAsync(FileReferenceCreateRequest request)
    {
        // TODO: 实现文件引用创建逻辑
        throw new NotImplementedException("创建文件引用功能尚未实现");
    }

    public async Task<bool> ConfirmReferenceAsync(long referenceId)
    {
        // TODO: 实现文件引用确认逻辑
        throw new NotImplementedException("确认文件引用功能尚未实现");
    }

    public async Task<bool> CancelReferenceAsync(long referenceId)
    {
        // TODO: 实现文件引用取消逻辑
        throw new NotImplementedException("取消文件引用功能尚未实现");
    }

    public async Task<BatchOperationResult> BatchConfirmReferencesAsync(IEnumerable<long> referenceIds)
    {
        // TODO: 实现批量确认文件引用逻辑
        var result = new BatchOperationResult
        {
            Total = referenceIds.Count(),
            Success = 0,
            Failed = 0
        };
        return result;
    }

    public async Task<bool> DeleteReferencesBySourceAsync(string sourceService, string sourceEntityType, string sourceEntityId)
    {
        // TODO: 实现按来源删除引用逻辑
        throw new NotImplementedException("按来源删除引用功能尚未实现");
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
