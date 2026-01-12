using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Models;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Dto;
using CodeSpirit.ScheduledTasks.Helpers;
using CodeSpirit.ScheduledTasks.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务执行器实现
/// </summary>
public class TaskExecutor : ITaskExecutor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ICacheService _cacheService;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<TaskExecutor> _logger;
    private readonly ScheduledTasksOptions _options;
    
    /// <summary>
    /// 定时任务缓存选项 - 仅使用 L2 缓存，避免跨服务实例数据不一致
    /// </summary>
    private static readonly CacheOptions _l2CacheOptions = CacheOptions.L2NeverExpires();
    
    /// <summary>
    /// 正在执行的任务字典
    /// </summary>
    private readonly ConcurrentDictionary<string, TaskExecutionContext> _runningTasks = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceScopeFactory">服务作用域工厂（用于创建作用域来解析任务处理器及其依赖）</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="lockProvider">分布式锁提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public TaskExecutor(
        IServiceScopeFactory serviceScopeFactory,
        ICacheService cacheService,
        IDistributedLockProvider lockProvider,
        ILogger<TaskExecutor> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
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
    /// <param name="triggerType">触发类型，默认为 "Scheduled"</param>
    /// <returns>执行记录</returns>
    public async Task<TaskExecution> ExecuteAsync(ScheduledTask task, CancellationToken cancellationToken = default, string triggerType = "Scheduled")
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
            TriggerType = triggerType,
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
            // ✅ 创建新作用域来解析任务处理器
            // 虽然 TaskExecutor 是从作用域中解析的，但构造函数注入的 IServiceProvider 可能不是作用域的服务提供者
            // 为了确保 DbContext 等 Scoped 服务从正确的作用域中解析，我们为每次任务执行创建独立的作用域
            // 这样可以确保任务处理器及其依赖（如 DbContext）在整个任务执行期间都有效
            using var scope = _serviceScopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            // 分布式锁控制
            if (task.ExecutionStrategy == ExecutionStrategy.Distributed)
            {
                await ExecuteWithDistributedLockAsync(task, context, serviceProvider);
            }
            else
            {
                await ExecuteTaskInternalAsync(task, context, serviceProvider);
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
            
            // ✅ 发送任务执行通知
            await SendNotificationAsync(task, execution);
        }

        return execution;
    }

    /// <summary>
    /// 使用分布式锁执行任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="context">执行上下文</param>
    /// <param name="serviceProvider">服务提供者（从当前作用域获取）</param>
    private async Task ExecuteWithDistributedLockAsync(ScheduledTask task, TaskExecutionContext context, IServiceProvider serviceProvider)
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
            await ExecuteTaskInternalAsync(task, context, serviceProvider);
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
    /// <param name="serviceProvider">服务提供者（从当前作用域获取）</param>
    private async Task ExecuteTaskInternalAsync(ScheduledTask task, TaskExecutionContext context, IServiceProvider serviceProvider)
    {
        var execution = context.Execution;
        var timeout = task.Timeout ?? _options.DefaultTimeout;
        var maxRetryCount = task.MaxRetryCount;
        var retryInterval = task.RetryInterval ?? _options.DefaultRetryInterval;

        execution.AddLog($"开始执行任务处理器: {task.HandlerType}");
        execution.AddLog($"任务超时时间: {timeout}");
        if (maxRetryCount > 0)
        {
            execution.AddLog($"最大重试次数: {maxRetryCount}, 重试间隔: {retryInterval}");
        }

        try
        {
            // ✅ 从当前作用域的服务提供者获取任务处理器
            execution.AddLog($"开始获取任务处理器: {task.HandlerType}");
            var handler = GetTaskHandler(task.HandlerType, serviceProvider);
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

            // ✅ 执行任务（支持重试机制）
            string? result = null;
            Exception? lastException = null;
            var currentAttempt = 0;

            while (currentAttempt <= maxRetryCount)
            {
                try
                {
                    if (currentAttempt > 0)
                    {
                        execution.RetryCount = currentAttempt;
                        execution.AddLog($"第 {currentAttempt} 次重试开始");
                        _logger.LogInformation("任务重试 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Attempt: {Attempt}/{MaxRetry}",
                            task.Id, execution.Id, currentAttempt, maxRetryCount);
                        
                        // 保存重试状态
                        await SaveExecutionAsync(execution);
                    }

                    // 创建超时控制
                    using var timeoutCts = TaskTimeoutHelper.CreateTimeoutToken(timeout, context.CancellationTokenSource.Token);
                    context.TimeoutCancellationTokenSource = timeoutCts;

                    result = await TaskTimeoutHelper.ExecuteWithTimeoutAsync(
                        async (ct) => await handler.ExecuteAsync(task.Parameters, ct),
                        timeout,
                        context.CancellationTokenSource.Token);

                    // 执行成功，跳出重试循环
                    execution.MarkCompleted(result);
                    if (currentAttempt > 0)
                    {
                        execution.AddLog($"任务在第 {currentAttempt} 次重试后执行成功");
                    }
                    else
                    {
                        execution.AddLog("任务执行完成");
                    }

                    _logger.LogInformation("任务执行成功 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Duration: {Duration}, RetryCount: {RetryCount}",
                        task.Id, execution.Id, execution.Duration, execution.RetryCount);
                    return;
                }
                catch (TimeoutException ex)
                {
                    lastException = ex;
                    execution.AddLog($"第 {currentAttempt + 1} 次执行超时");
                    _logger.LogWarning("任务执行超时 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Attempt: {Attempt}/{MaxRetry}",
                        task.Id, execution.Id, currentAttempt + 1, maxRetryCount + 1);
                }
                catch (OperationCanceledException) when (context.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    // 取消操作不应重试
                    execution.MarkCancelled();
                    execution.AddLog("任务执行被取消");
                    _logger.LogInformation("任务执行被取消 - TaskId: {TaskId}, ExecutionId: {ExecutionId}",
                        task.Id, execution.Id);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    execution.AddLog($"第 {currentAttempt + 1} 次执行失败: {ex.Message}");
                    _logger.LogWarning(ex, "任务执行失败 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Attempt: {Attempt}/{MaxRetry}",
                        task.Id, execution.Id, currentAttempt + 1, maxRetryCount + 1);
                }

                currentAttempt++;

                // 如果还有重试机会，等待重试间隔
                if (currentAttempt <= maxRetryCount)
                {
                    execution.AddLog($"等待 {retryInterval} 后进行第 {currentAttempt} 次重试");
                    try
                    {
                        await Task.Delay(retryInterval, context.CancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        execution.MarkCancelled();
                        execution.AddLog("任务在等待重试期间被取消");
                        return;
                    }
                }
            }

            // 所有重试都失败了
            if (lastException is TimeoutException)
            {
                execution.MarkTimeout();
                execution.AddLog($"任务在 {maxRetryCount + 1} 次尝试后仍超时");
                _logger.LogWarning("任务执行超时（已用尽重试） - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Timeout: {Timeout}, RetryCount: {RetryCount}",
                    task.Id, execution.Id, timeout, execution.RetryCount);
            }
            else
            {
                execution.MarkFailed(lastException?.Message ?? "未知错误", lastException?.StackTrace);
                execution.AddLog($"任务在 {maxRetryCount + 1} 次尝试后仍失败");
                _logger.LogError(lastException, "任务执行失败（已用尽重试） - TaskId: {TaskId}, ExecutionId: {ExecutionId}, RetryCount: {RetryCount}",
                    task.Id, execution.Id, execution.RetryCount);
            }
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
    /// <param name="serviceProvider">服务提供者（从当前作用域获取，确保 DbContext 等 Scoped 服务正确解析）</param>
    /// <returns>任务处理器实例</returns>
    private ITaskHandler? GetTaskHandler(string handlerTypeName, IServiceProvider serviceProvider)
    {
        try
        {
            _logger.LogDebug("开始查找任务处理器: {HandlerType}", handlerTypeName);
            
            // 解析类型名称（移除程序集名称部分，如果存在）
            var pureTypeName = handlerTypeName;
            var commaIndex = handlerTypeName.IndexOf(',');
            if (commaIndex > 0)
            {
                pureTypeName = handlerTypeName.Substring(0, commaIndex).Trim();
            }
            
            // 尝试直接获取类型
            var handlerType = Type.GetType(pureTypeName);
            
            // 如果直接获取失败，尝试在当前程序集中搜索
            if (handlerType == null)
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    handlerType = entryAssembly.GetType(pureTypeName);
                }
            }
            
            // 如果仍然找不到，尝试在所有已加载的程序集中搜索
            if (handlerType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        handlerType = assembly.GetType(pureTypeName);
                        if (handlerType != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略单个程序集的异常
                    }
                }
            }

            if (handlerType != null)
            {
                _logger.LogDebug("找到类型: {TypeName}", handlerType.FullName);
                
                // ✅ 从传入的服务提供者（当前作用域）获取任务处理器
                // 这确保 DbContext 等 Scoped 服务从正确的作用域中解析
                var handler = serviceProvider.GetService(handlerType) as ITaskHandler;
                if (handler != null)
                {
                    _logger.LogDebug("从服务容器获取任务处理器成功: {HandlerType}", handlerTypeName);
                    return handler;
                }

                _logger.LogWarning("任务处理器类型已找到，但无法从服务容器获取: {HandlerType}", handlerTypeName);
            }
            else
            {
                _logger.LogWarning("未找到任务处理器类型: {HandlerType}", handlerTypeName);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建任务处理器时发生异常: {HandlerType}", handlerTypeName);
            return null;
        }
    }


    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否取消成功</returns>
    public Task<bool> CancelAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (_runningTasks.TryGetValue(executionId, out var context))
        {
            context.CancellationTokenSource.Cancel();
            context.Execution.AddLog("收到取消请求");
            
            _logger.LogInformation("任务执行被取消 - ExecutionId: {ExecutionId}", executionId);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
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
            var cacheOptions = new CodeSpirit.Caching.Models.CacheOptions
            {
                Level = CodeSpirit.Caching.Models.CacheLevel.L2Only, // 分布式环境仅使用Redis缓存
                AbsoluteExpiration = _options.ExecutionHistoryRetention
            };
            
            // 保存执行记录
            await _cacheService.SetAsync(cacheKey, execution, cacheOptions);
            
            // 更新执行记录索引（按 TaskId 索引）
            await UpdateExecutionIndexAsync(execution.TaskId, execution.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存执行记录失败 - ExecutionId: {ExecutionId}", execution.Id);
        }
    }

    /// <summary>
    /// 更新执行记录索引（按 TaskId 索引执行记录ID列表）
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="executionId">执行记录ID</param>
    private async Task UpdateExecutionIndexAsync(string taskId, string executionId)
    {
        try
        {
            var indexKey = $"{_options.CacheKeyPrefix}Index:Executions:{taskId}";
            // ✅ 使用 L2Only 缓存选项
            var executionIds = await _cacheService.GetAsync<List<string>>(indexKey, _l2CacheOptions) ?? new List<string>();
            
            // 如果执行记录ID不在索引中，添加到索引
            if (!executionIds.Contains(executionId))
            {
                executionIds.Add(executionId);
                // 限制索引大小，只保留最近的执行记录（例如最近1000条）
                if (executionIds.Count > 1000)
                {
                    executionIds = executionIds.Skip(executionIds.Count - 1000).ToList();
                }
                
                await _cacheService.SetAsync(indexKey, executionIds, new CacheOptions
                {
                    Level = CacheLevel.L2Only,
                    AbsoluteExpiration = _options.ExecutionHistoryRetention
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新执行记录索引失败 - TaskId: {TaskId}, ExecutionId: {ExecutionId}", taskId, executionId);
        }
    }

    /// <summary>
    /// 发送任务执行通知
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="execution">执行记录</param>
    private async Task SendNotificationAsync(ScheduledTask task, TaskExecution execution)
    {
        try
        {
            // 解析通知配置
            NotificationConfig? config = null;
            if (!string.IsNullOrEmpty(task.NotificationConfig))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<NotificationConfig>(task.NotificationConfig);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析通知配置失败 - TaskId: {TaskId}", task.Id);
                }
            }

            // 如果没有配置或未启用，跳过通知
            if (config == null || !config.Enabled)
            {
                return;
            }

            // 创建新作用域来获取通知器
            using var scope = _serviceScopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetService<ITaskExecutionNotifier>();
            
            if (notifier == null)
            {
                _logger.LogDebug("未注册任务执行通知器，跳过通知");
                return;
            }

            var isSuccess = execution.Status == TaskStatus.Completed;
            
            var notification = new TaskExecutionNotification
            {
                TaskId = task.Id,
                TaskName = task.Name,
                ExecutionId = execution.Id,
                Status = execution.Status.ToString(),
                IsSuccess = isSuccess,
                StartTime = execution.StartTime,
                EndTime = execution.EndTime,
                Duration = execution.Duration,
                Result = execution.Result,
                ErrorMessage = execution.ErrorMessage,
                ExecutionNode = execution.ExecutionNode,
                RetryCount = execution.RetryCount,
                Config = config
            };

            await notifier.NotifyAsync(notification);
        }
        catch (Exception ex)
        {
            // 通知失败不应影响任务执行结果
            _logger.LogError(ex, "发送任务执行通知失败 - TaskId: {TaskId}, ExecutionId: {ExecutionId}", 
                task.Id, execution.Id);
        }
    }
}
