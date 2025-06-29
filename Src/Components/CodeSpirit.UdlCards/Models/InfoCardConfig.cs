namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// 信息卡片配置
/// </summary>
public class InfoCardConfig : UdlCardConfig
{
    /// <summary>
    /// 卡片类型：info
    /// </summary>
    public override string Type => "info";

    /// <summary>
    /// 信息内容配置
    /// </summary>
    public InfoContentConfig Content { get; set; } = new();

    /// <summary>
    /// 布局配置
    /// </summary>
    public InfoLayoutConfig? Layout { get; set; }

    /// <summary>
    /// 操作按钮配置
    /// </summary>
    public List<InfoActionConfig>? Actions { get; set; }
}

/// <summary>
/// 信息内容配置
/// </summary>
public class InfoContentConfig
{
    /// <summary>
    /// 内容类型：text, html, template, list, properties
    /// </summary>
    [Required]
    public string Type { get; set; } = "text";

    /// <summary>
    /// 文本内容
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// HTML内容
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// 模板内容
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// 列表项配置（用于list类型）
    /// </summary>
    public List<InfoListItem>? ListItems { get; set; }

    /// <summary>
    /// 属性项配置（用于properties类型）
    /// </summary>
    public List<InfoPropertyItem>? PropertyItems { get; set; }

    /// <summary>
    /// 数据源API
    /// </summary>
    public string? ApiUrl { get; set; }
}

/// <summary>
/// 信息列表项配置
/// </summary>
public class InfoListItem
{
    /// <summary>
    /// 项目标题
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 项目内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 项目图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 项目链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 项目徽章
    /// </summary>
    public InfoBadgeConfig? Badge { get; set; }

    /// <summary>
    /// 显示条件
    /// </summary>
    public string? VisibleOn { get; set; }
}

/// <summary>
/// 信息属性项配置
/// </summary>
public class InfoPropertyItem
{
    /// <summary>
    /// 属性名称
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 属性标签
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 属性值
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 值类型：text, number, date, boolean, link, image, tag
    /// </summary>
    public string ValueType { get; set; } = "text";

    /// <summary>
    /// 格式化配置
    /// </summary>
    public InfoFormatConfig? Format { get; set; }

    /// <summary>
    /// 显示条件
    /// </summary>
    public string? VisibleOn { get; set; }

    /// <summary>
    /// 是否可复制
    /// </summary>
    public bool Copyable { get; set; } = false;
}

/// <summary>
/// 信息徽章配置
/// </summary>
public class InfoBadgeConfig
{
    /// <summary>
    /// 徽章文本
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 徽章数量
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// 徽章颜色
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 徽章状态：default, processing, success, error, warning
    /// </summary>
    public string Status { get; set; } = "default";

    /// <summary>
    /// 是否显示小圆点
    /// </summary>
    public bool ShowDot { get; set; } = false;
}

/// <summary>
/// 信息格式化配置
/// </summary>
public class InfoFormatConfig
{
    /// <summary>
    /// 日期格式
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// 数字小数位数
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 是否显示千分位分隔符
    /// </summary>
    public bool ShowSeparator { get; set; } = false;

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
}

/// <summary>
/// 信息布局配置
/// </summary>
public class InfoLayoutConfig
{
    /// <summary>
    /// 布局方式：vertical, horizontal, grid
    /// </summary>
    public string Mode { get; set; } = "vertical";

    /// <summary>
    /// 网格列数（用于grid模式）
    /// </summary>
    public int? GridColumns { get; set; }

    /// <summary>
    /// 标签宽度（用于horizontal模式）
    /// </summary>
    public string? LabelWidth { get; set; }

    /// <summary>
    /// 标签对齐方式：left, center, right
    /// </summary>
    public string LabelAlign { get; set; } = "left";

    /// <summary>
    /// 间距配置
    /// </summary>
    public InfoSpacingConfig? Spacing { get; set; }
}

/// <summary>
/// 信息间距配置
/// </summary>
public class InfoSpacingConfig
{
    /// <summary>
    /// 项目间垂直间距
    /// </summary>
    public string? ItemSpacing { get; set; }

    /// <summary>
    /// 标签与值之间的间距
    /// </summary>
    public string? LabelValueSpacing { get; set; }

    /// <summary>
    /// 内边距
    /// </summary>
    public string? Padding { get; set; }

    /// <summary>
    /// 外边距
    /// </summary>
    public string? Margin { get; set; }
}

/// <summary>
/// 信息操作配置
/// </summary>
public class InfoActionConfig
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作标签
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型：button, link
    /// </summary>
    public string Type { get; set; } = "button";

    /// <summary>
    /// 按钮级别：primary, secondary, success, warning, danger, info
    /// </summary>
    public string Level { get; set; } = "secondary";

    /// <summary>
    /// 按钮大小：xs, sm, md, lg
    /// </summary>
    public string Size { get; set; } = "md";

    /// <summary>
    /// 操作图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 操作URL或脚本
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool RequireConfirm { get; set; } = false;

    /// <summary>
    /// 确认提示文本
    /// </summary>
    public string? ConfirmText { get; set; }

    /// <summary>
    /// 显示条件
    /// </summary>
    public string? VisibleOn { get; set; }

    /// <summary>
    /// 禁用条件
    /// </summary>
    public string? DisabledOn { get; set; }
} 