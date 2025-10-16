using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Helpers;
using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 定时任务服务实现
/// </summary>
public class ScheduledTaskService : IScheduledTaskService, IScheduledTaskQueryService
{
    private readonly ICacheService _cacheService;
    private readonly ITaskExecutor _taskExecutor;
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly ScheduledTasksOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="taskExecutor">任务执行器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public ScheduledTaskService(
        ICacheService cacheService,
        ITaskExecutor taskExecutor,
        ILogger<ScheduledTaskService> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _cacheService = cacheService;
        _taskExecutor = taskExecutor;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 创建定时任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的任务</returns>
    public async Task<ScheduledTask> CreateTaskAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
        {
            task.Id = Guid.NewGuid().ToString();
        }

        // 验证任务配置
        ValidateTask(task);

        // 计算下次执行时间
        await UpdateNextExecuteTimeInternalAsync(task);

        // 保存任务
        await SaveTaskAsync(task);

        // 更新任务索引
        await UpdateTaskIndexAsync();

        _logger.LogInformation("创建定时任务成功 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);

        return task;
    }

    /// <summary>
    /// 更新定时任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的任务</returns>
    public async Task<ScheduledTask?> UpdateTaskAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        var existingTask = await GetTaskAsync(task.Id, cancellationToken);
        if (existingTask == null)
        {
            return null;
        }

        // 验证任务配置
        ValidateTask(task);

        // 更新时间戳
        task.UpdatedAt = DateTime.UtcNow;

        // 如果是配置文件任务，不允许修改某些字段
        if (existingTask.IsFromConfiguration)
        {
            task.IsFromConfiguration = true;
        }

        // 计算下次执行时间
        await UpdateNextExecuteTimeInternalAsync(task);

        // 保存任务
        await SaveTaskAsync(task);

        // 更新任务索引
        await UpdateTaskIndexAsync();

        _logger.LogInformation("更新定时任务成功 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);

        return task;
    }

    /// <summary>
    /// 删除定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        // 配置文件任务不允许删除
        if (task.IsFromConfiguration)
        {
            throw new InvalidOperationException("配置文件定义的任务不允许删除");
        }

        // 检查任务是否正在执行
        if (await _taskExecutor.IsTaskRunningAsync(taskId))
        {
            throw new InvalidOperationException("任务正在执行中，无法删除");
        }

        // 删除任务
        var cacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);

        // 更新任务索引
        await UpdateTaskIndexAsync();

        _logger.LogInformation("删除定时任务成功 - TaskId: {TaskId}", taskId);

        return true;
    }

    /// <summary>
    /// 启用定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启用成功</returns>
    public async Task<bool> EnableTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        task.Status = TaskStatus.Enabled;
        task.UpdatedAt = DateTime.UtcNow;

        // 计算下次执行时间
        await UpdateNextExecuteTimeInternalAsync(task);

        await SaveTaskAsync(task);
        await UpdateTaskIndexAsync();

        _logger.LogInformation("启用定时任务成功 - TaskId: {TaskId}", taskId);

        return true;
    }

    /// <summary>
    /// 禁用定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否禁用成功</returns>
    public async Task<bool> DisableTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        task.Status = TaskStatus.Disabled;
        task.UpdatedAt = DateTime.UtcNow;
        task.NextExecuteTime = null;

        await SaveTaskAsync(task);
        await UpdateTaskIndexAsync();

        _logger.LogInformation("禁用定时任务成功 - TaskId: {TaskId}", taskId);

        return true;
    }

    /// <summary>
    /// 获取定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务信息</returns>
    public async Task<ScheduledTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        return await _cacheService.GetAsync<ScheduledTask>(cacheKey, cancellationToken);
    }

    /// <summary>
    /// 获取所有定时任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    public async Task<List<ScheduledTask>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        var indexKey = $"{_options.CacheKeyPrefix}Index:All";
        var taskIds = await _cacheService.GetAsync<List<string>>(indexKey, cancellationToken) ?? new List<string>();

        var tasks = new List<ScheduledTask>();
        foreach (var taskId in taskIds)
        {
            var task = await GetTaskAsync(taskId, cancellationToken);
            if (task != null)
            {
                tasks.Add(task);
            }
        }

        return tasks.OrderBy(t => t.Name).ToList();
    }

    /// <summary>
    /// 获取启用的定时任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    public async Task<List<ScheduledTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default)
    {
        var allTasks = await GetAllTasksAsync(cancellationToken);
        return allTasks.Where(t => t.IsEnabled).ToList();
    }

    /// <summary>
    /// 手动触发任务执行
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行ID</returns>
    public async Task<string> TriggerTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            throw new InvalidOperationException($"任务不存在: {taskId}");
        }

        // 检查任务是否正在执行
        if (await _taskExecutor.IsTaskRunningAsync(taskId))
        {
            throw new InvalidOperationException("任务正在执行中，无法重复触发");
        }

        var execution = await _taskExecutor.ExecuteAsync(task, cancellationToken);

        _logger.LogInformation("手动触发任务执行 - TaskId: {TaskId}, ExecutionId: {ExecutionId}", taskId, execution.Id);

        return execution.Id;
    }

    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否取消成功</returns>
    public async Task<bool> CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default)
    {
        return await _taskExecutor.CancelAsync(executionId, cancellationToken);
    }

    /// <summary>
    /// 更新任务的下次执行时间
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateNextExecuteTimeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            return false;
        }

        await UpdateNextExecuteTimeInternalAsync(task);
        await SaveTaskAsync(task);

        return true;
    }

    /// <summary>
    /// 从配置文件加载任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>加载的任务数量</returns>
    public async Task<int> LoadTasksFromConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var loadedCount = 0;

        foreach (var taskDefinition in _options.Tasks)
        {
            try
            {
                var task = taskDefinition.ToScheduledTask();
                
                // 检查任务是否已存在
                var existingTask = await GetTaskAsync(task.Id, cancellationToken);
                if (existingTask != null)
                {
                    // 更新现有任务（保留运行时状态）
                    task.Status = existingTask.Status;
                    task.ExecutionCount = existingTask.ExecutionCount;
                    task.LastExecuteTime = existingTask.LastExecuteTime;
                }

                await UpdateNextExecuteTimeInternalAsync(task);
                await SaveTaskAsync(task);
                
                loadedCount++;
                
                _logger.LogDebug("从配置加载任务 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载配置任务失败 - TaskId: {TaskId}", taskDefinition.Id);
            }
        }

        if (loadedCount > 0)
        {
            await UpdateTaskIndexAsync();
        }

        _logger.LogInformation("从配置文件加载任务完成 - 加载数量: {Count}", loadedCount);

        return loadedCount;
    }

    #region 查询服务实现

    /// <summary>
    /// 分页查询定时任务
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    public async Task<PageList<ScheduledTask>> GetTasksPagedAsync(TaskQueryDto queryDto, CancellationToken cancellationToken = default)
    {
        var allTasks = await GetAllTasksAsync(cancellationToken);

        // 应用筛选条件
        var filteredTasks = allTasks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryDto.Name))
        {
            filteredTasks = filteredTasks.Where(t => t.Name.Contains(queryDto.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (queryDto.Status.HasValue)
        {
            filteredTasks = filteredTasks.Where(t => t.Status == queryDto.Status.Value);
        }

        if (queryDto.Type.HasValue)
        {
            filteredTasks = filteredTasks.Where(t => t.Type == queryDto.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryDto.Group))
        {
            filteredTasks = filteredTasks.Where(t => t.Group == queryDto.Group);
        }

        if (queryDto.IsFromConfiguration.HasValue)
        {
            filteredTasks = filteredTasks.Where(t => t.IsFromConfiguration == queryDto.IsFromConfiguration.Value);
        }

        var totalCount = filteredTasks.Count();
        
        // 排序
        IEnumerable<ScheduledTask> sortedTasks;
        if (!string.IsNullOrWhiteSpace(queryDto.OrderBy))
        {
            sortedTasks = queryDto.OrderDir?.ToLower() == "desc"
                ? filteredTasks.OrderByDescending(GetSortExpression(queryDto.OrderBy))
                : filteredTasks.OrderBy(GetSortExpression(queryDto.OrderBy));
        }
        else
        {
            sortedTasks = filteredTasks.OrderBy(t => t.Name);
        }

        var pagedTasks = sortedTasks
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToList();

        return new PageList<ScheduledTask>(pagedTasks, totalCount);
    }

    /// <summary>
    /// 获取任务执行历史
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行历史</returns>
    public async Task<PageList<TaskExecution>> GetExecutionHistoryAsync(string taskId, QueryDtoBase queryDto, CancellationToken cancellationToken = default)
    {
        var executionQuery = new ExecutionQueryDto
        {
            TaskId = taskId
        };
        executionQuery.Page = queryDto.Page;
        executionQuery.PerPage = queryDto.PerPage;
        executionQuery.OrderBy = queryDto.OrderBy;
        executionQuery.OrderDir = queryDto.OrderDir;

        return await GetAllExecutionHistoryAsync(executionQuery, cancellationToken);
    }

    /// <summary>
    /// 获取所有执行历史
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行历史</returns>
    public async Task<PageList<TaskExecution>> GetAllExecutionHistoryAsync(ExecutionQueryDto queryDto, CancellationToken cancellationToken = default)
    {
        // 这里简化实现，实际应该从缓存中获取执行历史
        // 由于缓存中的执行记录可能很多，这里只返回正在执行的任务作为示例
        var runningExecutions = await _taskExecutor.GetRunningExecutionsAsync();
        
        var filteredExecutions = runningExecutions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryDto.TaskId))
        {
            filteredExecutions = filteredExecutions.Where(e => e.TaskId == queryDto.TaskId);
        }

        if (queryDto.Status.HasValue)
        {
            filteredExecutions = filteredExecutions.Where(e => e.Status == queryDto.Status.Value);
        }

        var totalCount = filteredExecutions.Count();
        var pagedExecutions = filteredExecutions
            .OrderByDescending(e => e.StartTime)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToList();

        return new PageList<TaskExecution>(pagedExecutions, totalCount);
    }

    /// <summary>
    /// 获取任务统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    public async Task<TaskStatistics> GetTaskStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var allTasks = await GetAllTasksAsync(cancellationToken);
        var runningExecutions = await _taskExecutor.GetRunningExecutionsAsync();

        var statistics = new TaskStatistics
        {
            TotalTasks = allTasks.Count,
            EnabledTasks = allTasks.Count(t => t.Status == Models.TaskStatus.Enabled),
            DisabledTasks = allTasks.Count(t => t.Status == Models.TaskStatus.Disabled),
            RunningTasks = runningExecutions.Count
        };

        // 按状态统计
        foreach (var status in Enum.GetValues<Models.TaskStatus>())
        {
            statistics.StatusStatistics[status] = allTasks.Count(t => t.Status == status);
        }

        // 按类型统计
        foreach (var type in Enum.GetValues<Models.TaskType>())
        {
            statistics.TypeStatistics[type] = allTasks.Count(t => t.Type == type);
        }

        return statistics;
    }

    /// <summary>
    /// 获取正在执行的任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行中的任务</returns>
    public async Task<List<TaskExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default)
    {
        return await _taskExecutor.GetRunningExecutionsAsync();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 验证任务配置
    /// </summary>
    /// <param name="task">任务信息</param>
    private static void ValidateTask(ScheduledTask task)
    {
        if (string.IsNullOrWhiteSpace(task.Name))
        {
            throw new ArgumentException("任务名称不能为空");
        }

        if (string.IsNullOrWhiteSpace(task.HandlerType))
        {
            throw new ArgumentException("任务处理器类型不能为空");
        }

        switch (task.Type)
        {
            case TaskType.Cron:
                if (string.IsNullOrWhiteSpace(task.CronExpression))
                {
                    throw new ArgumentException("Cron任务必须指定Cron表达式");
                }
                if (!CronHelper.IsValidCronExpression(task.CronExpression))
                {
                    throw new ArgumentException("无效的Cron表达式");
                }
                break;

            case TaskType.Delay:
                if (!task.DelayTime.HasValue || task.DelayTime.Value <= TimeSpan.Zero)
                {
                    throw new ArgumentException("延迟任务必须指定有效的延迟时间");
                }
                break;

            case TaskType.OneTime:
                if (!task.ExecuteAt.HasValue || task.ExecuteAt.Value <= DateTime.UtcNow)
                {
                    throw new ArgumentException("一次性任务必须指定未来的执行时间");
                }
                break;
        }
    }

    /// <summary>
    /// 更新任务的下次执行时间
    /// </summary>
    /// <param name="task">任务信息</param>
    private static async Task UpdateNextExecuteTimeInternalAsync(ScheduledTask task)
    {
        if (task.Status != TaskStatus.Enabled)
        {
            task.NextExecuteTime = null;
            return;
        }

        switch (task.Type)
        {
            case TaskType.Cron:
                if (!string.IsNullOrWhiteSpace(task.CronExpression))
                {
                    task.NextExecuteTime = CronHelper.GetNextOccurrence(task.CronExpression);
                }
                break;

            case TaskType.Delay:
                if (task.DelayTime.HasValue)
                {
                    task.NextExecuteTime = DateTime.UtcNow.Add(task.DelayTime.Value);
                }
                break;

            case TaskType.OneTime:
                task.NextExecuteTime = task.ExecuteAt;
                break;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 保存任务到缓存
    /// </summary>
    /// <param name="task">任务信息</param>
    private async Task SaveTaskAsync(ScheduledTask task)
    {
        var cacheKey = $"{_options.CacheKeyPrefix}Tasks:{task.Id}";
        await _cacheService.SetAsync(cacheKey, task);
        
        // 更新索引，确保任务ID在索引中
        await AddTaskToIndexAsync(task.Id);
    }
    
    /// <summary>
    /// 将任务ID添加到索引中
    /// </summary>
    /// <param name="taskId">任务ID</param>
    private async Task AddTaskToIndexAsync(string taskId)
    {
        try
        {
            var indexKey = $"{_options.CacheKeyPrefix}Index:All";
            var taskIds = await _cacheService.GetAsync<List<string>>(indexKey) ?? new List<string>();
            
            if (!taskIds.Contains(taskId))
            {
                taskIds.Add(taskId);
                await _cacheService.SetAsync(indexKey, taskIds);
                _logger.LogDebug("任务ID已添加到索引 - TaskId: {TaskId}", taskId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加任务到索引失败 - TaskId: {TaskId}", taskId);
        }
    }

    /// <summary>
    /// 更新任务索引
    /// </summary>
    private async Task UpdateTaskIndexAsync()
    {
        try
        {
            // 获取当前索引中的所有任务ID
            var indexKey = $"{_options.CacheKeyPrefix}Index:All";
            var currentTaskIds = await _cacheService.GetAsync<List<string>>(indexKey) ?? new List<string>();
            
            // 验证每个任务是否仍然存在，移除不存在的任务
            var validTaskIds = new List<string>();
            foreach (var taskId in currentTaskIds)
            {
                var taskKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
                var task = await _cacheService.GetAsync<ScheduledTask>(taskKey);
                if (task != null)
                {
                    validTaskIds.Add(taskId);
                }
            }
            
            // 更新索引
            await _cacheService.SetAsync(indexKey, validTaskIds);
            
            _logger.LogDebug("更新任务索引完成 - 任务数量: {Count}", validTaskIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务索引失败");
        }
    }

    /// <summary>
    /// 获取排序表达式
    /// </summary>
    /// <param name="sortBy">排序字段</param>
    /// <returns>排序表达式</returns>
    private static Func<ScheduledTask, object> GetSortExpression(string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "name" => t => t.Name,
            "status" => t => t.Status,
            "type" => t => t.Type,
            "createdat" => t => t.CreatedAt,
            "updatedat" => t => t.UpdatedAt,
            "nextexecutetime" => t => t.NextExecuteTime ?? DateTime.MaxValue,
            "priority" => t => t.Priority,
            _ => t => t.Name
        };
    }

    #endregion
}
