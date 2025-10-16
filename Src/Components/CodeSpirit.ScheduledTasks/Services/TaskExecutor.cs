using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Helpers;
using CodeSpirit.ScheduledTasks.Models;
using System.Collections.Concurrent;
using System.Reflection;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务执行器实现
/// </summary>
public class TaskExecutor : ITaskExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICacheService _cacheService;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<TaskExecutor> _logger;
    private readonly ScheduledTasksOptions _options;
    
    /// <summary>
    /// 正在执行的任务字典
    /// </summary>
    private readonly ConcurrentDictionary<string, TaskExecutionContext> _runningTasks = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="lockProvider">分布式锁提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public TaskExecutor(
        IServiceProvider serviceProvider,
        ICacheService cacheService,
        IDistributedLockProvider lockProvider,
        ILogger<TaskExecutor> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _serviceProvider = serviceProvider;
        _cacheService = cacheService;
        _lockProvider = lockProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行记录</returns>
    public async Task<TaskExecution> ExecuteAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var execution = new TaskExecution
        {
            Id = executionId,
            TaskId = task.Id,
            TaskName = task.Name,
            Status = TaskStatus.Running,
            StartTime = DateTime.UtcNow,
            Parameters = task.Parameters,
            TriggerType = "Scheduled",
            ExecutionNode = Environment.MachineName
        };

        execution.AddLog("开始执行任务");
        
        // 保存执行记录到缓存
        await SaveExecutionAsync(execution);

        var context = new TaskExecutionContext
        {
            Execution = execution,
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        };

        _runningTasks.TryAdd(executionId, context);

        try
        {
            // 分布式锁控制
            if (task.ExecutionStrategy == ExecutionStrategy.Distributed)
            {
                await ExecuteWithDistributedLockAsync(task, context);
            }
            else
            {
                await ExecuteTaskInternalAsync(task, context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行异常 - TaskId: {TaskId}, ExecutionId: {ExecutionId}", task.Id, executionId);
            execution.MarkFailed(ex.Message, ex.StackTrace);
        }
        finally
        {
            _runningTasks.TryRemove(executionId, out _);
            await SaveExecutionAsync(execution);
        }

        return execution;
    }

    /// <summary>
    /// 使用分布式锁执行任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="context">执行上下文</param>
    private async Task ExecuteWithDistributedLockAsync(ScheduledTask task, TaskExecutionContext context)
    {
        var lockKey = $"{_options.CacheKeyPrefix}Lock:{task.Id}";
        var timeout = task.Timeout ?? _options.DefaultTimeout;
        var lockTimeout = timeout.Add(TimeSpan.FromSeconds(30)); // 锁超时时间比任务超时时间多30秒

        context.Execution.AddLog($"尝试获取分布式锁: {lockKey}");

        using var distributedLock = await _lockProvider.TryAcquireLockAsync(lockKey, lockTimeout);
        if (distributedLock == null)
        {
            context.Execution.AddLog("获取分布式锁失败，任务可能正在其他节点执行");
            context.Execution.MarkFailed("获取分布式锁失败，任务可能正在其他节点执行");
            return;
        }

        context.Execution.AddLog("成功获取分布式锁");

        try
        {
            await ExecuteTaskInternalAsync(task, context);
        }
        finally
        {
            context.Execution.AddLog("释放分布式锁");
        }
    }

    /// <summary>
    /// 内部执行任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="context">执行上下文</param>
    private async Task ExecuteTaskInternalAsync(ScheduledTask task, TaskExecutionContext context)
    {
        var execution = context.Execution;
        var timeout = task.Timeout ?? _options.DefaultTimeout;

        execution.AddLog($"开始执行任务处理器: {task.HandlerType}");
        execution.AddLog($"任务超时时间: {timeout}");

        try
        {
            // 创建超时控制
            using var timeoutCts = TaskTimeoutHelper.CreateTimeoutToken(timeout, context.CancellationTokenSource.Token);
            context.TimeoutCancellationTokenSource = timeoutCts;

            // 获取任务处理器
            execution.AddLog($"开始获取任务处理器: {task.HandlerType}");
            var handler = GetTaskHandler(task.HandlerType);
            if (handler == null)
            {
                // 如果找不到任务处理器，说明此任务不属于当前服务，跳过执行
                _logger.LogWarning("任务处理器不在当前服务中，跳过执行 - TaskId: {TaskId}, HandlerType: {HandlerType}", 
                    task.Id, task.HandlerType);
                execution.MarkSkipped("任务处理器不在当前服务中");
                execution.AddLog("任务处理器未找到，跳过执行");
                return;
            }

            execution.AddLog("成功获取任务处理器");
            _logger.LogInformation("成功获取任务处理器 - TaskId: {TaskId}, HandlerType: {HandlerType}", 
                task.Id, task.HandlerType);

            // 执行任务
            var result = await TaskTimeoutHelper.ExecuteWithTimeoutAsync(
                async (ct) => await handler.ExecuteAsync(task.Parameters, ct),
                timeout,
                context.CancellationTokenSource.Token);

            execution.MarkCompleted(result);
            execution.AddLog("任务执行完成");

            _logger.LogInformation("任务执行成功 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Duration: {Duration}",
                task.Id, execution.Id, execution.Duration);
        }
        catch (TimeoutException)
        {
            execution.MarkTimeout();
            execution.AddLog("任务执行超时");
            _logger.LogWarning("任务执行超时 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Timeout: {Timeout}",
                task.Id, execution.Id, timeout);
        }
        catch (OperationCanceledException) when (context.CancellationTokenSource.Token.IsCancellationRequested)
        {
            execution.MarkCancelled();
            execution.AddLog("任务执行被取消");
            _logger.LogInformation("任务执行被取消 - TaskId: {TaskId}, ExecutionId: {ExecutionId}",
                task.Id, execution.Id);
        }
        catch (Exception ex)
        {
            execution.MarkFailed(ex.Message, ex.StackTrace);
            execution.AddLog($"任务执行异常: {ex.Message}");
            _logger.LogError(ex, "任务执行失败 - TaskId: {TaskId}, ExecutionId: {ExecutionId}",
                task.Id, execution.Id);
        }
    }

    /// <summary>
    /// 获取任务处理器
    /// </summary>
    /// <param name="handlerTypeName">处理器类型名称</param>
    /// <returns>任务处理器</returns>
    private ITaskHandler? GetTaskHandler(string handlerTypeName)
    {
        try
        {
            _logger.LogDebug("开始查找任务处理器: {HandlerType}", handlerTypeName);
            
            // 首先尝试直接获取类型
            var handlerType = Type.GetType(handlerTypeName);
            _logger.LogDebug("Type.GetType结果: {Result}", handlerType?.FullName ?? "null");
            
            // 如果直接获取失败，尝试在所有已加载的程序集中搜索
            if (handlerType == null)
            {
                _logger.LogDebug("直接获取类型失败，开始在已加载程序集中搜索");
                handlerType = FindTypeInLoadedAssemblies(handlerTypeName);
                _logger.LogDebug("程序集搜索结果: {Result}", handlerType?.FullName ?? "null");
            }

            if (handlerType != null)
            {
                _logger.LogDebug("找到类型，尝试从服务容器获取实例");
                
                // 尝试从服务容器获取
                var handler = _serviceProvider.GetService(handlerType) as ITaskHandler;
                if (handler != null)
                {
                    _logger.LogDebug("从服务容器获取任务处理器成功: {HandlerType}", handlerTypeName);
                    return handler;
                }

                _logger.LogDebug("从服务容器获取任务处理器失败，检查是否实现ITaskHandler接口");
                
                // 尝试通过反射创建（如果类型实现了ITaskHandler接口）
                if (typeof(ITaskHandler).IsAssignableFrom(handlerType))
                {
                    _logger.LogDebug("类型实现了ITaskHandler接口，尝试通过反射创建: {HandlerType}", handlerTypeName);
                    var instance = Activator.CreateInstance(handlerType) as ITaskHandler;
                    if (instance != null)
                    {
                        _logger.LogDebug("通过反射创建任务处理器成功: {HandlerType}", handlerTypeName);
                        return instance;
                    }
                    else
                    {
                        _logger.LogError("通过反射创建任务处理器失败: {HandlerType}", handlerTypeName);
                    }
                }
                else
                {
                    _logger.LogError("类型未实现ITaskHandler接口: {HandlerType}", handlerTypeName);
                }
            }

            // 如果找不到任务处理器，这是正常的（可能属于其他服务）
            _logger.LogDebug("在当前服务中未找到任务处理器: {HandlerType}", handlerTypeName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建任务处理器时发生异常: {HandlerType}", handlerTypeName);
            return null;
        }
    }

    /// <summary>
    /// 在已加载的程序集中查找类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>找到的类型，如果未找到则返回null</returns>
    private Type? FindTypeInLoadedAssemblies(string typeName)
    {
        try
        {
            // 在所有已加载的程序集中搜索类型
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        _logger.LogDebug("在程序集 {AssemblyName} 中找到类型: {TypeName}", assembly.FullName, typeName);
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    // 忽略单个程序集的异常，继续搜索其他程序集
                    _logger.LogDebug(ex, "在程序集 {AssemblyName} 中搜索类型 {TypeName} 时发生异常", assembly.FullName, typeName);
                }
            }

            _logger.LogWarning("在所有已加载的程序集中都未找到类型: {TypeName}", typeName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索类型时发生异常: {TypeName}", typeName);
            return null;
        }
    }

    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否取消成功</returns>
    public async Task<bool> CancelAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (_runningTasks.TryGetValue(executionId, out var context))
        {
            context.CancellationTokenSource.Cancel();
            context.Execution.AddLog("收到取消请求");
            
            _logger.LogInformation("任务执行被取消 - ExecutionId: {ExecutionId}", executionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取正在执行的任务
    /// </summary>
    /// <returns>执行中的任务列表</returns>
    public async Task<List<TaskExecution>> GetRunningExecutionsAsync()
    {
        var runningExecutions = _runningTasks.Values
            .Select(context => context.Execution)
            .ToList();

        return await Task.FromResult(runningExecutions);
    }

    /// <summary>
    /// 检查任务是否正在执行
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>是否正在执行</returns>
    public async Task<bool> IsTaskRunningAsync(string taskId)
    {
        var isRunning = _runningTasks.Values
            .Any(context => context.Execution.TaskId == taskId);

        return await Task.FromResult(isRunning);
    }

    /// <summary>
    /// 保存执行记录
    /// </summary>
    /// <param name="execution">执行记录</param>
    private async Task SaveExecutionAsync(TaskExecution execution)
    {
        try
        {
            var cacheKey = $"{_options.CacheKeyPrefix}Executions:{execution.Id}";
            await _cacheService.SetAsync(cacheKey, execution, new CodeSpirit.Caching.Models.CacheOptions
            {
                AbsoluteExpiration = _options.ExecutionHistoryRetention
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存执行记录失败 - ExecutionId: {ExecutionId}", execution.Id);
        }
    }
}

/// <summary>
/// 任务执行上下文
/// </summary>
internal class TaskExecutionContext
{
    /// <summary>
    /// 执行记录
    /// </summary>
    public TaskExecution Execution { get; set; } = null!;

    /// <summary>
    /// 取消令牌源
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;

    /// <summary>
    /// 超时取消令牌源
    /// </summary>
    public CancellationTokenSource? TimeoutCancellationTokenSource { get; set; }
}
