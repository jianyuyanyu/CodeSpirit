namespace CodeSpirit.Charts.Attributes;

    /// <summary>
    /// 图表特性，用于标记控制器方法返回图表
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ChartAttribute : Attribute
    {
        /// <summary>
        /// 图表类型
        /// </summary>
        public string ChartType { get; }

        /// <summary>
        /// 图表标题
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 图表描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 图表主题
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// 是否自动刷新
        /// </summary>
        public bool AutoRefresh { get; set; }

        /// <summary>
        /// 刷新间隔（毫秒）
        /// </summary>
        public int RefreshInterval { get; set; } = 5000;

        /// <summary>
        /// 是否显示工具箱
        /// </summary>
        public bool ShowToolbox { get; set; }

        /// <summary>
        /// 是否启用导出功能
        /// </summary>
        public bool EnableExport { get; set; }

        /// <summary>
        /// 是否启用交互功能
        /// </summary>
        public bool EnableInteraction { get; set; } = true;

        /// <summary>
        /// 初始化图表特性
        /// </summary>
        /// <param name="chartType">图表类型，如 line, bar, pie 等，使用 auto 表示自动推荐</param>
        public ChartAttribute(string chartType = "auto")
        {
            ChartType = chartType;
        }
    }