# CodeSpirit.ScheduledTasks 技术设计文档

## 文档信息

- **组件名称**: CodeSpirit.ScheduledTasks
- **版本**: v1.0.0

## 设计目标

### 主要目标

1. **分布式友好**: 支持多实例部署，避免任务重复执行
2. **高可用性**: 基于Redis缓存，无单点故障
3. **易于使用**: 提供简洁的API和Web管理界面
4. **可扩展性**: 支持自定义任务处理器和配置

### 非功能性需求

- **性能**: 支持高并发任务调度
- **可靠性**: 99.9%的任务执行成功率
- **可维护性**: 清晰的代码结构和完善的文档
- **可观测性**: 完整的日志记录和监控指标

## 架构设计

### 整体架构

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web UI        │    │   API Services  │    │   Background    │
│   (AMIS)        │    │                 │    │   Services      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────────────────────────────────────┐
         │           Core Services Layer                   │
         │  ┌─────────────────┐  ┌─────────────────┐      │
         │  │ TaskService     │  │ QueryService    │      │
         │  └─────────────────┘  └─────────────────┘      │
         │  ┌─────────────────┐  ┌─────────────────┐      │
         │  │ TaskExecutor    │  │ TaskScheduler   │      │
         │  └─────────────────┘  └─────────────────┘      │
         └─────────────────────────────────────────────────┘
                                 │
         ┌─────────────────────────────────────────────────┐
         │           Infrastructure Layer                  │
         │  ┌─────────────────┐  ┌─────────────────┐      │
         │  │ Cache Service   │  │ Lock Provider   │      │
         │  └─────────────────┘  └─────────────────┘      │
         └─────────────────────────────────────────────────┘
                                 │
                    ┌─────────────────┐
                    │     Redis       │
                    │   (Cache/Lock)  │
                    └─────────────────┘
```

### 核心组件

#### 1. 任务调度器 (TaskScheduler)

**职责**:
- 扫描待执行任务
- 管理任务执行队列
- 处理任务超时

**关键特性**:
- 基于时间轮算法的高效调度
- 支持秒级精度
- 内存友好的数据结构

#### 2. 任务执行器 (TaskExecutor)

**职责**:
- 执行具体任务
- 管理执行上下文
- 处理异常和超时

**关键特性**:
- 支持并发执行
- 优雅的超时控制
- 完整的生命周期管理

#### 3. 分布式锁提供者 (IDistributedLockProvider)

**职责**:
- 提供分布式锁机制
- 防止任务重复执行
- 支持锁续期

**实现方式**:
- 基于Redis的分布式锁
- 使用Lua脚本保证原子性
- 自动过期和续期机制

#### 4. 缓存服务 (ICacheService)

**职责**:
- 存储任务定义和执行记录
- 提供高性能数据访问
- 支持数据过期策略

**存储结构**:
```
CodeSpirit:ScheduledTasks:Tasks:{TaskId}        -> ScheduledTask
CodeSpirit:ScheduledTasks:Executions:{ExecId}  -> TaskExecution
CodeSpirit:ScheduledTasks:Index:Active         -> List<TaskId>
CodeSpirit:ScheduledTasks:Lock:{TaskId}        -> Lock Info
```

## 数据模型

### 核心实体

#### ScheduledTask (定时任务)

```csharp
public class ScheduledTask
{
    public string Id { get; set; }                    // 任务唯一标识
    public string Name { get; set; }                  // 任务名称
    public string? Description { get; set; }          // 任务描述
    public TaskType Type { get; set; }                // 任务类型
    public string? CronExpression { get; set; }       // Cron表达式
    public DateTime? ScheduledTime { get; set; }      // 计划执行时间
    public string HandlerType { get; set; }           // 处理器类型
    public TaskStatus Status { get; set; }            // 任务状态
    public TimeSpan Timeout { get; set; }             // 超时时间
    public bool Enabled { get; set; }                 // 是否启用
    public DateTime? NextExecuteTime { get; set; }    // 下次执行时间
    public DateTime? LastExecuteTime { get; set; }    // 上次执行时间
    public bool IsFromConfiguration { get; set; }     // 是否来自配置文件
    public DateTime CreatedAt { get; set; }           // 创建时间
    public DateTime UpdatedAt { get; set; }           // 更新时间
}
```

#### TaskExecution (任务执行记录)

```csharp
public class TaskExecution
{
    public string Id { get; set; }                    // 执行唯一标识
    public string TaskId { get; set; }                // 任务ID
    public string TaskName { get; set; }              // 任务名称
    public ExecutionStatus Status { get; set; }       // 执行状态
    public DateTime StartTime { get; set; }           // 开始时间
    public DateTime? EndTime { get; set; }            // 结束时间
    public TimeSpan? Duration { get; set; }           // 执行时长
    public string? ErrorMessage { get; set; }         // 错误信息
    public string? StackTrace { get; set; }           // 堆栈跟踪
    public string? Result { get; set; }               // 执行结果
    public string ServerInstance { get; set; }        // 服务器实例
}
```

### 枚举定义

```csharp
public enum TaskType
{
    Cron = 1,      // Cron表达式任务
    OneTime = 2    // 一次性任务
}

public enum TaskStatus
{
    Enabled = 1,   // 启用
    Disabled = 2   // 禁用
}

public enum ExecutionStatus
{
    Running = 1,   // 运行中
    Completed = 2, // 已完成
    Failed = 3,    // 失败
    Cancelled = 4, // 已取消
    Timeout = 5    // 超时
}
```

## 关键算法

### 1. 任务调度算法

使用时间轮 (Time Wheel) 算法实现高效的任务调度：

```csharp
public class TaskScheduler
{
    private readonly TimeWheel _timeWheel;
    private readonly Timer _timer;
    
    public void ScheduleTask(ScheduledTask task)
    {
        var delay = CalculateDelay(task);
        _timeWheel.AddTask(task, delay);
    }
    
    private void OnTimerTick()
    {
        var expiredTasks = _timeWheel.GetExpiredTasks();
        foreach (var task in expiredTasks)
        {
            _ = Task.Run(() => ExecuteTaskAsync(task));
        }
    }
}
```

### 2. 分布式锁算法

使用Redis Lua脚本实现分布式锁：

```lua
-- 获取锁
local key = KEYS[1]
local value = ARGV[1]
local ttl = ARGV[2]

if redis.call('SET', key, value, 'NX', 'EX', ttl) then
    return 1
else
    return 0
end
```

```lua
-- 释放锁
local key = KEYS[1]
local value = ARGV[1]

if redis.call('GET', key) == value then
    return redis.call('DEL', key)
else
    return 0
end
```

### 3. Cron表达式解析

使用 Cronos 库解析Cron表达式：

```csharp
public static class CronHelper
{
    public static DateTime? GetNextOccurrence(string cronExpression, DateTime from)
    {
        try
        {
            var cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            return cron.GetNextOccurrence(from, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }
}
```

## 并发控制

### 1. 任务级别并发控制

```csharp
public class TaskExecutor
{
    private readonly SemaphoreSlim _semaphore;
    
    public TaskExecutor(ScheduledTasksOptions options)
    {
        _semaphore = new SemaphoreSlim(options.MaxConcurrentTasks);
    }
    
    public async Task ExecuteAsync(ScheduledTask task)
    {
        await _semaphore.WaitAsync();
        try
        {
            await ExecuteTaskInternal(task);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 2. 分布式并发控制

```csharp
public async Task<bool> TryExecuteTaskAsync(ScheduledTask task)
{
    var lockKey = $"CodeSpirit:ScheduledTasks:Lock:{task.Id}";
    var lockValue = Environment.MachineName + "-" + Guid.NewGuid();
    
    using var distributedLock = await _lockProvider.TryAcquireLockAsync(
        lockKey, 
        task.Timeout.Add(TimeSpan.FromSeconds(30))
    );
    
    if (distributedLock == null)
    {
        _logger.LogDebug("任务 {TaskId} 已被其他实例执行", task.Id);
        return false;
    }
    
    await ExecuteTaskInternal(task);
    return true;
}
```

## 错误处理策略

### 1. 异常分类

```csharp
public enum ErrorType
{
    BusinessError,    // 业务逻辑错误
    SystemError,      // 系统错误
    TimeoutError,     // 超时错误
    CancellationError // 取消错误
}
```

### 2. 重试机制

```csharp
public async Task ExecuteWithRetryAsync(ScheduledTask task, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await ExecuteTaskInternal(task);
            return;
        }
        catch (Exception ex) when (ShouldRetry(ex, attempt, maxRetries))
        {
            var delay = CalculateRetryDelay(attempt);
            await Task.Delay(delay);
        }
    }
}
```

### 3. 错误恢复

- **任务失败**: 记录错误信息，根据配置决定是否重试
- **实例故障**: 其他实例自动接管未完成的任务
- **Redis故障**: 降级到内存模式，恢复后同步状态

## 性能优化

### 1. 缓存优化

```csharp
public class TaskCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _distributedCache;
    
    public async Task<ScheduledTask?> GetTaskAsync(string taskId)
    {
        // L1缓存 (内存)
        if (_memoryCache.TryGetValue(taskId, out ScheduledTask? task))
        {
            return task;
        }
        
        // L2缓存 (Redis)
        task = await _distributedCache.GetAsync<ScheduledTask>(taskId);
        if (task != null)
        {
            _memoryCache.Set(taskId, task, TimeSpan.FromMinutes(5));
        }
        
        return task;
    }
}
```

### 2. 批量操作

```csharp
public async Task SaveExecutionRecordsAsync(List<TaskExecution> executions)
{
    var pipeline = _redis.CreateBatch();
    
    foreach (var execution in executions)
    {
        var key = $"CodeSpirit:ScheduledTasks:Executions:{execution.Id}";
        pipeline.StringSetAsync(key, JsonSerializer.Serialize(execution), 
            TimeSpan.FromDays(7));
    }
    
    await pipeline.ExecuteAsync();
}
```

### 3. 内存管理

- 使用对象池减少GC压力
- 及时释放不再使用的资源
- 合理设置缓存过期时间

## 监控和诊断

### 1. 关键指标

```csharp
public class TaskMetrics
{
    public static readonly Counter TaskExecutionCount = 
        Metrics.CreateCounter("scheduled_tasks_executed_total", "任务执行总数");
    
    public static readonly Histogram TaskExecutionDuration = 
        Metrics.CreateHistogram("scheduled_tasks_duration_seconds", "任务执行时长");
    
    public static readonly Gauge ActiveTasksCount = 
        Metrics.CreateGauge("scheduled_tasks_active_count", "活跃任务数量");
}
```

### 2. 健康检查

```csharp
public class ScheduledTasksHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查Redis连接
            await _cacheService.GetAsync<string>("health-check");
            
            // 检查任务调度器状态
            var isSchedulerRunning = _backgroundService.IsRunning;
            
            return isSchedulerRunning 
                ? HealthCheckResult.Healthy("定时任务组件运行正常")
                : HealthCheckResult.Unhealthy("任务调度器未运行");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("定时任务组件异常", ex);
        }
    }
}
```

### 3. 日志记录

```csharp
public static class LogEvents
{
    public static readonly EventId TaskStarted = new(1001, "TaskStarted");
    public static readonly EventId TaskCompleted = new(1002, "TaskCompleted");
    public static readonly EventId TaskFailed = new(1003, "TaskFailed");
    public static readonly EventId TaskTimeout = new(1004, "TaskTimeout");
    public static readonly EventId LockAcquired = new(1005, "LockAcquired");
    public static readonly EventId LockFailed = new(1006, "LockFailed");
}
```

## 安全考虑

### 1. 任务处理器验证

```csharp
public bool IsValidHandlerType(string handlerType)
{
    // 验证处理器类型是否在允许列表中
    var allowedTypes = _configuration.GetSection("ScheduledTasks:AllowedHandlers")
        .Get<string[]>() ?? Array.Empty<string>();
    
    return allowedTypes.Contains(handlerType) || 
           handlerType.StartsWith("MyApp.Tasks.");
}
```

### 2. 权限控制

```csharp
[Authorize(Policy = "ScheduledTasksManagement")]
public class ScheduledTasksController : ApiControllerBase
{
    // 只有具有相应权限的用户才能管理任务
}
```

### 3. 输入验证

```csharp
public class CreateTaskRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [CronExpression]
    public string? CronExpression { get; set; }
    
    [ValidHandlerType]
    public string HandlerType { get; set; }
}
```

## 扩展点

### 1. 自定义任务处理器

```csharp
public interface ITaskHandler
{
    Task ExecuteAsync(TaskExecutionContext context, CancellationToken cancellationToken);
}
```

### 2. 自定义任务调度策略

```csharp
public interface ITaskSchedulingStrategy
{
    DateTime? CalculateNextExecutionTime(ScheduledTask task);
    bool ShouldExecuteNow(ScheduledTask task);
}
```

### 3. 自定义存储提供者

```csharp
public interface ITaskStorageProvider
{
    Task<ScheduledTask?> GetTaskAsync(string taskId);
    Task SaveTaskAsync(ScheduledTask task);
    Task DeleteTaskAsync(string taskId);
}
```

## 部署考虑

### 1. 环境要求

- **.NET 9.0+**
- **Redis 6.0+** (支持分布式锁)
- **内存**: 建议至少512MB可用内存
- **CPU**: 支持多核处理器

### 2. 配置建议

```json
{
  "ScheduledTasks": {
    "Enabled": true,
    "DefaultTimeout": "00:30:00",
    "MaxConcurrentTasks": 10,
    "TaskCleanupInterval": "01:00:00",
    "ExecutionHistoryRetention": "7.00:00:00"
  },
  "Caching": {
    "EnableL1Cache": true,
    "EnableL2Cache": true,
    "LockTimeout": "00:00:30"
  }
}
```

### 3. 监控建议

- 设置任务执行失败率告警
- 监控Redis连接状态
- 跟踪任务执行时长趋势
- 监控内存和CPU使用情况

## 测试策略

### 1. 单元测试

```csharp
[Test]
public async Task ExecuteAsync_ShouldCompleteSuccessfully()
{
    // Arrange
    var task = CreateTestTask();
    var handler = new Mock<ITaskHandler>();
    
    // Act
    await _taskExecutor.ExecuteAsync(task);
    
    // Assert
    handler.Verify(h => h.ExecuteAsync(It.IsAny<TaskExecutionContext>(), 
        It.IsAny<CancellationToken>()), Times.Once);
}
```

### 2. 集成测试

```csharp
[Test]
public async Task ScheduledTask_ShouldExecuteAtCorrectTime()
{
    // 测试任务是否在正确的时间执行
    var task = new ScheduledTask
    {
        CronExpression = "*/5 * * * * *", // 每5秒执行
        HandlerType = "TestTaskHandler"
    };
    
    await _taskService.CreateTaskAsync(task);
    
    // 等待任务执行
    await Task.Delay(TimeSpan.FromSeconds(6));
    
    // 验证任务已执行
    var executions = await _queryService.GetExecutionHistoryAsync(task.Id, new QueryDtoBase());
    Assert.That(executions.Items.Count, Is.GreaterThan(0));
}
```

### 3. 性能测试

```csharp
[Test]
public async Task ScheduleMultipleTasks_ShouldHandleConcurrency()
{
    // 测试并发任务调度性能
    var tasks = Enumerable.Range(1, 1000)
        .Select(i => CreateTestTask($"task-{i}"))
        .ToList();
    
    var stopwatch = Stopwatch.StartNew();
    
    await Task.WhenAll(tasks.Select(t => _taskService.CreateTaskAsync(t)));
    
    stopwatch.Stop();
    
    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000));
}
```

## 版本规划

### v1.0.0 (当前版本)
- ✅ 基础任务调度功能
- ✅ 分布式执行支持
- ✅ Web管理界面
- ✅ 配置文件支持

### v1.1.0 (计划中)
- 🔄 任务依赖关系支持
- 🔄 更丰富的重试策略
- 🔄 任务执行结果通知
- 🔄 更多的监控指标

### v1.2.0 (未来版本)
- 📋 任务工作流支持
- 📋 动态任务参数
- 📋 任务执行历史分析
- 📋 可视化任务编辑器

## 总结

CodeSpirit.ScheduledTasks 组件通过合理的架构设计和技术选型，实现了一个功能完整、性能优秀、易于使用的分布式定时任务系统。组件充分利用了现有的缓存和锁机制，避免了对数据库的依赖，同时提供了丰富的管理功能和监控能力。

通过模块化的设计和丰富的扩展点，组件能够满足不同场景下的定时任务需求，并为未来的功能扩展预留了空间。
