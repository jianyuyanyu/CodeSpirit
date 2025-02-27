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
    /// 是否自动注册应用
    /// </summary>
    public bool AutoRegisterApp { get; set; } = false;

    /// <summary>
    /// 应用名称（仅用于自动注册）
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// 轮询配置更新的时间间隔（秒）
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否使用SignalR实时监听配置变更
    /// </summary>
    public bool UseSignalR { get; set; } = true;

    /// <summary>
    /// 配置获取超时时间（秒）
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
    /// 缓存文件的最大有效期（分钟），超过此时间将视为缓存过期
    /// 默认为1440分钟（24小时）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 1440;

    /// <summary>
    /// 当主配置源可用时，是否仍然优先使用缓存
    /// </summary>
    public bool PreferCache { get; set; } = false;
}