namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// 信息网格卡片配置
/// </summary>
public class InfoGridCardConfig : UdlCardConfig
{
    /// <summary>
    /// 卡片类型：info-grid
    /// </summary>
    public override string Type => "info-grid";

    /// <summary>
    /// 网格配置
    /// </summary>
    public InfoGridConfig Grid { get; set; } = new();

    /// <summary>
    /// 网格项配置
    /// </summary>
    public List<InfoGridItem> Items { get; set; } = new();

    /// <summary>
    /// 数据源配置
    /// </summary>
    public InfoGridDataConfig? Data { get; set; }
}

/// <summary>
/// 信息网格配置
/// </summary>
public class InfoGridConfig
{
    /// <summary>
    /// 网格列数
    /// </summary>
    public int Columns { get; set; } = 3;

    /// <summary>
    /// 网格间距
    /// </summary>
    public string Gap { get; set; } = "16px";

    /// <summary>
    /// 是否自适应
    /// </summary>
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// 响应式断点配置
    /// </summary>
    public Dictionary<string, int>? ResponsiveColumns { get; set; }

    /// <summary>
    /// 网格项最小高度
    /// </summary>
    public string? MinHeight { get; set; }

    /// <summary>
    /// 网格项对齐方式：start, center, end, stretch
    /// </summary>
    public string ItemAlign { get; set; } = "stretch";
}

/// <summary>
/// 信息网格项配置
/// </summary>
public class InfoGridItem
{
    /// <summary>
    /// 项目标识符
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 项目标题
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 项目值
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 项目图标
    /// </summary>
    public InfoGridIconConfig? Icon { get; set; }

    /// <summary>
    /// 项目颜色主题
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// 项目链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 是否高亮显示
    /// </summary>
    public bool Highlight { get; set; } = false;

    /// <summary>
    /// 显示条件
    /// </summary>
    public string? VisibleOn { get; set; }

    /// <summary>
    /// 自定义样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }

    /// <summary>
    /// 格式化配置
    /// </summary>
    public InfoGridFormatConfig? Format { get; set; }
}

/// <summary>
/// 信息网格图标配置
/// </summary>
public class InfoGridIconConfig
{
    /// <summary>
    /// 图标名称或URL
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 图标大小：xs, sm, md, lg, xl
    /// </summary>
    public string Size { get; set; } = "md";

    /// <summary>
    /// 图标位置：left, right, top, bottom
    /// </summary>
    public string Position { get; set; } = "left";

    /// <summary>
    /// 图标背景
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 是否显示边框
    /// </summary>
    public bool ShowBorder { get; set; } = false;
}

/// <summary>
/// 信息网格格式化配置
/// </summary>
public class InfoGridFormatConfig
{
    /// <summary>
    /// 值类型：text, number, percent, currency, filesize
    /// </summary>
    public string ValueType { get; set; } = "text";

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 后缀
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// 是否显示千分位分隔符
    /// </summary>
    public bool ShowSeparator { get; set; } = false;
}

/// <summary>
/// 信息网格数据配置
/// </summary>
public class InfoGridDataConfig
{
    /// <summary>
    /// API数据源URL
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// 数据字段映射
    /// </summary>
    public Dictionary<string, string>? FieldMapping { get; set; }

    /// <summary>
    /// 数据刷新间隔（毫秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 0;

    /// <summary>
    /// 数据过滤器
    /// </summary>
    public List<InfoGridDataFilter>? Filters { get; set; }
}

/// <summary>
/// 信息网格数据过滤器
/// </summary>
public class InfoGridDataFilter
{
    /// <summary>
    /// 字段名
    /// </summary>
    [Required]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 操作符：eq, ne, gt, gte, lt, lte, in, nin, contains
    /// </summary>
    [Required]
    public string Operator { get; set; } = "eq";

    /// <summary>
    /// 比较值
    /// </summary>
    public object? Value { get; set; }
} 