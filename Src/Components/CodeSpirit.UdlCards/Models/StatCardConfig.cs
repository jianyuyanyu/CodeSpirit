namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// 统计卡片配置
/// </summary>
public class StatCardConfig : UdlCardConfig
{
    /// <summary>
    /// 卡片类型：stat
    /// </summary>
    public override string Type => "stat";

    /// <summary>
    /// 统计数据配置
    /// </summary>
    public StatDataConfig Data { get; set; } = new();

    /// <summary>
    /// 图标配置
    /// </summary>
    public StatIconConfig? Icon { get; set; }

    /// <summary>
    /// 趋势配置
    /// </summary>
    public StatTrendConfig? Trend { get; set; }

    /// <summary>
    /// 进度条配置
    /// </summary>
    public StatProgressConfig? Progress { get; set; }

    /// <summary>
    /// 数值动画配置
    /// </summary>
    public StatAnimationConfig? Animation { get; set; }
}

/// <summary>
/// 统计数据配置
/// </summary>
public class StatDataConfig
{
    /// <summary>
    /// 统计值
    /// </summary>
    [Required]
    public object Value { get; set; } = 0;

    /// <summary>
    /// 显示标签
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 数值单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 数值前缀
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 数值后缀
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// 数值格式化器：number, currency, percent, filesize
    /// </summary>
    public string? Formatter { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 是否显示千分位分隔符
    /// </summary>
    public bool ShowSeparator { get; set; } = true;

    /// <summary>
    /// API数据源URL
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// API数据字段映射
    /// </summary>
    public Dictionary<string, string>? FieldMapping { get; set; }
}

/// <summary>
/// 统计图标配置
/// </summary>
public class StatIconConfig
{
    /// <summary>
    /// 图标名称或URL
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标位置：left, right, top, bottom
    /// </summary>
    public string Position { get; set; } = "left";

    /// <summary>
    /// 图标大小：xs, sm, md, lg, xl
    /// </summary>
    public string Size { get; set; } = "md";

    /// <summary>
    /// 图标颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 图标背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 是否显示边框
    /// </summary>
    public bool ShowBorder { get; set; } = false;

    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// 自定义样式
    /// </summary>
    public Dictionary<string, object>? Style { get; set; }
}

/// <summary>
/// 统计趋势配置
/// </summary>
public class StatTrendConfig
{
    /// <summary>
    /// 趋势方向：up, down, stable
    /// </summary>
    public string Direction { get; set; } = "stable";

    /// <summary>
    /// 趋势值
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// 是否显示为百分比
    /// </summary>
    public bool IsPercentage { get; set; } = true;

    /// <summary>
    /// 趋势文本
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 自定义颜色配置
    /// </summary>
    public StatTrendColorConfig? Colors { get; set; }
}

/// <summary>
/// 趋势颜色配置
/// </summary>
public class StatTrendColorConfig
{
    /// <summary>
    /// 上升趋势颜色
    /// </summary>
    public string Up { get; set; } = "#52c41a";

    /// <summary>
    /// 下降趋势颜色
    /// </summary>
    public string Down { get; set; } = "#ff4d4f";

    /// <summary>
    /// 稳定趋势颜色
    /// </summary>
    public string Stable { get; set; } = "#faad14";
}

/// <summary>
/// 统计进度条配置
/// </summary>
public class StatProgressConfig
{
    /// <summary>
    /// 目标值
    /// </summary>
    [Required]
    public decimal Target { get; set; }

    /// <summary>
    /// 是否显示进度条
    /// </summary>
    public bool Show { get; set; } = true;

    /// <summary>
    /// 进度条高度
    /// </summary>
    public int Height { get; set; } = 6;

    /// <summary>
    /// 是否显示百分比文本
    /// </summary>
    public bool ShowText { get; set; } = true;

    /// <summary>
    /// 进度条颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// 统计动画配置
/// </summary>
public class StatAnimationConfig
{
    /// <summary>
    /// 是否启用数值动画
    /// </summary>
    public bool EnableValueAnimation { get; set; } = true;

    /// <summary>
    /// 动画持续时间（毫秒）
    /// </summary>
    public int Duration { get; set; } = 2000;

    /// <summary>
    /// 动画缓动函数：ease, ease-in, ease-out, ease-in-out, linear
    /// </summary>
    public string Easing { get; set; } = "ease-out";

    /// <summary>
    /// 动画延迟（毫秒）
    /// </summary>
    public int Delay { get; set; } = 0;
} 