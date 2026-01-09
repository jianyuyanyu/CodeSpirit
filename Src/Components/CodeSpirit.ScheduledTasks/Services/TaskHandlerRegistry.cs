using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务处理器注册表实现（基于Redis）
/// </summary>
public class TaskHandlerRegistry : ITaskHandlerRegistry
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ScheduledTasksOptions _options;
    private readonly ILogger<TaskHandlerRegistry> _logger;
    
    private const string RegistryKeyPrefix = "ScheduledTasks:Registry:";
    private const string TaskServiceKeyPrefix = "ScheduledTasks:TaskService:";
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceScopeFactory">服务作用域工厂</param>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志记录器</param>
    public TaskHandlerRegistry(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ScheduledTasksOptions> options,
        ILogger<TaskHandlerRegistry> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行缓存操作（自动管理作用域）
    /// </summary>
    private async Task<T> ExecuteWithCacheServiceAsync<T>(Func<ICacheService, Task<T>> operation)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetService<ICacheService>();
        if (cacheService == null)
        {
            throw new InvalidOperationException($"Unable to resolve service for type '{typeof(ICacheService)}'.");
        }
        return await operation(cacheService);
    }
    
    /// <summary>
    /// 执行缓存操作（自动管理作用域，无返回值）
    /// </summary>
    private async Task ExecuteWithCacheServiceAsync(Func<ICacheService, Task> operation)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetService<ICacheService>();
        if (cacheService == null)
        {
            throw new InvalidOperationException($"Unable to resolve service for type '{typeof(ICacheService)}'.");
        }
        await operation(cacheService);
    }
    
    /// <summary>
    /// 注册任务处理器
    /// </summary>
    public async Task RegisterHandlersAsync(string serviceName, IEnumerable<string> handlerTypes, CancellationToken cancellationToken = default)
    {
        try
        {
            var handlerList = handlerTypes.ToList();
            if (!handlerList.Any())
            {
                _logger.LogWarning("服务 {ServiceName} 没有注册任何任务处理器", serviceName);
                return;
            }
            
            var registryKey = $"{_options.CacheKeyPrefix}{RegistryKeyPrefix}{serviceName}";
            var json = JsonSerializer.Serialize(handlerList);
            
            await ExecuteWithCacheServiceAsync(async cacheService =>
            {
                await cacheService.SetAsync(registryKey, json, new CodeSpirit.Caching.Models.CacheOptions
                {
                    Level = CodeSpirit.Caching.Models.CacheLevel.L2Only, // 分布式环境仅使用Redis缓存
                    AbsoluteExpiration = null // 永久存储，直到服务重启
                }, cancellationToken);
            });
            
            _logger.LogInformation("✅ 服务 {ServiceName} 注册了 {Count} 个任务处理器: {Handlers}", 
                serviceName, handlerList.Count, string.Join(", ", handlerList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册任务处理器失败 - ServiceName: {ServiceName}", serviceName);
            throw;
        }
    }
    
    /// <summary>
    /// 注册任务所属服务
    /// </summary>
    public async Task RegisterTaskServiceAsync(string taskId, string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var taskServiceKey = $"{_options.CacheKeyPrefix}{TaskServiceKeyPrefix}{taskId}";
            
            await ExecuteWithCacheServiceAsync(async cacheService =>
            {
                await cacheService.SetAsync(taskServiceKey, serviceName, new CodeSpirit.Caching.Models.CacheOptions
                {
                    Level = CodeSpirit.Caching.Models.CacheLevel.L2Only, // 分布式环境仅使用Redis缓存
                    AbsoluteExpiration = null // 永久存储
                }, cancellationToken);
            });
            
            _logger.LogInformation("✅ 注册任务所属服务 - TaskId: {TaskId}, ServiceName: {ServiceName}", taskId, serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册任务所属服务失败 - TaskId: {TaskId}, ServiceName: {ServiceName}", taskId, serviceName);
            throw;
        }
    }
    
    /// <summary>
    /// 查询任务所属服务
    /// </summary>
    public async Task<string?> GetTaskServiceNameAsync(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var taskServiceKey = $"{_options.CacheKeyPrefix}{TaskServiceKeyPrefix}{taskId}";
            return await ExecuteWithCacheServiceAsync(async cacheService =>
            {
                return await cacheService.GetAsync<string>(taskServiceKey, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询任务所属服务失败 - TaskId: {TaskId}", taskId);
            return null;
        }
    }
    
    /// <summary>
    /// 检查任务是否属于指定服务
    /// </summary>
    public async Task<bool> IsTaskOwnedByServiceAsync(string taskId, string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            // 首先检查 Redis 中的注册信息
            var registeredService = await GetTaskServiceNameAsync(taskId, cancellationToken);
            if (!string.IsNullOrEmpty(registeredService))
            {
                return registeredService == serviceName;
            }

            // 如果 Redis 中没有注册信息，尝试从任务模型中获取 TargetService
            // 这是一个备选方案，用于处理任务创建后但尚未注册的情况
            var task = await GetTaskFromCacheAsync(taskId, cancellationToken);
            if (task != null)
            {
                // 如果任务指定了 TargetService，检查是否匹配
                if (!string.IsNullOrEmpty(task.TargetService))
                {
                    return task.TargetService == serviceName;
                }

                // 如果任务没有指定 TargetService，则任何服务都可以执行
                // 但为了避免重复执行，我们自动注册到当前服务
                await RegisterTaskServiceAsync(taskId, serviceName, cancellationToken);
                _logger.LogInformation("任务未注册服务映射，自动注册到当前服务 - TaskId: {TaskId}, ServiceName: {ServiceName}", taskId, serviceName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查任务所属服务失败 - TaskId: {TaskId}, ServiceName: {ServiceName}", taskId, serviceName);
            return false;
        }
    }

    /// <summary>
    /// 从缓存获取任务信息
    /// </summary>
    private async Task<ScheduledTask?> GetTaskFromCacheAsync(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
            return await ExecuteWithCacheServiceAsync(async cacheService =>
            {
                return await cacheService.GetAsync<ScheduledTask>(cacheKey, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存获取任务失败 - TaskId: {TaskId}", taskId);
            return null;
        }
    }
    
    /// <summary>
    /// 获取服务注册的所有处理器类型
    /// </summary>
    public async Task<List<string>> GetServiceHandlersAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var registryKey = $"{_options.CacheKeyPrefix}{RegistryKeyPrefix}{serviceName}";
            var json = await ExecuteWithCacheServiceAsync(async cacheService =>
            {
                return await cacheService.GetAsync<string>(registryKey, cancellationToken);
            });
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<string>();
            }
            
            var handlers = JsonSerializer.Deserialize<List<string>>(json);
            return handlers ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务处理器列表失败 - ServiceName: {ServiceName}", serviceName);
            return new List<string>();
        }
    }
}

