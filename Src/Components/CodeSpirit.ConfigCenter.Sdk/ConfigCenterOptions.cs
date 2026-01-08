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
}

