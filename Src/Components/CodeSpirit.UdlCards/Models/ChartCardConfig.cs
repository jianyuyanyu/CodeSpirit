namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// 图表卡片配置
/// </summary>
public class ChartCardConfig : UdlCardConfig
{
    /// <summary>
    /// 卡片类型：chart
    /// </summary>
    public override string Type => "chart";

    /// <summary>
    /// 图表配置
    /// </summary>
    public ChartConfig Chart { get; set; } = new();

    /// <summary>
    /// 数据源配置
    /// </summary>
    public ChartDataConfig Data { get; set; } = new();
}

/// <summary>
/// 图表配置
/// </summary>
public class ChartConfig
{
    /// <summary>
    /// 图表类型：line, bar, pie, area, scatter, radar, gauge
    /// </summary>
    [Required]
    public string Type { get; set; } = "line";

    /// <summary>
    /// 图表高度
    /// </summary>
    public int Height { get; set; } = 300;

    /// <summary>
    /// 图表宽度
    /// </summary>
    public string Width { get; set; } = "100%";

    /// <summary>
    /// ECharts配置选项
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }

    /// <summary>
    /// 主题配置
    /// </summary>
    public string Theme { get; set; } = "default";

    /// <summary>
    /// 是否响应式
    /// </summary>
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// 图表动画配置
    /// </summary>
    public ChartAnimationConfig? Animation { get; set; }

    /// <summary>
    /// 工具栏配置
    /// </summary>
    public ChartToolboxConfig? Toolbox { get; set; }
}

/// <summary>
/// 图表数据配置
/// </summary>
public class ChartDataConfig
{
    /// <summary>
    /// 静态数据
    /// </summary>
    public List<Dictionary<string, object>>? StaticData { get; set; }

    /// <summary>
    /// API数据源URL
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// 数据字段映射
    /// </summary>
    public ChartFieldMapping? FieldMapping { get; set; }

    /// <summary>
    /// 数据刷新间隔（毫秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 0;

    /// <summary>
    /// 数据过滤器
    /// </summary>
    public List<ChartDataFilter>? Filters { get; set; }
}

/// <summary>
/// 图表字段映射
/// </summary>
public class ChartFieldMapping
{
    /// <summary>
    /// X轴字段
    /// </summary>
    public string? XField { get; set; }

    /// <summary>
    /// Y轴字段
    /// </summary>
    public string? YField { get; set; }

    /// <summary>
    /// 系列字段
    /// </summary>
    public string? SeriesField { get; set; }

    /// <summary>
    /// 值字段
    /// </summary>
    public string? ValueField { get; set; }

    /// <summary>
    /// 标签字段
    /// </summary>
    public string? LabelField { get; set; }

    /// <summary>
    /// 颜色字段
    /// </summary>
    public string? ColorField { get; set; }
}

/// <summary>
/// 图表数据过滤器
/// </summary>
public class ChartDataFilter
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

/// <summary>
/// 图表动画配置
/// </summary>
public class ChartAnimationConfig
{
    /// <summary>
    /// 是否启用动画
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 动画持续时间
    /// </summary>
    public int Duration { get; set; } = 1000;

    /// <summary>
    /// 动画缓动函数
    /// </summary>
    public string Easing { get; set; } = "cubicOut";

    /// <summary>
    /// 动画延迟
    /// </summary>
    public int Delay { get; set; } = 0;
}

/// <summary>
/// 图表工具栏配置
/// </summary>
public class ChartToolboxConfig
{
    /// <summary>
    /// 是否显示工具栏
    /// </summary>
    public bool Show { get; set; } = false;

    /// <summary>
    /// 工具栏位置：top, bottom, left, right
    /// </summary>
    public string Position { get; set; } = "top";

    /// <summary>
    /// 可用工具：saveAsImage, restore, dataView, dataZoom, magicType
    /// </summary>
    public List<string> Tools { get; set; } = new();
} 