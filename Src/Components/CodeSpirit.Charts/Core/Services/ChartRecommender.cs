using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Core.Services;

/// <summary>
/// 图表推荐器的默认实现
/// </summary>
public class ChartRecommender : IChartRecommender
{
    private readonly ILogger<ChartRecommender> _logger;
    private readonly Dictionary<string, ChartTypeDescription> _chartTypeDescriptions;

    public ChartRecommender(ILogger<ChartRecommender> logger)
    {
        _logger = logger;
        _chartTypeDescriptions = InitializeChartTypeDescriptions();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChartRecommendation>> RecommendChartTypesAsync(object data, string providerName)
    {
        try
        {
            // 分析数据特征
            var features = await AnalyzeDataFeaturesAsync(data);
            
            // 根据数据特征评估每种图表类型的适用性
            var recommendations = new List<ChartRecommendation>();
            
            foreach (var (chartType, description) in _chartTypeDescriptions)
            {
                var score = await GetChartTypeSuitabilityScoreAsync(data, chartType, providerName);
                if (score > 0)
                {
                    recommendations.Add(new ChartRecommendation
                    {
                        ChartType = chartType,
                        Score = score,
                        Reason = GenerateRecommendationReason(features, description),
                        ExampleConfig = GenerateExampleConfig(chartType, data)
                    });
                }
            }

            // 按评分降序排序
            return recommendations.OrderByDescending(r => r.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending chart types for provider '{ProviderName}'", providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetChartTypeSuitabilityScoreAsync(object data, string chartType, string providerName)
    {
        try
        {
            var features = await AnalyzeDataFeaturesAsync(data);
            
            // 根据图表类型和数据特征计算适用性评分
            return chartType.ToLowerInvariant() switch
            {
                "line" => CalculateLineChartScore(features),
                "bar" => CalculateBarChartScore(features),
                "pie" => CalculatePieChartScore(features),
                "scatter" => CalculateScatterChartScore(features),
                "radar" => CalculateRadarChartScore(features),
                "tree" => CalculateTreeChartScore(features),
                "treemap" => CalculateTreemapChartScore(features),
                "sankey" => CalculateSankeyChartScore(features),
                "heatmap" => CalculateHeatmapChartScore(features),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating suitability score for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<DataFeatures> AnalyzeDataFeaturesAsync(object data)
    {
        try
        {
            var features = new DataFeatures();

            // 根据数据类型进行不同的分析
            switch (data)
            {
                case System.Data.DataTable dt:
                    AnalyzeDataTable(dt, features);
                    break;
                case IEnumerable<object> collection:
                    AnalyzeCollection(collection, features);
                    break;
                case string jsonString:
                    AnalyzeJson(jsonString, features);
                    break;
                default:
                    throw new ArgumentException($"Unsupported data type: {data.GetType().Name}");
            }

            return Task.FromResult(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing data features");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<ChartTypeDescription> GetChartTypeDescriptionAsync(string chartType, string providerName)
    {
        try
        {
            if (!_chartTypeDescriptions.TryGetValue(chartType.ToLowerInvariant(), out var description))
            {
                throw new KeyNotFoundException($"Description not found for chart type: {chartType}");
            }

            return Task.FromResult(description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting description for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetChartTypeBestPracticesAsync(string chartType, string providerName)
    {
        try
        {
            var description = await GetChartTypeDescriptionAsync(chartType, providerName);
            return description.BestPractices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best practices for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    #region Private Methods

    private Dictionary<string, ChartTypeDescription> InitializeChartTypeDescriptions()
    {
        return new Dictionary<string, ChartTypeDescription>
        {
            ["line"] = new ChartTypeDescription
            {
                ChartType = "line",
                Name = "折线图",
                Description = "用于显示数据在连续时间或有序类别上的变化趋势",
                UseCases = new List<string>
                {
                    "时间序列数据展示",
                    "趋势分析",
                    "连续数据变化"
                },
                Advantages = new List<string>
                {
                    "直观展示数据变化趋势",
                    "适合展示连续数据",
                    "可以同时比较多个系列"
                },
                Disadvantages = new List<string>
                {
                    "不适合展示离散数据",
                    "当数据点过多时可能显得杂乱"
                },
                BestPractices = new List<string>
                {
                    "确保X轴数据有序",
                    "避免使用过多的数据系列",
                    "考虑使用平滑曲线提高可读性"
                }
            },
            ["bar"] = new ChartTypeDescription
            {
                ChartType = "bar",
                Name = "柱状图",
                Description = "用于比较不同类别之间的数值大小",
                UseCases = new List<string>
                {
                    "类别数据比较",
                    "排名展示",
                    "分组数据对比"
                },
                Advantages = new List<string>
                {
                    "直观比较数值大小",
                    "适合展示分类数据",
                    "支持分组和堆叠"
                },
                Disadvantages = new List<string>
                {
                    "不适合展示过多类别",
                    "不适合展示连续变化趋势"
                },
                BestPractices = new List<string>
                {
                    "从大到小排序以提高可读性",
                    "使用合适的间距",
                    "考虑使用水平方向的条形图"
                }
            },
            // 添加更多图表类型的描述...
        };
    }

    private void AnalyzeDataTable(System.Data.DataTable dt, DataFeatures features)
    {
        features.DimensionCount = dt.Columns.Count;
        features.RowCount = dt.Rows.Count;
        
        // 分析列类型
        foreach (System.Data.DataColumn column in dt.Columns)
        {
            if (column.DataType == typeof(DateTime))
            {
                features.HasTimeSeriesData = true;
            }
            // 添加更多类型分析...
        }
    }

    private void AnalyzeCollection(IEnumerable<object> collection, DataFeatures features)
    {
        var list = collection.ToList();
        features.RowCount = list.Count;
        
        if (list.Any())
        {
            var firstItem = list.First();
            if (firstItem != null)
            {
                var properties = firstItem.GetType().GetProperties();
                features.DimensionCount = properties.Length;
                
                // 分析属性类型
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(DateTime))
                    {
                        features.HasTimeSeriesData = true;
                    }
                    // 添加更多类型分析...
                }
            }
        }
    }

    private void AnalyzeJson(string jsonString, DataFeatures features)
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<object>(jsonString);
        if (data is IEnumerable<object> collection)
        {
            AnalyzeCollection(collection, features);
        }
        // 添加更多 JSON 分析逻辑...
    }

    private string GenerateRecommendationReason(DataFeatures features, ChartTypeDescription description)
    {
        var reasons = new List<string>();

        // 根据数据特征和图表类型的特点生成推荐理由
        if (features.HasTimeSeriesData && description.ChartType == "line")
        {
            reasons.Add("数据包含时间序列，适合使用折线图展示变化趋势");
        }

        if (features.DimensionCount <= 2 && description.ChartType == "pie")
        {
            reasons.Add("数据维度较少，适合使用饼图展示占比关系");
        }

        // 添加更多推荐理由逻辑...

        return string.Join("；", reasons);
    }

    private object? GenerateExampleConfig(string chartType, object data)
    {
        // 根据图表类型和数据生成示例配置
        return chartType.ToLowerInvariant() switch
        {
            "line" => GenerateLineChartExampleConfig(data),
            "bar" => GenerateBarChartExampleConfig(data),
            "pie" => GeneratePieChartExampleConfig(data),
            _ => null
        };
    }

    #region Chart Score Calculation Methods

    private int CalculateLineChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算折线图的适用性评分
        if (features.HasTimeSeriesData) score += 40;
        if (features.DimensionCount >= 2) score += 30;
        if (features.RowCount >= 5) score += 30;
        
        return score;
    }

    private int CalculateBarChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算柱状图的适用性评分
        if (!features.HasTimeSeriesData) score += 30;
        if (features.DimensionCount >= 2) score += 35;
        if (features.RowCount <= 20) score += 35;
        
        return score;
    }

    private int CalculatePieChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算饼图的适用性评分
        if (features.DimensionCount == 2) score += 40;
        if (features.RowCount <= 10) score += 40;
        if (!features.HasTimeSeriesData) score += 20;
        
        return score;
    }

    private int CalculateScatterChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算散点图的适用性评分
        if (features.DimensionCount >= 2) score += 40;
        if (!features.HasTimeSeriesData) score += 30;
        if (features.RowCount >= 10) score += 30;
        
        return score;
    }

    private int CalculateRadarChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算雷达图的适用性评分
        if (features.DimensionCount >= 3 && features.DimensionCount <= 10) score += 40;
        if (features.RowCount <= 5) score += 40;
        if (!features.HasTimeSeriesData) score += 20;
        
        return score;
    }

    private int CalculateTreeChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算树图的适用性评分
        if (features.HasHierarchicalData) score += 50;
        if (features.DimensionCount >= 3) score += 25;
        if (!features.HasTimeSeriesData) score += 25;
        
        return score;
    }

    private int CalculateTreemapChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算矩形树图的适用性评分
        if (features.HasHierarchicalData) score += 40;
        if (features.RowCount >= 10) score += 30;
        if (!features.HasTimeSeriesData) score += 30;
        
        return score;
    }

    private int CalculateSankeyChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算桑基图的适用性评分
        if (features.HasHierarchicalData) score += 40;
        if (features.DimensionCount >= 3) score += 30;
        if (features.RowCount >= 5) score += 30;
        
        return score;
    }

    private int CalculateHeatmapChartScore(DataFeatures features)
    {
        var score = 0;
        
        // 基于数据特征计算热力图的适用性评分
        if (features.DimensionCount >= 3) score += 40;
        if (features.RowCount >= 10) score += 30;
        if (!features.HasTimeSeriesData) score += 30;
        
        return score;
    }

    #endregion

    #region Example Config Generation Methods

    private object GenerateLineChartExampleConfig(object data)
    {
        // 生成折线图示例配置
        return new Dictionary<string, object>
        {
            ["type"] = "line",
            ["options"] = new Dictionary<string, object>
            {
                ["xAxis"] = new Dictionary<string, object>
                {
                    ["type"] = "category"
                },
                ["yAxis"] = new Dictionary<string, object>
                {
                    ["type"] = "value"
                }
            }
        };
    }

    private object GenerateBarChartExampleConfig(object data)
    {
        // 生成柱状图示例配置
        return new Dictionary<string, object>
        {
            ["type"] = "bar",
            ["options"] = new Dictionary<string, object>
            {
                ["xAxis"] = new Dictionary<string, object>
                {
                    ["type"] = "category"
                },
                ["yAxis"] = new Dictionary<string, object>
                {
                    ["type"] = "value"
                }
            }
        };
    }

    private object GeneratePieChartExampleConfig(object data)
    {
        // 生成饼图示例配置
        return new Dictionary<string, object>
        {
            ["type"] = "pie",
            ["options"] = new Dictionary<string, object>
            {
                ["radius"] = "50%"
            }
        };
    }

    #endregion

    #endregion
}