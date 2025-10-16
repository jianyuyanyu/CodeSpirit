namespace CodeSpirit.ScheduledTasks.Models;

/// <summary>
/// 任务执行记录
/// </summary>
public class TaskExecution
{
    /// <summary>
    /// 执行ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 执行状态
    /// </summary>
    [DisplayName("执行状态")]
    public TaskStatus Status { get; set; } = TaskStatus.Running;

    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 执行时长
    /// </summary>
    [DisplayName("执行时长")]
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    /// <summary>
    /// 执行结果
    /// </summary>
    [DisplayName("执行结果")]
    public string? Result { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [DisplayName("异常堆栈")]
    public string? StackTrace { get; set; }

    /// <summary>
    /// 执行节点
    /// </summary>
    [DisplayName("执行节点")]
    public string? ExecutionNode { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [DisplayName("重试次数")]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 是否超时
    /// </summary>
    [DisplayName("是否超时")]
    public bool IsTimeout { get; set; } = false;

    /// <summary>
    /// 执行参数
    /// </summary>
    [DisplayName("执行参数")]
    public string? Parameters { get; set; }

    /// <summary>
    /// 触发方式
    /// </summary>
    [DisplayName("触发方式")]
    public string? TriggerType { get; set; }

    /// <summary>
    /// 执行日志
    /// </summary>
    [DisplayName("执行日志")]
    public List<string> Logs { get; set; } = new();

    /// <summary>
    /// 性能指标
    /// </summary>
    [DisplayName("性能指标")]
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// 添加日志
    /// </summary>
    /// <param name="message">日志消息</param>
    public void AddLog(string message)
    {
        Logs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    /// <summary>
    /// 添加性能指标
    /// </summary>
    /// <param name="key">指标名称</param>
    /// <param name="value">指标值</param>
    public void AddMetric(string key, object value)
    {
        Metrics[key] = value;
    }

    /// <summary>
    /// 标记为完成
    /// </summary>
    /// <param name="result">执行结果</param>
    public void MarkCompleted(string? result = null)
    {
        Status = TaskStatus.Completed;
        EndTime = DateTime.UtcNow;
        Result = result;
    }

    /// <summary>
    /// 标记为失败
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <param name="stackTrace">异常堆栈</param>
    public void MarkFailed(string errorMessage, string? stackTrace = null)
    {
        Status = TaskStatus.Failed;
        EndTime = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;
    }

    /// <summary>
    /// 标记为取消
    /// </summary>
    public void MarkCancelled()
    {
        Status = TaskStatus.Cancelled;
        EndTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记为超时
    /// </summary>
    public void MarkTimeout()
    {
        Status = TaskStatus.Timeout;
        EndTime = DateTime.UtcNow;
        IsTimeout = true;
        ErrorMessage = "任务执行超时";
    }

    /// <summary>
    /// 标记为跳过
    /// </summary>
    /// <param name="reason">跳过原因</param>
    public void MarkSkipped(string reason)
    {
        Status = TaskStatus.Skipped;
        EndTime = DateTime.UtcNow;
        ErrorMessage = reason;
    }
}
