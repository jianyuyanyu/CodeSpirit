namespace CodeSpirit.ConfigCenter.Sdk;

/// <summary>
/// 配置中心选项
/// </summary>
public class ConfigCenterOptions
{
    /// <summary>
    /// 应用ID（自动推断）
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// 是否自动注册应用
    /// </summary>
    public bool AutoRegister { get; set; } = true;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 配置中心服务地址（从 Aspire 服务发现自动获取）
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// 是否启用详细日志（开发环境建议启用，以便确认配置变更）
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// 是否使用轮询模式代替SSE（在Aspire环境中SSE可能不可用）
    /// </summary>
    public bool UsePollingMode { get; set; } = false;

    /// <summary>
    /// 轮询间隔（秒），仅在轮询模式下生效
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// SSE连续失败多少次后自动切换到轮询模式
    /// </summary>
    public int SseFailureThresholdBeforePolling { get; set; } = 3;
}

