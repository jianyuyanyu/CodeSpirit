namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL仪表板配置
/// </summary>
public class UdlDashboardConfig
{
    /// <summary>
    /// 仪表板标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 仪表板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 仪表板布局类型
    /// </summary>
    public string Layout { get; set; } = "grid";

    /// <summary>
    /// 仪表板版块列表
    /// </summary>
    public List<UdlDashboardSection> Sections { get; set; } = new();

    /// <summary>
    /// 布局配置
    /// </summary>
    public UdlLayoutConfig? LayoutConfig { get; set; }

    /// <summary>
    /// 权限控制
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// 角色控制
    /// </summary>
    public List<string>? Roles { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int? RefreshInterval { get; set; }

    /// <summary>
    /// 是否显示刷新按钮
    /// </summary>
    public bool ShowRefresh { get; set; } = true;

    /// <summary>
    /// 是否显示全屏按钮
    /// </summary>
    public bool ShowFullscreen { get; set; } = true;

    /// <summary>
    /// 是否显示设置按钮
    /// </summary>
    public bool ShowSettings { get; set; } = false;

    /// <summary>
    /// 仪表板样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 仪表板类名
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object>? CustomProperties { get; set; }

    /// <summary>
    /// 刷新配置
    /// </summary>
    public UdlRefreshConfig? Refresh { get; set; }
} 