namespace CodeSpirit.ConfigCenter.Sdk.Events;

/// <summary>
/// 配置变更事件（与服务器端事件类保持一致）
/// </summary>
public class ConfigChangedEvent
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 变更时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 变更的配置键（可选）
    /// </summary>
    public string? ChangedKey { get; set; }
}

