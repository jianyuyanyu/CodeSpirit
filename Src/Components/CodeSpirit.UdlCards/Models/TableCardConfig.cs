namespace CodeSpirit.UdlCards.Models;

/// <summary>
/// 表格卡片配置
/// </summary>
public class TableCardConfig : UdlCardConfig
{
    /// <summary>
    /// 卡片类型：table
    /// </summary>
    public override string Type => "table";

    /// <summary>
    /// 表格配置
    /// </summary>
    public TableConfig Table { get; set; } = new();

    /// <summary>
    /// 数据源配置
    /// </summary>
    public TableDataConfig Data { get; set; } = new();

    /// <summary>
    /// 搜索配置
    /// </summary>
    public TableSearchConfig? Search { get; set; }

    /// <summary>
    /// 操作配置
    /// </summary>
    public TableActionConfig? Actions { get; set; }
}

/// <summary>
/// 表格配置
/// </summary>
public class TableConfig
{
    /// <summary>
    /// 表格列配置
    /// </summary>
    [Required]
    public List<TableColumn> Columns { get; set; } = new();

    /// <summary>
    /// 是否显示序号列
    /// </summary>
    public bool ShowIndex { get; set; } = false;

    /// <summary>
    /// 是否显示选择列
    /// </summary>
    public bool ShowSelection { get; set; } = false;

    /// <summary>
    /// 表格大小：small, middle, large
    /// </summary>
    public string Size { get; set; } = "middle";

    /// <summary>
    /// 是否显示边框
    /// </summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>
    /// 是否显示斑马纹
    /// </summary>
    public bool ShowStripe { get; set; } = true;

    /// <summary>
    /// 是否悬停高亮
    /// </summary>
    public bool ShowHover { get; set; } = true;

    /// <summary>
    /// 分页配置
    /// </summary>
    public TablePaginationConfig? Pagination { get; set; }

    /// <summary>
    /// 排序配置
    /// </summary>
    public TableSortConfig? Sort { get; set; }
}

/// <summary>
/// 表格列配置
/// </summary>
public class TableColumn
{
    /// <summary>
    /// 列标识符
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 列标题
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 列类型：text, number, date, status, mapping, operation, image, link
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// 列宽度
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// 是否固定列
    /// </summary>
    public string? Fixed { get; set; }

    /// <summary>
    /// 是否可排序
    /// </summary>
    public bool Sortable { get; set; } = false;

    /// <summary>
    /// 是否可搜索
    /// </summary>
    public bool Searchable { get; set; } = false;

    /// <summary>
    /// 对齐方式：left, center, right
    /// </summary>
    public string Align { get; set; } = "left";

    /// <summary>
    /// 数据映射（用于status类型）
    /// </summary>
    public Dictionary<string, object>? Mapping { get; set; }

    /// <summary>
    /// 格式化配置
    /// </summary>
    public TableColumnFormat? Format { get; set; }

    /// <summary>
    /// 显示条件
    /// </summary>
    public string? VisibleOn { get; set; }

    /// <summary>
    /// 自定义模板
    /// </summary>
    public string? Template { get; set; }
}

/// <summary>
/// 表格列格式化配置
/// </summary>
public class TableColumnFormat
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
    /// 货币符号
    /// </summary>
    public string? CurrencySymbol { get; set; }

    /// <summary>
    /// 文本截断长度
    /// </summary>
    public int? TruncateLength { get; set; }
}

/// <summary>
/// 表格数据配置
/// </summary>
public class TableDataConfig
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
    /// 每页显示数量
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 默认排序字段
    /// </summary>
    public string? DefaultSort { get; set; }

    /// <summary>
    /// 默认排序方向：asc, desc
    /// </summary>
    public string DefaultSortOrder { get; set; } = "asc";
}

/// <summary>
/// 表格搜索配置
/// </summary>
public class TableSearchConfig
{
    /// <summary>
    /// 是否启用搜索
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 搜索模式：simple, advanced
    /// </summary>
    public string Mode { get; set; } = "simple";

    /// <summary>
    /// 搜索框占位符
    /// </summary>
    public string Placeholder { get; set; } = "请输入搜索关键词";

    /// <summary>
    /// 高级搜索字段
    /// </summary>
    public List<TableSearchField>? AdvancedFields { get; set; }
}

/// <summary>
/// 表格搜索字段
/// </summary>
public class TableSearchField
{
    /// <summary>
    /// 字段名
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段标签
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 字段类型：text, number, date, select
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// 选项列表（用于select类型）
    /// </summary>
    public List<Dictionary<string, object>>? Options { get; set; }
}

/// <summary>
/// 表格操作配置
/// </summary>
public class TableActionConfig
{
    /// <summary>
    /// 行操作
    /// </summary>
    public List<TableRowAction>? RowActions { get; set; }

    /// <summary>
    /// 批量操作
    /// </summary>
    public List<TableBatchAction>? BatchActions { get; set; }

    /// <summary>
    /// 工具栏操作
    /// </summary>
    public List<TableToolbarAction>? ToolbarActions { get; set; }
}

/// <summary>
/// 表格行操作
/// </summary>
public class TableRowAction
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
}

/// <summary>
/// 表格批量操作
/// </summary>
public class TableBatchAction
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
    public bool RequireConfirm { get; set; } = true;

    /// <summary>
    /// 确认提示文本
    /// </summary>
    public string? ConfirmText { get; set; }
}

/// <summary>
/// 表格工具栏操作
/// </summary>
public class TableToolbarAction
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
    /// 操作类型：button, dropdown
    /// </summary>
    public string Type { get; set; } = "button";

    /// <summary>
    /// 操作图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 操作URL或脚本
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// 下拉选项（用于dropdown类型）
    /// </summary>
    public List<TableToolbarAction>? Children { get; set; }
}

/// <summary>
/// 表格分页配置
/// </summary>
public class TablePaginationConfig
{
    /// <summary>
    /// 是否启用分页
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 分页位置：top, bottom, both
    /// </summary>
    public string Position { get; set; } = "bottom";

    /// <summary>
    /// 每页大小选项
    /// </summary>
    public List<int> PageSizeOptions { get; set; } = new() { 10, 20, 50, 100 };

    /// <summary>
    /// 是否显示快速跳转
    /// </summary>
    public bool ShowQuickJumper { get; set; } = true;

    /// <summary>
    /// 是否显示总数
    /// </summary>
    public bool ShowTotal { get; set; } = true;
}

/// <summary>
/// 表格排序配置
/// </summary>
public class TableSortConfig
{
    /// <summary>
    /// 是否启用多列排序
    /// </summary>
    public bool MultiSort { get; set; } = false;

    /// <summary>
    /// 排序模式：local, remote
    /// </summary>
    public string Mode { get; set; } = "remote";

    /// <summary>
    /// 排序图标位置：left, right
    /// </summary>
    public string IconPosition { get; set; } = "right";
} 