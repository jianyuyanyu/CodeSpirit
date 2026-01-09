using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Dto;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly ScheduledTasksOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="taskExecutor">任务执行器</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">配置选项</param>
    public ScheduledTaskService(
        ICacheService cacheService,
        ITaskExecutor taskExecutor,
        IServiceProvider serviceProvider,
        ILogger<ScheduledTaskService> logger,
        IOptions<ScheduledTasksOptions> options)
    {
        _cacheService = cacheService;
        _taskExecutor = taskExecutor;
        _serviceProvider = serviceProvider;
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

        // 如果未指定目标服务，使用当前服务名称
        if (string.IsNullOrEmpty(task.TargetService) && !string.IsNullOrEmpty(_options.ServiceName))
        {
            task.TargetService = _options.ServiceName;
        }

        // 计算下次执行时间
        await UpdateNextExecuteTimeInternalAsync(task);

        // 保存任务
        await SaveTaskAsync(task);

        // 更新任务索引
        await UpdateTaskIndexAsync();

        // 注册任务所属服务映射
        await RegisterTaskServiceMappingAsync(task, cancellationToken);

        _logger.LogInformation("创建定时任务成功 - TaskId: {TaskId}, Name: {Name}, TargetService: {TargetService}", 
            task.Id, task.Name, task.TargetService);

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

        // 更新任务所属服务映射（如果 TargetService 发生变化）
        if (existingTask.TargetService != task.TargetService)
        {
            await RegisterTaskServiceMappingAsync(task, cancellationToken);
        }

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

        if (!taskIds.Any())
        {
            return new List<ScheduledTask>();
        }

        // ✅ 使用批量获取优化性能，减少 Redis 调用次数
        var cacheKeys = taskIds.Select(id => $"{_options.CacheKeyPrefix}Tasks:{id}").ToList();
        var taskDict = await _cacheService.GetManyAsync<ScheduledTask>(cacheKeys, cancellationToken);

        var tasks = taskDict.Values
            .Where(t => t != null)
            .Select(t => t!)
            .OrderBy(t => t.Name)
            .ToList();

        return tasks;
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

        // 执行任务
        var execution = await _taskExecutor.ExecuteAsync(task, cancellationToken);

        _logger.LogInformation("手动触发任务执行成功 - TaskId: {TaskId}, ExecutionId: {ExecutionId}", taskId, execution.Id);

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
        
        // 获取注册表服务（如果可用）
        ITaskHandlerRegistry? registry = null;
        try
        {
            registry = _serviceProvider.GetService<ITaskHandlerRegistry>();
        }
        catch
        {
            // 注册表可能未注册，忽略
        }

        foreach (var taskDefinition in _options.Tasks)
        {
            try
            {
                // 从配置文件创建任务对象（包含最新配置）
                var task = taskDefinition.ToScheduledTask();
                
                // 如果未指定目标服务，使用当前服务名称
                if (string.IsNullOrEmpty(task.TargetService) && !string.IsNullOrEmpty(_options.ServiceName))
                {
                    task.TargetService = _options.ServiceName;
                }
                
                // 检查任务是否已存在
                var existingTask = await GetTaskAsync(task.Id, cancellationToken);
                if (existingTask != null)
                {
                    // ✅ 配置文件任务启动时覆盖：用配置文件的最新值覆盖所有配置项
                    // 但保留以下运行时状态：
                    task.Status = existingTask.Status;              // 保留用户手动启用/禁用的状态
                    task.ExecutionCount = existingTask.ExecutionCount; // 保留执行次数统计
                    task.LastExecuteTime = existingTask.LastExecuteTime; // 保留上次执行时间
                    task.CreatedAt = existingTask.CreatedAt;        // 保留原始创建时间
                    task.CreatedBy = existingTask.CreatedBy;        // 保留原始创建者
                    task.UpdatedAt = DateTime.UtcNow;               // 更新修改时间
                    
                    _logger.LogDebug("覆盖配置文件任务 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);
                }
                else
                {
                    _logger.LogDebug("新增配置文件任务 - TaskId: {TaskId}, Name: {Name}", task.Id, task.Name);
                }

                await UpdateNextExecuteTimeInternalAsync(task);
                await SaveTaskAsync(task);
                
                // ✅ 注册任务所属服务
                if (registry != null && !string.IsNullOrEmpty(_options.ServiceName))
                {
                    await registry.RegisterTaskServiceAsync(task.Id, _options.ServiceName, cancellationToken);
                }
                
                loadedCount++;
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
        var allExecutions = new List<TaskExecution>();
        
        // 如果指定了 TaskId，从索引中获取该任务的执行记录
        if (!string.IsNullOrWhiteSpace(queryDto.TaskId))
        {
            var indexKey = $"{_options.CacheKeyPrefix}Index:Executions:{queryDto.TaskId}";
            var executionIds = await _cacheService.GetAsync<List<string>>(indexKey, cancellationToken) ?? new List<string>();
            
            // ✅ 使用批量获取优化性能
            if (executionIds.Any())
            {
                var cacheKeys = executionIds.Select(id => $"{_options.CacheKeyPrefix}Executions:{id}").ToList();
                var executionDict = await _cacheService.GetManyAsync<TaskExecution>(cacheKeys, cancellationToken);
                allExecutions.AddRange(executionDict.Values.Where(e => e != null).Select(e => e!));
            }
        }
        else
        {
            // 如果没有指定 TaskId，获取所有任务的执行记录
            var allTasks = await GetAllTasksAsync(cancellationToken);
            
            // ✅ 先收集所有执行记录ID，然后批量获取
            var allExecutionIds = new List<string>();
            foreach (var task in allTasks)
            {
                var indexKey = $"{_options.CacheKeyPrefix}Index:Executions:{task.Id}";
                var executionIds = await _cacheService.GetAsync<List<string>>(indexKey, cancellationToken) ?? new List<string>();
                allExecutionIds.AddRange(executionIds.Select(id => $"{_options.CacheKeyPrefix}Executions:{id}"));
            }
            
            // 批量获取所有执行记录
            if (allExecutionIds.Any())
            {
                var executionDict = await _cacheService.GetManyAsync<TaskExecution>(allExecutionIds, cancellationToken);
                allExecutions.AddRange(executionDict.Values.Where(e => e != null).Select(e => e!));
            }
        }
        
        // 合并正在执行的任务（可能还没有保存到缓存）
        var runningExecutions = await _taskExecutor.GetRunningExecutionsAsync();
        foreach (var runningExecution in runningExecutions)
        {
            // 如果执行记录不在列表中，添加它
            if (!allExecutions.Any(e => e.Id == runningExecution.Id))
            {
                allExecutions.Add(runningExecution);
            }
            else
            {
                // 如果已在列表中，更新为最新的状态（正在执行的任务状态可能已更新）
                var existingIndex = allExecutions.FindIndex(e => e.Id == runningExecution.Id);
                if (existingIndex >= 0)
                {
                    allExecutions[existingIndex] = runningExecution;
                }
            }
        }
        
        // 应用过滤条件
        var filteredExecutions = allExecutions.AsQueryable();
        
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

        // ✅ 计算今日执行统计
        await CalculateTodayExecutionStatisticsAsync(statistics, allTasks, cancellationToken);

        return statistics;
    }

    /// <summary>
    /// 计算今日执行统计
    /// </summary>
    /// <param name="statistics">统计对象</param>
    /// <param name="allTasks">所有任务</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task CalculateTodayExecutionStatisticsAsync(
        TaskStatistics statistics, 
        List<ScheduledTask> allTasks, 
        CancellationToken cancellationToken)
    {
        try
        {
            var todayStart = DateTime.UtcNow.Date;

            // ✅ 先收集所有执行记录ID，然后批量获取
            var allExecutionCacheKeys = new List<string>();
            foreach (var task in allTasks)
            {
                var indexKey = $"{_options.CacheKeyPrefix}Index:Executions:{task.Id}";
                var executionIds = await _cacheService.GetAsync<List<string>>(indexKey, cancellationToken) ?? new List<string>();
                allExecutionCacheKeys.AddRange(executionIds.Select(id => $"{_options.CacheKeyPrefix}Executions:{id}"));
            }

            // 批量获取所有执行记录
            var todayExecutions = new List<TaskExecution>();
            if (allExecutionCacheKeys.Any())
            {
                var executionDict = await _cacheService.GetManyAsync<TaskExecution>(allExecutionCacheKeys, cancellationToken);
                
                // 只统计今日的执行记录
                todayExecutions = executionDict.Values
                    .Where(e => e != null && e.StartTime >= todayStart)
                    .Select(e => e!)
                    .ToList();
            }

            // 计算今日统计数据
            statistics.TodayExecutions = todayExecutions.Count;
            statistics.TodaySuccessExecutions = todayExecutions.Count(e => e.Status == TaskStatus.Completed);
            statistics.TodayFailedExecutions = todayExecutions.Count(e => 
                e.Status == TaskStatus.Failed || 
                e.Status == TaskStatus.Timeout);

            // 计算成功率
            if (statistics.TodayExecutions > 0)
            {
                statistics.SuccessRate = Math.Round(
                    (double)statistics.TodaySuccessExecutions / statistics.TodayExecutions * 100, 2);
            }
            else
            {
                statistics.SuccessRate = 0;
            }

            _logger.LogDebug("今日执行统计 - 总执行: {Total}, 成功: {Success}, 失败: {Failed}, 成功率: {Rate}%",
                statistics.TodayExecutions, statistics.TodaySuccessExecutions, 
                statistics.TodayFailedExecutions, statistics.SuccessRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算今日执行统计失败");
            // 发生错误时保持默认值0
        }
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

    /// <summary>
    /// 获取仪表板数据
    /// </summary>
    /// <param name="days">趋势数据天数（默认7天）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仪表板数据</returns>
    public async Task<DashboardData> GetDashboardDataAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var dashboard = new DashboardData();

        // 获取统计信息
        dashboard.Statistics = await GetTaskStatisticsAsync(cancellationToken);

        // 获取执行趋势数据
        dashboard.ExecutionTrend = await GetExecutionTrendAsync(days, cancellationToken);

        // 状态分布（用于饼图）
        dashboard.StatusDistribution = new List<ChartDataItem>
        {
            new ChartDataItem { Name = "成功", Value = dashboard.Statistics.TodaySuccessExecutions },
            new ChartDataItem { Name = "失败", Value = dashboard.Statistics.TodayFailedExecutions },
            new ChartDataItem { Name = "运行中", Value = dashboard.Statistics.RunningTasks }
        };

        // 任务类型分布
        foreach (var kvp in dashboard.Statistics.TypeStatistics)
        {
            dashboard.TypeDistribution.Add(new ChartDataItem
            {
                Name = GetTaskTypeName(kvp.Key),
                Value = kvp.Value
            });
        }

        // 获取最近执行记录（最近10条）
        var executionQuery = new ExecutionQueryDto { Page = 1, PerPage = 10 };
        var recentResult = await GetAllExecutionHistoryAsync(executionQuery, cancellationToken);
        dashboard.RecentExecutions = recentResult.Items.ToList();

        return dashboard;
    }

    /// <summary>
    /// 获取执行趋势数据
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>趋势数据</returns>
    private async Task<List<ExecutionTrendItem>> GetExecutionTrendAsync(int days, CancellationToken cancellationToken)
    {
        var trend = new List<ExecutionTrendItem>();
        var allTasks = await GetAllTasksAsync(cancellationToken);

        // ✅ 先收集所有执行记录ID，然后批量获取
        var allExecutionCacheKeys = new List<string>();
        foreach (var task in allTasks)
        {
            var indexKey = $"{_options.CacheKeyPrefix}Index:Executions:{task.Id}";
            var executionIds = await _cacheService.GetAsync<List<string>>(indexKey, cancellationToken) ?? new List<string>();
            allExecutionCacheKeys.AddRange(executionIds.Select(id => $"{_options.CacheKeyPrefix}Executions:{id}"));
        }

        // 批量获取所有执行记录
        var allExecutions = new List<TaskExecution>();
        if (allExecutionCacheKeys.Any())
        {
            var executionDict = await _cacheService.GetManyAsync<TaskExecution>(allExecutionCacheKeys, cancellationToken);
            allExecutions = executionDict.Values.Where(e => e != null).Select(e => e!).ToList();
        }

        // 按日期分组统计
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dayExecutions = allExecutions.Where(e => e.StartTime.Date == date).ToList();

            trend.Add(new ExecutionTrendItem
            {
                Date = date.ToString("MM-dd"),
                Total = dayExecutions.Count,
                Success = dayExecutions.Count(e => e.Status == TaskStatus.Completed),
                Failed = dayExecutions.Count(e => e.Status == TaskStatus.Failed || e.Status == TaskStatus.Timeout)
            });
        }

        return trend;
    }

    /// <summary>
    /// 获取任务类型名称
    /// </summary>
    /// <param name="type">任务类型</param>
    /// <returns>类型名称</returns>
    private static string GetTaskTypeName(TaskType type)
    {
        return type switch
        {
            TaskType.Cron => "Cron定时任务",
            TaskType.Delay => "延迟任务",
            TaskType.OneTime => "一次性任务",
            _ => type.ToString()
        };
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 注册任务所属服务映射
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task RegisterTaskServiceMappingAsync(ScheduledTask task, CancellationToken cancellationToken)
    {
        try
        {
            var registry = _serviceProvider.GetService<ITaskHandlerRegistry>();
            if (registry == null)
            {
                _logger.LogWarning("任务注册表服务不可用，跳过注册任务服务映射 - TaskId: {TaskId}", task.Id);
                return;
            }

            // 优先使用任务指定的 TargetService，否则使用当前服务的 ServiceName
            var targetService = !string.IsNullOrEmpty(task.TargetService) 
                ? task.TargetService 
                : _options.ServiceName;

            if (string.IsNullOrEmpty(targetService))
            {
                _logger.LogWarning("无法确定任务所属服务（TargetService 和 ServiceName 均为空）- TaskId: {TaskId}", task.Id);
                return;
            }

            await registry.RegisterTaskServiceAsync(task.Id, targetService, cancellationToken);
            _logger.LogDebug("注册任务服务映射成功 - TaskId: {TaskId}, TargetService: {TargetService}", task.Id, targetService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册任务服务映射失败 - TaskId: {TaskId}", task.Id);
        }
    }

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
        await _cacheService.SetAsync(cacheKey, task, CodeSpirit.Caching.Models.CacheOptions.L2NeverExpires());
        
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
                await _cacheService.SetAsync(indexKey, taskIds, CodeSpirit.Caching.Models.CacheOptions.L2NeverExpires());
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
            await _cacheService.SetAsync(indexKey, validTaskIds, CodeSpirit.Caching.Models.CacheOptions.L2NeverExpires());
            
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
