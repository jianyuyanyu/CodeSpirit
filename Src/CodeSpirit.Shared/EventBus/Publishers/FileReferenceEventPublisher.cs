using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeSpirit.Shared.EventBus.Publishers;

/// <summary>
/// 文件引用事件发布器
/// 提供文件引用相关事件的统一发布接口
/// </summary>
public class FileReferenceEventPublisher
{
    private readonly ITenantAwareEventBus _eventBus;
    private readonly ILogger<FileReferenceEventPublisher> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventBus">租户感知事件总线</param>
    /// <param name="logger">日志记录器</param>
    public FileReferenceEventPublisher(
        ITenantAwareEventBus eventBus,
        ILogger<FileReferenceEventPublisher> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 发布文件引用事件
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="entityName">实体名称</param>
    /// <param name="operationType">操作类型</param>
    /// <param name="fileReferences">文件引用信息列表</param>
    /// <param name="operatorUserId">操作用户ID</param>
    /// <param name="operatorUserName">操作用户名称</param>
    /// <param name="additionalData">附加数据</param>
    /// <returns>发布任务</returns>
    public async Task PublishFileReferenceEventAsync(
        string entityType,
        string entityId,
        string entityName,
        FileReferenceOperationType operationType,
        List<FileReferenceInfo> fileReferences,
        long? operatorUserId = null,
        string operatorUserName = "",
        object? additionalData = null)
    {
        try
        {
            var @event = new FileReferenceEvent
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                OperationType = operationType,
                FileReferences = fileReferences ?? new List<FileReferenceInfo>(),
                OperationTime = DateTime.UtcNow,
                OperatorUserId = operatorUserId,
                OperatorUserName = operatorUserName ?? string.Empty,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : string.Empty
            };

            await _eventBus.PublishAsync(@event);

            _logger.LogInformation(
                "已发布文件引用事件: 实体类型={EntityType}, 实体ID={EntityId}, 操作类型={OperationType}, 文件数量={FileCount}",
                entityType, entityId, operationType, fileReferences?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "发布文件引用事件失败: 实体类型={EntityType}, 实体ID={EntityId}, 操作类型={OperationType}",
                entityType, entityId, operationType);
            throw;
        }
    }





        /// <summary>
    /// 从文件URL中提取文件ID
    /// </summary>
    /// <param name="fileUrl">文件URL</param>
    /// <returns>文件ID，如果URL无效或为空则返回null</returns>
    public static long? ExtractFileIdFromUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return null;

        try
        {
            // 支持两种URL格式:
            // 1. API端点格式: /api/file/files/{fileId}/download
            // 2. 静态文件格式: /files/bucket/{fileId}_{hash}.ext
            
            var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
            var path = uri.IsAbsoluteUri ? uri.AbsolutePath : fileUrl;
            
            // 首先尝试从路径分段中查找数字ID（API端点格式）
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (long.TryParse(part, out var fileId) && fileId > 0)
                {
                    return fileId;
                }
            }
            
            // 如果没找到，尝试从文件名中提取ID（静态文件格式）
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(fileName))
            {
                var fileNameParts = fileName.Split('_');
                if (fileNameParts.Length >= 2 && long.TryParse(fileNameParts[0], out var fileIdFromName) && fileIdFromName > 0)
                {
                    return fileIdFromName;
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 创建文件引用信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileUrl">文件URL</param>
    /// <param name="referenceType">引用类型</param>
    /// <param name="description">描述</param>
    /// <param name="isPrimary">是否为主要文件</param>
    /// <param name="sortOrder">排序顺序</param>
    /// <returns>文件引用信息</returns>
    public static FileReferenceInfo CreateFileReference(
        long? fileId,
        string fileUrl,
        string referenceType,
        string description = "",
        bool isPrimary = false,
        int sortOrder = 0)
    {
        // 如果没有fileId但有URL，尝试从URL提取fileId
        if (!fileId.HasValue && !string.IsNullOrEmpty(fileUrl))
        {
            fileId = ExtractFileIdFromUrl(fileUrl);
        }

        return new FileReferenceInfo
        {
            FileId = fileId,
            FileUrl = fileUrl ?? string.Empty,
            ReferenceType = referenceType ?? string.Empty,
            Description = description ?? string.Empty,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }
}