namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL Cards配置选项
/// </summary>
public class UdlCardsOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "UdlCards";

    /// <summary>
    /// 默认主题
    /// </summary>
    public string DefaultTheme { get; set; } = "default";

    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool EnableCaching { get; set; } = false;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// 每页最大卡片数量
    /// </summary>
    public int MaxCardsPerPage { get; set; } = 10;

    /// <summary>
    /// 默认刷新间隔（毫秒）
    /// </summary>
    public int DefaultRefreshInterval { get; set; } = 0;

    /// <summary>
    /// 页面配置
    /// </summary>
    public Dictionary<string, object>? PageConfig { get; set; }

    /// <summary>
    /// 布局配置
    /// </summary>
    public Dictionary<string, object>? LayoutConfig { get; set; }

    /// <summary>
    /// 仪表板配置
    /// </summary>
    public Dictionary<string, object>? DashboardConfig { get; set; }

    /// <summary>
    /// 严格模式，启用时单个卡片失败会导致整个页面生成失败
    /// </summary>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// 是否启用权限控制
    /// </summary>
    public bool EnablePermissionControl { get; set; } = true;

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// API基础URL
    /// </summary>
    public string? ApiBaseUrl { get; set; }
}

 