namespace CodeSpirit.Charts.Analysis
{
    /// <summary>
    /// 数据结构信息
    /// </summary>
    public class DataStructureInfo
    {
        /// <summary>
        /// 数据行数
        /// </summary>
        public int RowCount { get; set; }
        
        /// <summary>
        /// 识别出的维度字段
        /// </summary>
        public List<string> DimensionFields { get; set; } = new List<string>();
        
        /// <summary>
        /// 识别出的指标字段
        /// </summary>
        public List<string> MetricFields { get; set; } = new List<string>();
        
        /// <summary>
        /// 字段类型映射
        /// </summary>
        public Dictionary<string, Type> FieldTypes { get; set; } = new Dictionary<string, Type>();
        
        /// <summary>
        /// 字段值示例
        /// </summary>
        public Dictionary<string, object?> FieldSamples { get; set; } = new Dictionary<string, object?>();
    }
    
    /// <summary>
    /// 数据特征
    /// </summary>
    public class DataFeatures
    {
        /// <summary>
        /// 时间序列相关性
        /// </summary>
        public bool IsTimeSeries { get; set; }
        
        /// <summary>
        /// 是否存在明显趋势
        /// </summary>
        public bool HasTrend { get; set; }
        
        /// <summary>
        /// 是否存在周期性
        /// </summary>
        public bool HasSeasonality { get; set; }
        
        /// <summary>
        /// 数据是否具有分类特性
        /// </summary>
        public bool IsCategorical { get; set; }
        
        /// <summary>
        /// 数据是否具有连续特性
        /// </summary>
        public bool IsContinuous { get; set; }
        
        /// <summary>
        /// 是否存在异常值
        /// </summary>
        public bool HasOutliers { get; set; }
        
        /// <summary>
        /// 指标字段的基本统计信息
        /// </summary>
        public Dictionary<string, MetricStats> MetricStatistics { get; set; } = new Dictionary<string, MetricStats>();
    }
    
    /// <summary>
    /// 指标统计信息
    /// </summary>
    public class MetricStats
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; set; }
        
        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; set; }
        
        /// <summary>
        /// 平均值
        /// </summary>
        public double Average { get; set; }
        
        /// <summary>
        /// 中位数
        /// </summary>
        public double Median { get; set; }
        
        /// <summary>
        /// 标准差
        /// </summary>
        public double StdDev { get; set; }
    }
    
    /// <summary>
    /// 数据关联信息
    /// </summary>
    public class DataCorrelation
    {
        /// <summary>
        /// 字段1
        /// </summary>
        public string Field1 { get; set; } = string.Empty;
        
        /// <summary>
        /// 字段2
        /// </summary>
        public string Field2 { get; set; } = string.Empty;
        
        /// <summary>
        /// 相关系数
        /// </summary>
        public double Coefficient { get; set; }
        
        /// <summary>
        /// 关联强度描述
        /// </summary>
        public string Strength { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 数据模式
    /// </summary>
    public class DataPattern
    {
        /// <summary>
        /// 模式类型
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// 模式描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 可信度
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// 相关字段
        /// </summary>
        public List<string> RelatedFields { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// 数据分析器接口
    /// </summary>
    public interface IDataAnalyzer
    {
        /// <summary>
        /// 分析数据结构
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>数据结构信息</returns>
        DataStructureInfo AnalyzeDataStructure(object data);
        
        /// <summary>
        /// 提取数据特征
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>数据特征信息</returns>
        DataFeatures ExtractDataFeatures(object data);
        
        /// <summary>
        /// 检测数据关联性
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>数据关联信息</returns>
        List<DataCorrelation> DetectCorrelations(object data);
        
        /// <summary>
        /// 识别数据模式
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>数据模式信息</returns>
        List<DataPattern> IdentifyPatterns(object data);
    }
} 