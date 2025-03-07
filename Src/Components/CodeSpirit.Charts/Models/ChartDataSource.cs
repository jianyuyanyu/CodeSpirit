namespace CodeSpirit.Charts.Models
{
    /// <summary>
    /// 图表数据源配置
    /// </summary>
    public class ChartDataSource
    {
        /// <summary>
        /// 数据源类型
        /// </summary>
        public DataSourceType Type { get; set; } = DataSourceType.Api;

        /// <summary>
        /// API接口地址
        /// </summary>
        public string? ApiUrl { get; set; }

        /// <summary>
        /// API请求方法
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// 请求参数
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// 静态数据
        /// </summary>
        public object? StaticData { get; set; }

        /// <summary>
        /// 数据映射配置
        /// </summary>
        public DataMapping? Mapping { get; set; }
        
        /// <summary>
        /// 数据转换器
        /// </summary>
        public List<DataTransformer>? Transformers { get; set; }
        
        /// <summary>
        /// 自动分析配置
        /// </summary>
        public DataAnalysisConfig? Analysis { get; set; }
    }

    /// <summary>
    /// 数据源类型
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// API数据源
        /// </summary>
        Api,
        
        /// <summary>
        /// 静态数据
        /// </summary>
        Static,
        
        /// <summary>
        /// 从当前数据中提取
        /// </summary>
        Current
    }

    /// <summary>
    /// 数据映射配置
    /// </summary>
    public class DataMapping
    {
        /// <summary>
        /// 数据路径（如 data.items 或 data[0].values）
        /// </summary>
        public string? DataPath { get; set; }

        /// <summary>
        /// 维度字段
        /// </summary>
        public string? DimensionField { get; set; }

        /// <summary>
        /// 多个维度字段
        /// </summary>
        public List<string>? DimensionFields { get; set; }

        /// <summary>
        /// 指标字段
        /// </summary>
        public string? MetricField { get; set; }

        /// <summary>
        /// 多个指标字段
        /// </summary>
        public List<string>? MetricFields { get; set; }

        /// <summary>
        /// 系列字段（用于多系列数据）
        /// </summary>
        public string? SeriesField { get; set; }

        /// <summary>
        /// 映射函数（JSON格式的字符串函数）
        /// </summary>
        public string? MappingFunction { get; set; }
    }

    /// <summary>
    /// 数据转换器
    /// </summary>
    public class DataTransformer
    {
        /// <summary>
        /// 转换类型
        /// </summary>
        public TransformerType Type { get; set; }

        /// <summary>
        /// 转换参数
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// 转换器类型
    /// </summary>
    public enum TransformerType
    {
        /// <summary>
        /// 排序
        /// </summary>
        Sort,

        /// <summary>
        /// 过滤
        /// </summary>
        Filter,

        /// <summary>
        /// 限制数量
        /// </summary>
        Limit,

        /// <summary>
        /// 分组
        /// </summary>
        Group,

        /// <summary>
        /// 聚合
        /// </summary>
        Aggregate,

        /// <summary>
        /// 格式化
        /// </summary>
        Format,

        /// <summary>
        /// 计算
        /// </summary>
        Calculate,

        /// <summary>
        /// 自定义
        /// </summary>
        Custom
    }

    /// <summary>
    /// 数据分析配置
    /// </summary>
    public class DataAnalysisConfig
    {
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
        /// 分析方法
        /// </summary>
        public string? AnalysisMethod { get; set; }

        /// <summary>
        /// 分析参数
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }
    }
} 