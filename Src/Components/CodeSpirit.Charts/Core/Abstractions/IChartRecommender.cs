namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表推荐器接口，定义了图表推荐的抽象能力
/// </summary>
public interface IChartRecommender
{
    /// <summary>
    /// 根据数据推荐图表类型
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>推荐的图表类型列表，按推荐度排序</returns>
    Task<IEnumerable<ChartRecommendation>> RecommendChartTypesAsync(object data, string providerName);
    
    /// <summary>
    /// 获取图表类型的适用性评分
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="chartType">图表类型</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>适用性评分（0-100）</returns>
    Task<int> GetChartTypeSuitabilityScoreAsync(object data, string chartType, string providerName);
    
    /// <summary>
    /// 分析数据特征
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>数据特征</returns>
    Task<DataFeatures> AnalyzeDataFeaturesAsync(object data);
    
    /// <summary>
    /// 获取图表类型的描述
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>图表类型描述</returns>
    Task<ChartTypeDescription> GetChartTypeDescriptionAsync(string chartType, string providerName);
    
    /// <summary>
    /// 获取图表类型的最佳实践
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="providerName">图表提供者名称</param>
    /// <returns>最佳实践列表</returns>
    Task<IEnumerable<string>> GetChartTypeBestPracticesAsync(string chartType, string providerName);
}

/// <summary>
/// 图表推荐结果
/// </summary>
public class ChartRecommendation
{
    /// <summary>
    /// 图表类型
    /// </summary>
    public string ChartType { get; set; } = string.Empty;
    
    /// <summary>
    /// 推荐度评分（0-100）
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// 推荐理由
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// 示例配置
    /// </summary>
    public object? ExampleConfig { get; set; }
}

/// <summary>
/// 数据特征
/// </summary>
public class DataFeatures
{
    /// <summary>
    /// 数据维度数量
    /// </summary>
    public int DimensionCount { get; set; }
    
    /// <summary>
    /// 数据指标数量
    /// </summary>
    public int MetricCount { get; set; }
    
    /// <summary>
    /// 数据行数
    /// </summary>
    public int RowCount { get; set; }
    
    /// <summary>
    /// 是否包含时间序列数据
    /// </summary>
    public bool HasTimeSeriesData { get; set; }
    
    /// <summary>
    /// 是否包含地理数据
    /// </summary>
    public bool HasGeoData { get; set; }
    
    /// <summary>
    /// 是否包含层次结构数据
    /// </summary>
    public bool HasHierarchicalData { get; set; }
    
    /// <summary>
    /// 数据分布类型
    /// </summary>
    public string DataDistributionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 其他特征
    /// </summary>
    public Dictionary<string, object> AdditionalFeatures { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// 图表类型描述
/// </summary>
public class ChartTypeDescription
{
    /// <summary>
    /// 图表类型
    /// </summary>
    public string ChartType { get; set; } = string.Empty;
    
    /// <summary>
    /// 图表名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 图表描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 适用场景
    /// </summary>
    public List<string> UseCases { get; set; } = new List<string>();
    
    /// <summary>
    /// 优点
    /// </summary>
    public List<string> Advantages { get; set; } = new List<string>();
    
    /// <summary>
    /// 缺点
    /// </summary>
    public List<string> Disadvantages { get; set; } = new List<string>();
    
    /// <summary>
    /// 最佳实践
    /// </summary>
    public List<string> BestPractices { get; set; } = new List<string>();
}