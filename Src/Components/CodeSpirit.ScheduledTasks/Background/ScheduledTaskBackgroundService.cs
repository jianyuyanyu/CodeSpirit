using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Background;

/// <summary>
/// 定时任务后台调度服务
/// </summary>
public class ScheduledTaskBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ScheduledTaskBackgroundService> _logger;
    private readonly ScheduledTasksOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceScopeFactory">服务范围工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public ScheduledTaskBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ScheduledTaskBackgroundService> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 执行后台服务
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("定时任务服务已禁用");
            return;
        }

        _logger.LogInformation("定时任务后台服务启动 - 扫描间隔: {ScanInterval}", _options.ScanInterval);

        // 启动时加载配置文件中的任务
        await LoadConfigurationTasksAsync();

        // 启动清理任务
        _ = Task.Run(() => CleanupTaskAsync(stoppingToken), stoppingToken);

        // 主调度循环
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndExecuteTasksAsync(stoppingToken);
                
                // 等待下次扫描
                await Task.Delay(_options.ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("定时任务后台服务正在停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务扫描过程中发生异常");
                
                // 发生异常时等待较长时间再重试
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("定时任务后台服务已停止");
    }

    /// <summary>
    /// 加载配置文件中的任务
    /// </summary>
    private async Task LoadConfigurationTasksAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<IScheduledTaskService>();
            
            var loadedCount = await taskService.LoadTasksFromConfigurationAsync();
            
            if (loadedCount > 0)
            {
                _logger.LogInformation("从配置文件加载任务完成 - 数量: {Count}", loadedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置文件任务失败");
        }
    }

    /// <summary>
    /// 扫描并执行到期任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ScanAndExecuteTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<IScheduledTaskService>();
        var taskExecutor = scope.ServiceProvider.GetRequiredService<ITaskExecutor>();

        try
        {
            // 获取所有启用的任务
            var enabledTasks = await taskService.GetEnabledTasksAsync(cancellationToken);
            
            if (!enabledTasks.Any())
            {
                return;
            }

            var currentTime = DateTime.UtcNow;
            var dueTasks = enabledTasks
                .Where(t => t.NextExecuteTime.HasValue && t.NextExecuteTime.Value <= currentTime)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.NextExecuteTime)
                .ToList();

            if (!dueTasks.Any())
            {
                return;
            }

            _logger.LogDebug("发现到期任务 - 数量: {Count}", dueTasks.Count);

            // 检查并发限制
            var runningTasks = await taskExecutor.GetRunningExecutionsAsync();
            var availableSlots = _options.MaxConcurrentTasks - runningTasks.Count;

            if (availableSlots <= 0)
            {
                _logger.LogWarning("已达到最大并发任务数限制 - 当前运行: {Running}, 最大并发: {Max}", 
                    runningTasks.Count, _options.MaxConcurrentTasks);
                return;
            }

            // 执行到期任务
            var tasksToExecute = dueTasks.Take(availableSlots).ToList();
            var executionTasks = new List<Task>();

            foreach (var task in tasksToExecute)
            {
                // 检查任务是否已在执行
                if (await taskExecutor.IsTaskRunningAsync(task.Id))
                {
                    _logger.LogDebug("任务已在执行中，跳过 - TaskId: {TaskId}", task.Id);
                    continue;
                }

                // 异步执行任务
                var executionTask = ExecuteTaskAsync(task, taskService, taskExecutor, cancellationToken);
                executionTasks.Add(executionTask);

                _logger.LogInformation("触发任务执行 - TaskId: {TaskId}, Name: {Name}, NextTime: {NextTime}", 
                    task.Id, task.Name, task.NextExecuteTime);
            }

            // 等待所有任务启动（不等待执行完成）
            if (executionTasks.Any())
            {
                await Task.WhenAll(executionTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描执行任务时发生异常");
        }
    }

    /// <summary>
    /// 执行单个任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="taskService">任务服务</param>
    /// <param name="taskExecutor">任务执行器</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ExecuteTaskAsync(
        ScheduledTask task, 
        IScheduledTaskService taskService, 
        ITaskExecutor taskExecutor, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 执行任务
            var execution = await taskExecutor.ExecuteAsync(task, cancellationToken);

            // 更新任务执行统计
            task.ExecutionCount++;
            task.LastExecuteTime = execution.StartTime;

            // 计算下次执行时间
            if (task.Type == TaskType.OneTime)
            {
                // 一次性任务执行后自动禁用
                task.Status = TaskStatus.Disabled;
                task.NextExecuteTime = null;
            }
            else
            {
                // 更新下次执行时间
                await taskService.UpdateNextExecuteTimeAsync(task.Id, cancellationToken);
            }

            // 保存任务状态
            await taskService.UpdateTaskAsync(task, cancellationToken);

            _logger.LogInformation("任务执行完成 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Status: {Status}, Duration: {Duration}",
                task.Id, execution.Id, execution.Status, execution.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行任务时发生异常 - TaskId: {TaskId}", task.Id);
        }
    }

    /// <summary>
    /// 清理任务（定期清理过期的执行记录等）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task CleanupTaskAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动定时任务清理服务 - 清理间隔: {CleanupInterval}", _options.TaskCleanupInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.TaskCleanupInterval, cancellationToken);

                using var scope = _serviceScopeFactory.CreateScope();
                
                // 这里可以添加清理逻辑，比如：
                // 1. 清理过期的执行记录
                // 2. 清理已完成的一次性任务
                // 3. 更新任务统计信息
                // 4. 检查任务健康状态

                await PerformCleanupAsync(scope.ServiceProvider, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行清理任务时发生异常");
            }
        }

        _logger.LogInformation("定时任务清理服务已停止");
    }

    /// <summary>
    /// 执行清理操作
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task PerformCleanupAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            // 清理已完成的一次性任务
            await CleanupCompletedOneTimeTasksAsync(serviceProvider, cancellationToken);

            // 这里可以添加更多清理逻辑
            _logger.LogDebug("定时任务清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行清理操作时发生异常");
        }
    }

    /// <summary>
    /// 清理已完成的一次性任务
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task CleanupCompletedOneTimeTasksAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            var taskService = serviceProvider.GetRequiredService<IScheduledTaskService>();
            var allTasks = await taskService.GetAllTasksAsync(cancellationToken);

            var completedOneTimeTasks = allTasks
                .Where(t => t.Type == TaskType.OneTime && 
                           t.Status == TaskStatus.Disabled && 
                           t.LastExecuteTime.HasValue &&
                           !t.IsFromConfiguration) // 不删除配置文件中的任务
                .Where(t => t.LastExecuteTime.HasValue && DateTime.UtcNow - t.LastExecuteTime.Value > TimeSpan.FromDays(1)) // 执行完成超过1天
                .ToList();

            foreach (var task in completedOneTimeTasks)
            {
                try
                {
                    await taskService.DeleteTaskAsync(task.Id, cancellationToken);
                    _logger.LogInformation("清理已完成的一次性任务 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理一次性任务失败 - TaskId: {TaskId}", task.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理一次性任务时发生异常");
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止定时任务后台服务...");
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("定时任务后台服务已停止");
    }
}
