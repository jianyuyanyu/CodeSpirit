namespace CodeSpirit.Charts.Attributes
{
    /// <summary>
    /// 排序方向
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// 升序
        /// </summary>
        Ascending,
        
        /// <summary>
        /// 降序
        /// </summary>
        Descending
    }

    /// <summary>
    /// 图表数据特性，用于指定数据分析配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class ChartDataAttribute : Attribute
    {
        /// <summary>
        /// 维度字段
        /// </summary>
        public string? DimensionField { get; set; }
        
        /// <summary>
        /// 多个维度字段
        /// </summary>
        public string[]? DimensionFields { get; set; }
        
        /// <summary>
        /// 指标字段
        /// </summary>
        public string[]? MetricFields { get; set; }
        
        /// <summary>
        /// 数据排序字段
        /// </summary>
        public string? SortField { get; set; }
        
        /// <summary>
        /// 排序方向
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
        
        /// <summary>
        /// 数据限制
        /// </summary>
        public int Limit { get; set; } = 0; // 0表示不限制
        
        /// <summary>
        /// 自定义SQL查询
        /// </summary>
        public string? SqlQuery { get; set; }
        
        /// <summary>
        /// 是否启用数据钻取
        /// </summary>
        public bool EnableDrill { get; set; } = false;
        
        /// <summary>
        /// 钻取配置
        /// </summary>
        public string? DrillConfig { get; set; }
        
        /// <summary>
        /// 是否启用趋势分析
        /// </summary>
        public bool EnableTrendAnalysis { get; set; } = false;
        
        /// <summary>
        /// 是否启用异常检测
        /// </summary>
        public bool EnableAnomalyDetection { get; set; } = false;
        
        /// <summary>
        /// 是否启用预测
        /// </summary>
        public bool EnableForecasting { get; set; } = false;
        
        /// <summary>
        /// 预测步数
        /// </summary>
        public int ForecastSteps { get; set; } = 5;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartDataAttribute()
        {
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dimensionField">维度字段</param>
        /// <param name="metricFields">指标字段</param>
        public ChartDataAttribute(string dimensionField, string[] metricFields)
        {
            DimensionField = dimensionField;
            MetricFields = metricFields;
        }
    }
} 