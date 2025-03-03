namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置中心客户端选项
/// </summary>
public class ConfigCenterClientOptions
{
    /// <summary>
    /// 配置中心服务地址
    /// </summary>
    public string ServiceUrl { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 应用密钥
    /// </summary>
    public string AppSecret { get; set; }

    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// 轮询配置更新的时间间隔(秒)
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否使用SignalR实时接收配置变更
    /// </summary>
    public bool UseSignalR { get; set; } = true;

    /// <summary>
    /// 配置获取超时时间(秒)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// 是否启用本地缓存
    /// </summary>
    public bool EnableLocalCache { get; set; } = true;

    /// <summary>
    /// 本地缓存目录
    /// </summary>
    public string LocalCacheDirectory { get; set; } = ".config-cache";

    /// <summary>
    /// 缓存文件的最大有效期(分钟)，超过此时间将视为过期
    /// 默认为1440分钟(24小时)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 1440;

    /// <summary>
    /// 当配置数据源不可用时，是否优先使用缓存
    /// </summary>
    public bool PreferCache { get; set; } = false;

    /// <summary>
    /// 是否忽略SSL证书错误
    /// </summary>
    public bool IgnoreSslCertificateErrors { get; set; } = false;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int? MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 重试延迟时间(秒)
    /// </summary>
    public int? RetryDelaySeconds { get; set; } = 2;
}