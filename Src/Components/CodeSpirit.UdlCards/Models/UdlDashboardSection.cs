namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL仪表板版块配置
/// </summary>
public class UdlDashboardSection
{
    /// <summary>
    /// 版块标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 版块描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 版块卡片列表
    /// </summary>
    public List<Dictionary<string, object>> Cards { get; set; } = new();

    /// <summary>
    /// 版块布局配置
    /// </summary>
    public UdlLayoutConfig? LayoutConfig { get; set; }

    /// <summary>
    /// 版块权限控制
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// 版块角色控制
    /// </summary>
    public List<string>? Roles { get; set; }

    /// <summary>
    /// 是否可折叠
    /// </summary>
    public bool Collapsible { get; set; } = false;

    /// <summary>
    /// 默认是否展开
    /// </summary>
    public bool Expanded { get; set; } = true;

    /// <summary>
    /// 版块样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 版块类名
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// 版块顺序
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 自定义属性
    /// </summary>
    public Dictionary<string, object>? CustomProperties { get; set; }

    /// <summary>
    /// 页面配置
    /// </summary>
    public UdlPageConfig? PageConfig { get; set; }
} 