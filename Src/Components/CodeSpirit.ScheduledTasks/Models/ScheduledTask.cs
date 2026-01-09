namespace CodeSpirit.ScheduledTasks.Models;

/// <summary>
/// 定时任务模型
/// </summary>
public class ScheduledTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(100, ErrorMessage = "任务名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [DisplayName("任务描述")]
    [StringLength(500, ErrorMessage = "任务描述长度不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [DisplayName("任务类型")]
    public TaskType Type { get; set; } = TaskType.Cron;

    /// <summary>
    /// Cron表达式（当Type为Cron时使用）
    /// </summary>
    [DisplayName("Cron表达式")]
    public string? CronExpression { get; set; }

    /// <summary>
    /// 延迟时间（当Type为Delay时使用）
    /// </summary>
    [DisplayName("延迟时间")]
    public TimeSpan? DelayTime { get; set; }

    /// <summary>
    /// 执行时间（当Type为OneTime时使用）
    /// </summary>
    [DisplayName("执行时间")]
    public DateTime? ExecuteAt { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus Status { get; set; } = TaskStatus.Disabled;

    /// <summary>
    /// 执行策略
    /// </summary>
    [DisplayName("执行策略")]
    public ExecutionStrategy ExecutionStrategy { get; set; } = ExecutionStrategy.Distributed;

    /// <summary>
    /// 超时时间
    /// </summary>
    [DisplayName("超时时间")]
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [DisplayName("最大重试次数")]
    [Range(0, 10, ErrorMessage = "最大重试次数必须在0-10之间")]
    public int MaxRetryCount { get; set; } = 0;

    /// <summary>
    /// 重试间隔
    /// </summary>
    [DisplayName("重试间隔")]
    public TimeSpan? RetryInterval { get; set; }

    /// <summary>
    /// 任务处理器类型名称
    /// </summary>
    [DisplayName("处理器类型")]
    [Required(ErrorMessage = "处理器类型不能为空")]
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// 任务参数（JSON格式）
    /// </summary>
    [DisplayName("任务参数")]
    public string? Parameters { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者
    /// </summary>
    [DisplayName("创建者")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新者
    /// </summary>
    [DisplayName("更新者")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [DisplayName("下次执行时间")]
    public DateTime? NextExecuteTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [DisplayName("上次执行时间")]
    public DateTime? LastExecuteTime { get; set; }

    /// <summary>
    /// 执行次数
    /// </summary>
    [DisplayName("执行次数")]
    public int ExecutionCount { get; set; } = 0;

    /// <summary>
    /// 是否来自配置文件
    /// </summary>
    [DisplayName("来自配置文件")]
    public bool IsFromConfiguration { get; set; } = false;

    /// <summary>
    /// 任务分组
    /// </summary>
    [DisplayName("任务分组")]
    [StringLength(50, ErrorMessage = "任务分组长度不能超过50个字符")]
    public string? Group { get; set; }

    /// <summary>
    /// 任务优先级
    /// </summary>
    [DisplayName("任务优先级")]
    [Range(1, 10, ErrorMessage = "任务优先级必须在1-10之间")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 目标服务名称（指定哪个服务执行此任务，为空表示任何服务都可以执行）
    /// </summary>
    [DisplayName("目标服务")]
    public string? TargetService { get; set; }

    /// <summary>
    /// 通知配置（JSON格式）
    /// </summary>
    [DisplayName("通知配置")]
    public string? NotificationConfig { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled => Status == TaskStatus.Enabled || Status == TaskStatus.Running;
}
