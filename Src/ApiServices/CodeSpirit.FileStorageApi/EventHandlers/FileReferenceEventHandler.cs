using CodeSpirit.Core;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Implementations;
using CodeSpirit.Shared.EventBus.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeSpirit.FileStorageApi.EventHandlers;

/// <summary>
/// 文件引用事件处理器
/// 处理通用的文件引用事件
/// </summary>
public class FileReferenceEventHandler : ITenantAwareEventHandler<FileReferenceEvent>
{
    private readonly FileStorageDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<FileReferenceEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">文件存储数据库上下文</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="logger">日志记录器</param>
    public FileReferenceEventHandler(
        FileStorageDbContext context,
        ICurrentUser currentUser,
        ILogger<FileReferenceEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 验证文件引用事件的权限
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <returns>是否有权限处理</returns>
    public async Task<bool> CanHandleEventAsync(FileReferenceEvent @event)
    {
        // 基本权限检查
        if (string.IsNullOrEmpty(@event.TenantId) || string.IsNullOrEmpty(@event.EntityId))
        {
            return false;
        }

        // 可以根据业务需求添加更复杂的权限检查逻辑
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理文件引用事件（IEventHandler接口）
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(FileReferenceEvent @event)
    {
        // 创建租户上下文需要服务提供者，这里简化处理
        await HandleWithTenantContextAsync(@event, null!);
    }

    /// <summary>
    /// 处理文件引用事件（租户上下文）
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(FileReferenceEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation(
                "开始处理文件引用事件: 源服务={SourceService}, 实体类型={EntityType}, 实体ID={EntityId}, 操作类型={OperationType}, 租户ID={TenantId}",
                @event.SourceService, @event.EntityType, @event.EntityId, @event.OperationType, @event.TenantId);

            // 设置事件处理过程中的用户上下文，确保审计字段能够正确设置
            if (@event.OperatorUserId.HasValue && _currentUser is ISettableCurrentUser settableCurrentUser)
            {
                settableCurrentUser.SetUserId(@event.OperatorUserId.Value);
                if (!string.IsNullOrEmpty(@event.OperatorUserName))
                {
                    settableCurrentUser.SetUserName(@event.OperatorUserName);
                }
                if (!string.IsNullOrEmpty(@event.TenantId))
                {
                    settableCurrentUser.SetTenantId(@event.TenantId);
                }
                
                _logger.LogDebug("已设置事件处理用户上下文: UserId={UserId}, UserName={UserName}, TenantId={TenantId}",
                    @event.OperatorUserId, @event.OperatorUserName, @event.TenantId);
            }

            switch (@event.OperationType)
            {
                case FileReferenceOperationType.Create:
                case FileReferenceOperationType.Update:
                    await HandleCreateOrUpdateAsync(@event);
                    break;
                
                case FileReferenceOperationType.Delete:
                    await HandleDeleteAsync(@event);
                    break;
                
                default:
                    _logger.LogWarning("未知的文件引用操作类型: {OperationType}", @event.OperationType);
                    break;
            }

            _logger.LogInformation(
                "文件引用事件处理完成: 源服务={SourceService}, 实体类型={EntityType}, 实体ID={EntityId}, 操作类型={OperationType}",
                @event.SourceService, @event.EntityType, @event.EntityId, @event.OperationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "处理文件引用事件失败: 源服务={SourceService}, 实体类型={EntityType}, 实体ID={EntityId}, 操作类型={OperationType}",
                @event.SourceService, @event.EntityType, @event.EntityId, @event.OperationType);
            throw;
        }
        finally
        {
            // 重置用户上下文，避免影响后续处理
            if (_currentUser is ISettableCurrentUser settableUser)
            {
                settableUser.Reset();
                _logger.LogDebug("已重置事件处理用户上下文");
            }
        }
    }

    /// <summary>
    /// 处理创建或更新文件引用
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <returns>处理任务</returns>
    private async Task HandleCreateOrUpdateAsync(FileReferenceEvent @event)
    {
        if (@event.FileReferences == null || @event.FileReferences.Count == 0)
        {
            _logger.LogInformation(
                "文件引用事件中没有文件引用信息: 实体类型={EntityType}, 实体ID={EntityId}",
                @event.EntityType, @event.EntityId);
            return;
        }

        // 如果是更新操作，先删除现有的文件引用
        if (@event.OperationType == FileReferenceOperationType.Update)
        {
            await RemoveExistingFileReferencesAsync(@event.EntityType, @event.EntityId);
        }

        // 创建新的文件引用
        foreach (var fileRef in @event.FileReferences)
        {
            if (fileRef.FileId.HasValue && fileRef.FileId.Value > 0)
            {
                await CreateFileReferenceAsync(@event, fileRef);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 处理删除文件引用
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <returns>处理任务</returns>
    private async Task HandleDeleteAsync(FileReferenceEvent @event)
    {
        await RemoveExistingFileReferencesAsync(@event.EntityType, @event.EntityId);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "已删除实体的所有文件引用: 实体类型={EntityType}, 实体ID={EntityId}",
            @event.EntityType, @event.EntityId);
    }

    /// <summary>
    /// 移除现有的文件引用
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <returns>移除任务</returns>
    private async Task RemoveExistingFileReferencesAsync(string entityType, string entityId)
    {
        var existingReferences = await _context.FileReferences
            .Where(fr => fr.SourceEntityType == entityType && fr.SourceEntityId == entityId)
            .ToListAsync();

        if (existingReferences.Any())
        {
            _context.FileReferences.RemoveRange(existingReferences);
            
            _logger.LogInformation(
                "已移除 {Count} 个现有文件引用: 实体类型={EntityType}, 实体ID={EntityId}",
                existingReferences.Count, entityType, entityId);
        }
    }

    /// <summary>
    /// 创建文件引用
    /// </summary>
    /// <param name="event">文件引用事件</param>
    /// <param name="fileReference">文件引用信息</param>
    /// <returns>创建任务</returns>
    private async Task CreateFileReferenceAsync(FileReferenceEvent @event, FileReferenceInfo fileReference)
    {
        // 检查文件是否存在
        var fileExists = await _context.Files
            .AnyAsync(f => f.Id == fileReference.FileId!.Value);

        if (!fileExists)
        {
            _logger.LogWarning(
                "文件不存在，跳过创建文件引用: 文件ID={FileId}, 实体类型={EntityType}, 实体ID={EntityId}",
                fileReference.FileId, @event.EntityType, @event.EntityId);
            return;
        }

        // 确保用户ID不为空，避免审计字段设置失败
        var operatorUserId = @event.OperatorUserId ?? _currentUser?.Id;
        if (!operatorUserId.HasValue)
        {
            _logger.LogWarning(
                "无法获取操作用户ID，使用系统默认用户: 实体类型={EntityType}, 实体ID={EntityId}, 文件ID={FileId}",
                @event.EntityType, @event.EntityId, fileReference.FileId);
            operatorUserId = 0; // 使用系统用户ID
        }

        var fileReferenceEntity = new FileReferenceEntity
        {
            FileId = fileReference.FileId!.Value,
            SourceService = !string.IsNullOrEmpty(@event.SourceService) ? @event.SourceService : "FileStorage", // 使用事件中的源服务名称，如果为空则使用默认值
            SourceEntityType = @event.EntityType,
            SourceEntityId = @event.EntityId,
            FieldName = fileReference.ReferenceType,
            ReferenceType = MapReferenceType(fileReference.ReferenceType),
            Status = ReferenceStatus.Active, // 默认状态为活跃
            IsTemporary = false,
            Remarks = fileReference.Description,
            Properties = CreatePropertiesJson(fileReference),
            TenantId = @event.TenantId,
            CreatedAt = @event.OperationTime,
            CreatedBy = operatorUserId.Value,
            UpdatedAt = @event.OperationTime,
            UpdatedBy = operatorUserId.Value
        };

        _context.FileReferences.Add(fileReferenceEntity);

        _logger.LogDebug(
            "已创建文件引用: 源服务={SourceService}, 文件ID={FileId}, 实体类型={EntityType}, 实体ID={EntityId}, 引用类型={ReferenceType}",
            @event.SourceService, fileReference.FileId, @event.EntityType, @event.EntityId, fileReference.ReferenceType);
    }

    /// <summary>
    /// 根据实体类型和ID获取文件引用列表
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <returns>文件引用列表</returns>
    public async Task<List<FileReferenceEntity>> GetFileReferencesAsync(
        string entityType, 
        string entityId)
    {
        return await _context.FileReferences
            .Where(fr => fr.SourceEntityType == entityType && fr.SourceEntityId == entityId)
            .OrderBy(fr => fr.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 根据引用类型获取文件引用列表
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="referenceType">引用类型</param>
    /// <returns>文件引用列表</returns>
    public async Task<List<FileReferenceEntity>> GetFileReferencesByTypeAsync(
        string entityType,
        string entityId,
        string referenceType)
    {
        return await _context.FileReferences
            .Where(fr => fr.SourceEntityType == entityType 
                      && fr.SourceEntityId == entityId 
                      && fr.FieldName == referenceType)
            .OrderBy(fr => fr.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取主要文件引用
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="referenceType">引用类型（可选）</param>
    /// <returns>主要文件引用</returns>
    public async Task<FileReferenceEntity?> GetPrimaryFileReferenceAsync(
        string entityType,
        string entityId,
        string? referenceType = null)
    {
        var query = _context.FileReferences
            .Where(fr => fr.SourceEntityType == entityType 
                      && fr.SourceEntityId == entityId 
                      && fr.Status == ReferenceStatus.Active);

        if (!string.IsNullOrEmpty(referenceType))
        {
            query = query.Where(fr => fr.FieldName == referenceType);
        }

        return await query
            .OrderBy(fr => fr.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 映射引用类型字符串到枚举
    /// </summary>
    /// <param name="referenceTypeString">引用类型字符串</param>
    /// <returns>引用类型枚举</returns>
    private static ReferenceType MapReferenceType(string referenceTypeString)
    {
        return referenceTypeString?.ToLower() switch
        {
            "avatar" => ReferenceType.Avatar,
            "logo" => ReferenceType.Logo,
            "banner" => ReferenceType.Banner,
            "image" => ReferenceType.Image,
            "document" => ReferenceType.Document,
            "video" => ReferenceType.Video,
            "audio" => ReferenceType.Audio,
            _ => ReferenceType.Attachment
        };
    }

    /// <summary>
    /// 创建属性JSON字符串
    /// </summary>
    /// <param name="fileReference">文件引用信息</param>
    /// <returns>属性JSON字符串</returns>
    private static string CreatePropertiesJson(FileReferenceInfo fileReference)
    {
        var properties = new
        {
            FileUrl = fileReference.FileUrl,
            FileName = fileReference.FileName,
            FileSize = fileReference.FileSize,
            MimeType = fileReference.MimeType,
            SortOrder = fileReference.SortOrder,
            IsPrimary = fileReference.IsPrimary
        };

        return JsonSerializer.Serialize(properties);
    }
}