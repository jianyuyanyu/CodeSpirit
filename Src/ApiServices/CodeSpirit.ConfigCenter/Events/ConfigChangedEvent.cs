namespace CodeSpirit.ConfigCenter.Events;

/// <summary>
/// 配置变更事件（用于分布式通知）
/// </summary>
public class ConfigChangedEvent
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 配置版本号
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// 变更时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 变更的配置键（可选）
    /// </summary>
    public string? ChangedKey { get; set; }
}

