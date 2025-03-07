namespace CodeSpirit.Charts.Models
{
    /// <summary>
    /// 图表配置类
    /// </summary>
    public class ChartConfig
    {
        /// <summary>
        /// 图表唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 图表标题
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 图表副标题
        /// </summary>
        public string? Subtitle { get; set; }

        /// <summary>
        /// 图表类型
        /// </summary>
        public ChartType Type { get; set; } = ChartType.Auto;

        /// <summary>
        /// 图表子类型
        /// </summary>
        public string? SubType { get; set; }

        /// <summary>
        /// 数据源配置
        /// </summary>
        public ChartDataSource DataSource { get; set; } = new ChartDataSource();

        /// <summary>
        /// X轴配置
        /// </summary>
        public AxisConfig? XAxis { get; set; }

        /// <summary>
        /// Y轴配置
        /// </summary>
        public AxisConfig? YAxis { get; set; }

        /// <summary>
        /// 图例配置
        /// </summary>
        public LegendConfig? Legend { get; set; }

        /// <summary>
        /// 系列配置
        /// </summary>
        public List<SeriesConfig> Series { get; set; } = new List<SeriesConfig>();

        /// <summary>
        /// 工具箱配置
        /// </summary>
        public ToolboxConfig? Toolbox { get; set; }

        /// <summary>
        /// 是否启用自动刷新
        /// </summary>
        public bool AutoRefresh { get; set; }

        /// <summary>
        /// 自动刷新间隔（秒）
        /// </summary>
        public int RefreshInterval { get; set; } = 60;

        /// <summary>
        /// 交互配置
        /// </summary>
        public InteractionConfig? Interaction { get; set; }

        /// <summary>
        /// 主题配置
        /// </summary>
        public string Theme { get; set; } = "default";

        /// <summary>
        /// 附加样式
        /// </summary>
        public Dictionary<string, object>? ExtraStyles { get; set; }
    }

    /// <summary>
    /// 图表类型枚举
    /// </summary>
    public enum ChartType
    {
        [Description("折线图")]
        Line,

        [Description("柱状图")]
        Bar,

        [Description("饼图")]
        Pie,

        [Description("散点图")]
        Scatter,

        [Description("雷达图")]
        Radar,

        [Description("仪表盘")]
        Gauge,

        [Description("热力图")]
        Heatmap,

        [Description("树图")]
        Tree,

        [Description("漏斗图")]
        Funnel,

        [Description("地图")]
        Map,

        [Description("桑基图")]
        Sankey,

        [Description("关系图")]
        Graph,

        [Description("自动推荐")]
        Auto
    }

    /// <summary>
    /// 坐标轴配置
    /// </summary>
    public class AxisConfig
    {
        /// <summary>
        /// 轴标题
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 轴类型
        /// </summary>
        public string Type { get; set; } = "category";

        /// <summary>
        /// 轴数据
        /// </summary>
        public List<string>? Data { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Show { get; set; } = true;

        /// <summary>
        /// 是否反转
        /// </summary>
        public bool Inverse { get; set; }

        /// <summary>
        /// 轴线配置
        /// </summary>
        public Dictionary<string, object>? AxisLine { get; set; }

        /// <summary>
        /// 轴标签配置
        /// </summary>
        public Dictionary<string, object>? AxisLabel { get; set; }

        /// <summary>
        /// 扩展参数
        /// </summary>
        public Dictionary<string, object> ExtraOptions { get; set; }
    }

    /// <summary>
    /// 图例配置
    /// </summary>
    public class LegendConfig
    {
        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Show { get; set; } = true;

        /// <summary>
        /// 图例方向
        /// </summary>
        public string Orient { get; set; } = "horizontal";

        /// <summary>
        /// 图例位置
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// 图例数据
        /// </summary>
        public List<string>? Data { get; set; }
    }

    /// <summary>
    /// 系列配置
    /// </summary>
    public class SeriesConfig
    {
        /// <summary>
        /// 系列名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 系列类型
        /// </summary>
        public string Type { get; set; } = "line";

        /// <summary>
        /// 系列数据
        /// </summary>
        public List<object>? Data { get; set; }

        /// <summary>
        /// 系列标签
        /// </summary>
        public Dictionary<string, object>? Label { get; set; }

        /// <summary>
        /// 系列样式
        /// </summary>
        public Dictionary<string, object>? ItemStyle { get; set; }

        /// <summary>
        /// 强调样式
        /// </summary>
        public Dictionary<string, object>? Emphasis { get; set; }

        /// <summary>
        /// 堆叠选项
        /// </summary>
        public string? Stack { get; set; }

        /// <summary>
        /// 附加选项
        /// </summary>
        public Dictionary<string, object>? ExtraOptions { get; set; }
    }

    /// <summary>
    /// 工具箱配置
    /// </summary>
    public class ToolboxConfig
    {
        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Show { get; set; } = true;

        /// <summary>
        /// 工具箱位置
        /// </summary>
        public string Orient { get; set; } = "horizontal";

        /// <summary>
        /// 功能列表
        /// </summary>
        public Dictionary<string, bool> Features { get; set; } = new Dictionary<string, bool>
        {
            { "saveAsImage", true },
            { "dataView", true },
            { "restore", true },
            { "dataZoom", true },
            { "magicType", true }
        };
    }

    /// <summary>
    /// 交互配置
    /// </summary>
    public class InteractionConfig
    {
        /// <summary>
        /// 是否可拖拽
        /// </summary>
        public bool Draggable { get; set; }

        /// <summary>
        /// 提示框配置
        /// </summary>
        public Dictionary<string, object>? Tooltip { get; set; }

        /// <summary>
        /// 数据区域缩放
        /// </summary>
        public Dictionary<string, object>? DataZoom { get; set; }

        /// <summary>
        /// 图表联动
        /// </summary>
        public List<string>? LinkedCharts { get; set; }

        /// <summary>
        /// 事件响应
        /// </summary>
        public Dictionary<string, string>? Events { get; set; }
    }
} 