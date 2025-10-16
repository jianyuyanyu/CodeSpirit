using CodeSpirit.ScheduledTasks.Models;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Configuration;

/// <summary>
/// 定时任务配置选项
/// </summary>
public class ScheduledTasksOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ScheduledTasks";

    /// <summary>
    /// 是否启用定时任务
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 默认超时时间
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 最大并发任务数
    /// </summary>
    public int MaxConcurrentTasks { get; set; } = 10;

    /// <summary>
    /// 任务清理间隔
    /// </summary>
    public TimeSpan TaskCleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 执行历史保留时间
    /// </summary>
    public TimeSpan ExecutionHistoryRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "CodeSpirit:ScheduledTasks:";

    /// <summary>
    /// 服务名称（用于标识当前服务，只执行属于当前服务的任务处理器）
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 分布式锁超时时间
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 任务扫描间隔
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 默认重试次数
    /// </summary>
    public int DefaultRetryCount { get; set; } = 3;

    /// <summary>
    /// 默认重试间隔
    /// </summary>
    public TimeSpan DefaultRetryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// 预定义任务列表
    /// </summary>
    public List<TaskDefinition> Tasks { get; set; } = new();

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <returns>验证结果</returns>
    public bool Validate()
    {
        if (DefaultTimeout <= TimeSpan.Zero)
        {
            return false;
        }

        if (MaxConcurrentTasks <= 0)
        {
            return false;
        }

        if (TaskCleanupInterval <= TimeSpan.Zero)
        {
            return false;
        }

        if (ExecutionHistoryRetention <= TimeSpan.Zero)
        {
            return false;
        }

        if (LockTimeout <= TimeSpan.Zero)
        {
            return false;
        }

        if (ScanInterval <= TimeSpan.Zero)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// 任务定义配置
/// </summary>
public class TaskDefinition
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    public TaskType Type { get; set; } = TaskType.Cron;

    /// <summary>
    /// Cron表达式
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// 延迟时间
    /// </summary>
    public TimeSpan? DelayTime { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime? ExecuteAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 执行策略
    /// </summary>
    public ExecutionStrategy ExecutionStrategy { get; set; } = ExecutionStrategy.Distributed;

    /// <summary>
    /// 超时时间
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 0;

    /// <summary>
    /// 重试间隔
    /// </summary>
    public TimeSpan? RetryInterval { get; set; }

    /// <summary>
    /// 任务处理器类型名称
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// 目标服务名称（指定哪个服务执行此任务，为空表示任何服务都可以执行）
    /// </summary>
    public string? TargetService { get; set; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// 任务优先级
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 转换为ScheduledTask
    /// </summary>
    /// <returns>定时任务对象</returns>
    public ScheduledTask ToScheduledTask()
    {
        return new ScheduledTask
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            CronExpression = CronExpression,
            DelayTime = DelayTime,
            ExecuteAt = ExecuteAt,
            Status = Enabled ? TaskStatus.Enabled : TaskStatus.Disabled,
            ExecutionStrategy = ExecutionStrategy,
            Timeout = Timeout,
            MaxRetryCount = MaxRetryCount,
            RetryInterval = RetryInterval,
            HandlerType = HandlerType,
            Parameters = Parameters != null ? JsonConvert.SerializeObject(Parameters) : null,
            Group = Group,
            Priority = Priority,
            TargetService = TargetService,
            IsFromConfiguration = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
