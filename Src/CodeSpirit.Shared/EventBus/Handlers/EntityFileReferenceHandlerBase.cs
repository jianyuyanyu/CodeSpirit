using CodeSpirit.Core;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Publishers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.EventBus.Handlers;

/// <summary>
/// 实体文件引用事件处理器基类
/// 提供通用的文件引用事件处理逻辑，支持配置驱动
/// </summary>
/// <typeparam name="THandler">具体处理器类型</typeparam>
public abstract class EntityFileReferenceHandlerBase<THandler>
    where THandler : EntityFileReferenceHandlerBase<THandler>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<THandler> _logger;

    /// <summary>
    /// 实体文件字段配置
    /// 由具体实现类提供配置
    /// </summary>
    protected abstract Dictionary<Type, EntityFileConfig> EntityConfigs { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    protected EntityFileReferenceHandlerBase(IServiceProvider serviceProvider, ILogger<THandler> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理实体状态变更事件
    /// </summary>
    public void HandleEntityStateChanged(EntityStateChangedEventArgs e, object entity)
    {
        // 快速过滤：只处理支持的实体类型
        if (!EntityConfigs.ContainsKey(entity.GetType()) || !IsEntityEventSupported(entity))
            return;

        // 异步处理，避免阻塞SaveChanges
        _ = Task.Run(async () => await ProcessFileReferenceEventAsync(e, entity));
    }

    /// <summary>
    /// 处理文件引用事件
    /// </summary>
    private async Task ProcessFileReferenceEventAsync(EntityStateChangedEventArgs e, object entity)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetService<FileReferenceEventPublisher>();
            var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
            
            if (publisher == null)
            {
                _logger.LogWarning("FileReferenceEventPublisher服务未注册");
                return;
            }

            var config = EntityConfigs[entity.GetType()];
            var fileUrl = config.GetFileUrl(entity);
            var entityId = config.GetEntityId(entity);
            var entityName = config.GetEntityName(entity);
            var operationType = GetOperationType(e.NewState);

            // 统一的文件引用处理逻辑
            var fileReferences = CreateFileReferences(fileUrl, config);
            
            await publisher.PublishFileReferenceEventAsync(
                entity.GetType().FullName ?? string.Empty, entityId, entityName, operationType, fileReferences,
                currentUser?.Id, currentUser?.UserName ?? string.Empty);

            _logger.LogDebug("{EntityType}文件引用事件发布成功: {EntityId}, {Operation}, {FileCount}个文件", 
                config.EntityType, entityId, operationType, fileReferences.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理{EntityType}文件引用事件失败: {EntityState}", 
                entity.GetType().Name, e.NewState);
        }
    }

    /// <summary>
    /// 创建文件引用列表
    /// </summary>
    protected virtual List<FileReferenceInfo> CreateFileReferences(string fileUrl, EntityFileConfig config)
    {
        var fileReferences = new List<FileReferenceInfo>();
        
        if (!string.IsNullOrEmpty(fileUrl))
        {
            var fileId = FileReferenceEventPublisher.ExtractFileIdFromUrl(fileUrl);
            fileReferences.Add(FileReferenceEventPublisher.CreateFileReference(
                fileId, fileUrl, config.FileType, config.FileDescription, isPrimary: true));
        }
        
        return fileReferences;
    }

    /// <summary>
    /// 检查实体是否支持事件处理
    /// </summary>
    protected virtual bool IsEntityEventSupported(object entity) =>
        entity is IEntityCreatedEvent or IEntityUpdatedEvent or IEntityDeletedEvent;

    /// <summary>
    /// 根据实体状态获取文件引用操作类型
    /// </summary>
    protected virtual FileReferenceOperationType GetOperationType(EntityState state) => state switch
    {
        EntityState.Added => FileReferenceOperationType.Create,
        EntityState.Modified => FileReferenceOperationType.Update,
        EntityState.Deleted => FileReferenceOperationType.Delete,
        _ => FileReferenceOperationType.Update
    };
}

/// <summary>
/// 实体文件配置
/// </summary>
public record EntityFileConfig(
    string EntityType,
    Func<object, string> GetFileUrl,
    Func<object, string> GetEntityId,
    Func<object, string> GetEntityName,
    string FileType,
    string FileDescription);
