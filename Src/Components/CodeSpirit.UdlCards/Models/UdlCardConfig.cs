namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL卡片配置基础类
/// </summary>
public abstract class UdlCardConfig
{
    /// <summary>
    /// 卡片唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 卡片类型
    /// </summary>
    [Required]
    public abstract string Type { get; }

    /// <summary>
    /// 卡片标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 卡片描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 自定义CSS类名
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// 内联样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public UdlCardTheme? ThemeConfig { get; set; }

    /// <summary>
    /// 主题名称
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 显示条件表达式
    /// </summary>
    public string? VisibleOn { get; set; }

    /// <summary>
    /// 数据刷新配置
    /// </summary>
    public UdlRefreshConfig? Refresh { get; set; }

    /// <summary>
    /// 权限配置
    /// </summary>
    public UdlPermissionConfig? Permission { get; set; }

    /// <summary>
    /// 权限控制
    /// </summary>
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// 角色控制
    /// </summary>
    public List<string>? Roles { get; set; }
}

/// <summary>
/// UDL卡片主题配置
/// </summary>
public class UdlCardTheme
{
    /// <summary>
    /// 主题名称：primary, success, warning, danger, info, dark
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// 自定义颜色
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 文字颜色
    /// </summary>
    public string? TextColor { get; set; }
}

/// <summary>
/// UDL刷新配置
/// </summary>
public class UdlRefreshConfig
{
    /// <summary>
    /// 刷新间隔（毫秒）
    /// </summary>
    public int Interval { get; set; } = 30000;

    /// <summary>
    /// 是否自动刷新
    /// </summary>
    public bool Auto { get; set; } = false;

    /// <summary>
    /// 是否显示刷新按钮
    /// </summary>
    public bool ShowButton { get; set; } = true;

    /// <summary>
    /// 刷新时是否显示加载状态
    /// </summary>
    public bool ShowLoading { get; set; } = true;
}

/// <summary>
/// UDL权限配置
/// </summary>
public class UdlPermissionConfig
{
    /// <summary>
    /// 需要的权限列表
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();

    /// <summary>
    /// 需要的角色列表
    /// </summary>
    public List<string> RequiredRoles { get; set; } = new();

    /// <summary>
    /// 权限验证失败时的处理方式：hide, disable, readonly
    /// </summary>
    public string OnDenied { get; set; } = "hide";
} 