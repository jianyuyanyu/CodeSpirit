namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// UDL布局配置
/// </summary>
public class UdlLayoutConfig
{
    /// <summary>
    /// 布局类型：grid, flex, tabs
    /// </summary>
    public string Type { get; set; } = "grid";

    /// <summary>
    /// 网格列数（grid布局使用）
    /// </summary>
    public int? Columns { get; set; }

    /// <summary>
    /// 网格间距
    /// </summary>
    public int? Gap { get; set; }

    /// <summary>
    /// 响应式断点配置
    /// </summary>
    public Dictionary<string, object>? Responsive { get; set; }

    /// <summary>
    /// 弹性布局方向（flex布局使用）
    /// </summary>
    public string? FlexDirection { get; set; }

    /// <summary>
    /// 弹性布局主轴对齐（flex布局使用）
    /// </summary>
    public string? JustifyContent { get; set; }

    /// <summary>
    /// 弹性布局交叉轴对齐（flex布局使用）
    /// </summary>
    public string? AlignItems { get; set; }

    /// <summary>
    /// 标签页配置（tabs布局使用）
    /// </summary>
    public Dictionary<string, object>? TabsConfig { get; set; }

    /// <summary>
    /// 布局样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 布局类名
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// 弹性布局方向（用于UdlCardsGenerator）
    /// </summary>
    public string? Direction { get; set; }

    /// <summary>
    /// 是否换行
    /// </summary>
    public bool? Wrap { get; set; }

    /// <summary>
    /// 主轴对齐方式
    /// </summary>
    public string? Justify { get; set; }

    /// <summary>
    /// 交叉轴对齐方式
    /// </summary>
    public string? Align { get; set; }
} 