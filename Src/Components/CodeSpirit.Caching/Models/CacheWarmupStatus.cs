namespace CodeSpirit.Caching.Models;

/// <summary>
/// 缓存预热状态
/// </summary>
public class CacheWarmupStatus
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 预热状态
    /// </summary>
    public WarmupState State { get; set; } = WarmupState.NotStarted;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 耗时
    /// </summary>
    public TimeSpan? Duration => CompletedTime.HasValue && StartTime.HasValue 
        ? CompletedTime.Value - StartTime.Value 
        : null;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => State == WarmupState.Completed && string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// 额外信息
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 预热状态枚举
/// </summary>
public enum WarmupState
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout = 5
}
