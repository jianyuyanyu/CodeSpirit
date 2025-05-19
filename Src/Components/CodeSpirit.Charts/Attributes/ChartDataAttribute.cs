namespace CodeSpirit.Charts.Attributes;

    /// <summary>
    /// 图表数据特性，用于指定数据映射
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ChartDataAttribute : Attribute
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
        /// 类别字段
        /// </summary>
        public string? CategoryField { get; set; }

        /// <summary>
        /// 标签字段
        /// </summary>
        public string? LabelField { get; set; }

        /// <summary>
        /// 颜色字段
        /// </summary>
        public string? ColorField { get; set; }

        /// <summary>
        /// 大小字段
        /// </summary>
        public string? SizeField { get; set; }

        /// <summary>
        /// 分组字段
        /// </summary>
        public string? GroupField { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// 是否升序排序
        /// </summary>
        public bool? SortAscending { get; set; }

        /// <summary>
        /// 数据转换器
        /// </summary>
        public Type? Transformer { get; set; }

        /// <summary>
        /// 维度字段
        /// </summary>
        public string? DimensionField { get; set; }

        /// <summary>
        /// 维度字段集合
        /// </summary>
        public string[]? DimensionFields { get; set; }

        /// <summary>
        /// 度量字段集合
        /// </summary>
        public string[]? MetricFields { get; set; }

        /// <summary>
        /// 是否启用趋势分析
        /// </summary>
        public bool EnableTrendAnalysis { get; set; }

        /// <summary>
        /// 是否启用异常检测
        /// </summary>
        public bool EnableAnomalyDetection { get; set; }

        /// <summary>
        /// 是否启用预测
        /// </summary>
        public bool EnableForecasting { get; set; }

        /// <summary>
        /// 预测步数
        /// </summary>
        public int ForecastSteps { get; set; } = 5;

        /// <summary>
        /// 初始化图表数据特性
        /// </summary>
        public ChartDataAttribute()
        {
        }

        /// <summary>
        /// 初始化图表数据特性
        /// </summary>
        /// <param name="xField">X轴字段</param>
        /// <param name="yField">Y轴字段</param>
        public ChartDataAttribute(string xField, string yField)
        {
            XField = xField;
            YField = yField;
        }

        /// <summary>
        /// 初始化图表数据特性
        /// </summary>
        /// <param name="valueField">值字段</param>
        /// <param name="categoryField">类别字段</param>
        /// <param name="seriesField">系列字段</param>
        public ChartDataAttribute(string valueField, string categoryField, string? seriesField = null)
        {
            ValueField = valueField;
            CategoryField = categoryField;
            SeriesField = seriesField;
        }
    }