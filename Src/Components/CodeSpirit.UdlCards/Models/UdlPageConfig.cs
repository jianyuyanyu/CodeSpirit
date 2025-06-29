namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL页面配置
/// </summary>
public class UdlPageConfig
{
    /// <summary>
    /// 页面标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 页面副标题
    /// </summary>
    public string? SubTitle { get; set; }

    /// <summary>
    /// 页面描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 页面布局类型
    /// </summary>
    public string Layout { get; set; } = "grid";

    /// <summary>
    /// 卡片列表
    /// </summary>
    public List<Dictionary<string, object>> Cards { get; set; } = new();

    /// <summary>
    /// 页面样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 页面类名
    /// </summary>
    public string? ClassName { get; set; }

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
    /// 是否显示标题
    /// </summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// 是否显示刷新按钮
    /// </summary>
    public bool ShowRefresh { get; set; } = true;

    /// <summary>
    /// 自动刷新间隔（秒）
    /// </summary>
    public int? RefreshInterval { get; set; }
} 