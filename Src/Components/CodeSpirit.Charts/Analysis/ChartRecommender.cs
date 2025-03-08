using CodeSpirit.Charts.Models;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Analysis
{
    /// <summary>
    /// 图表推荐器实现
    /// </summary>
    public class ChartRecommender : IChartRecommender, ISingletonDependency
    {
        private readonly IDataAnalyzer _dataAnalyzer;
        private readonly ILogger<ChartRecommender> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartRecommender(IDataAnalyzer dataAnalyzer, ILogger<ChartRecommender> logger)
        {
            _dataAnalyzer = dataAnalyzer;
            _logger = logger;
        }
        
        /// <summary>
        /// 推荐最适合的图表类型
        /// </summary>
        public ChartType RecommendChartType(object data)
        {
            try
            {
                var structure = _dataAnalyzer.AnalyzeDataStructure(data);
                var features = _dataAnalyzer.ExtractDataFeatures(data);
                
                return DetermineOptimalChartType(structure, features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推荐图表类型时出错");
                return ChartType.Bar; // 默认返回柱状图
            }
        }
        
        /// <summary>
        /// 生成完整的图表配置
        /// </summary>
        public ChartConfig GenerateChartConfig(object data, ChartType? preferredType = null)
        {
            try
            {
                ChartType chartType = preferredType ?? RecommendChartType(data);
                
                return BuildOptimalChartConfig(data, chartType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成图表配置时出错");
                return new ChartConfig { Type = ChartType.Bar };
            }
        }
        
        /// <summary>
        /// 推荐多个适合的图表类型及评分
        /// </summary>
        public Dictionary<ChartType, double> RecommendChartTypes(object data, int maxCount = 3)
        {
            try
            {
                var structure = _dataAnalyzer.AnalyzeDataStructure(data);
                var features = _dataAnalyzer.ExtractDataFeatures(data);
                
                return ScoreChartTypes(structure, features, maxCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推荐多个图表类型时出错");
                return new Dictionary<ChartType, double> { { ChartType.Bar, 1.0 } };
            }
        }
        
        /// <summary>
        /// 根据数据分析结果优化图表配置
        /// </summary>
        public ChartConfig OptimizeChartConfig(ChartConfig config, object data)
        {
            try
            {
                var structure = _dataAnalyzer.AnalyzeDataStructure(data);
                var features = _dataAnalyzer.ExtractDataFeatures(data);
                
                // 优化配置
                OptimizeAxisConfig(config, structure, features);
                OptimizeSeriesConfig(config, structure, features);
                OptimizeInteractionConfig(config, features);
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化图表配置时出错");
                return config;
            }
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 确定最优图表类型
        /// </summary>
        private ChartType DetermineOptimalChartType(DataStructureInfo structure, DataFeatures features)
        {
            // 如果只有一个指标字段且数据具有分类特性，则可能适合饼图
            if (structure.MetricFields.Count == 1 && features.IsCategorical && !features.IsTimeSeries)
            {
                return ChartType.Pie;
            }
            
            // 如果是时间序列数据，通常使用折线图
            if (features.IsTimeSeries)
            {
                return ChartType.Line;
            }
            
            // 如果有多个维度和指标，且维度是分类数据，通常使用柱状图
            if (structure.DimensionFields.Count >= 1 && structure.MetricFields.Count >= 1 && features.IsCategorical)
            {
                return ChartType.Bar;
            }
            
            // 如果有两个以上的指标字段，可以使用雷达图
            if (structure.MetricFields.Count >= 3)
            {
                return ChartType.Radar;
            }
            
            // 如果没有明显的数据模式，默认使用柱状图
            return ChartType.Bar;
        }
        
        /// <summary>
        /// 评分不同图表类型
        /// </summary>
        private Dictionary<ChartType, double> ScoreChartTypes(DataStructureInfo structure, DataFeatures features, int maxCount)
        {
            var scores = new Dictionary<ChartType, double>();
            
            // 饼图评分
            scores[ChartType.Pie] = ScorePieChart(structure, features);
            
            // 折线图评分
            scores[ChartType.Line] = ScoreLineChart(structure, features);
            
            // 柱状图评分
            scores[ChartType.Bar] = ScoreBarChart(structure, features);
            
            // 散点图评分
            scores[ChartType.Scatter] = ScoreScatterChart(structure, features);
            
            // 雷达图评分
            scores[ChartType.Radar] = ScoreRadarChart(structure, features);
            
            // 热力图评分
            scores[ChartType.Heatmap] = ScoreHeatmapChart(structure, features);
            
            // 按得分排序并返回前maxCount个
            return scores.OrderByDescending(kv => kv.Value)
                         .Take(maxCount)
                         .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        
        /// <summary>
        /// 饼图评分
        /// </summary>
        private double ScorePieChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.5; // 基础分
            
            // 如果只有一个指标字段，加分
            if (structure.MetricFields.Count == 1)
                score += 0.3;
            
            // 如果数据量适中（不太多也不太少），加分
            if (structure.RowCount >= 3 && structure.RowCount <= 10)
                score += 0.2;
            
            // 如果是分类数据，加分
            if (features.IsCategorical)
                score += 0.2;
            
            // 如果是时间序列，减分
            if (features.IsTimeSeries)
                score -= 0.3;
            
            // 如果数据点太多，减分
            if (structure.RowCount > 15)
                score -= 0.1 * (Math.Min(structure.RowCount, 30) - 15) / 15.0;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 折线图评分
        /// </summary>
        private double ScoreLineChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.5; // 基础分
            
            // 如果是时间序列，加分
            if (features.IsTimeSeries)
                score += 0.3;
            
            // 如果有趋势，加分
            if (features.HasTrend)
                score += 0.2;
            
            // 如果有合适数量的数据点，加分
            if (structure.RowCount >= 5)
                score += 0.1;
            
            // 如果有多个指标但只有一个维度，加分
            if (structure.MetricFields.Count > 1 && structure.DimensionFields.Count == 1)
                score += 0.1;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 柱状图评分
        /// </summary>
        private double ScoreBarChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.6; // 基础分（柱状图通常更通用）
            
            // 如果是分类数据，加分
            if (features.IsCategorical)
                score += 0.2;
            
            // 如果数据点数量适中，加分
            if (structure.RowCount <= 20)
                score += 0.1;
            
            // 如果有多个指标，加分
            if (structure.MetricFields.Count > 1)
                score += 0.1;
            
            // 如果是时间序列且有明显趋势，减分（倾向于使用折线图）
            if (features.IsTimeSeries && features.HasTrend)
                score -= 0.1;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 散点图评分
        /// </summary>
        private double ScoreScatterChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.3; // 基础分
            
            // 如果有两个或更多数值类型字段，加分
            if (structure.MetricFields.Count >= 2)
                score += 0.3;
            
            // 如果数据点较多，加分
            if (structure.RowCount > 20)
                score += 0.2;
            
            // 如果数据有明显关联性，加分
            var correlations = _dataAnalyzer.DetectCorrelations(structure);
            if (correlations.Any(c => Math.Abs(c.Coefficient) > 0.5))
                score += 0.2;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 雷达图评分
        /// </summary>
        private double ScoreRadarChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.3; // 基础分
            
            // 如果有3-7个指标字段，加分（雷达图最适合多维度比较）
            if (structure.MetricFields.Count >= 3 && structure.MetricFields.Count <= 7)
                score += 0.4;
            
            // 如果数据点不太多，加分
            if (structure.RowCount <= 7)
                score += 0.2;
            
            // 如果是分类数据，加分
            if (features.IsCategorical)
                score += 0.1;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 热力图评分
        /// </summary>
        private double ScoreHeatmapChart(DataStructureInfo structure, DataFeatures features)
        {
            double score = 0.2; // 基础分
            
            // 如果有两个以上的维度字段，加分
            if (structure.DimensionFields.Count >= 2)
                score += 0.3;
            
            // 如果有一个明显的指标字段，加分
            if (structure.MetricFields.Count == 1)
                score += 0.2;
            
            // 如果数据点较多，加分
            if (structure.RowCount > 20)
                score += 0.2;
            
            return Math.Max(0, Math.Min(1, score));
        }
        
        /// <summary>
        /// 构建最优图表配置
        /// </summary>
        private ChartConfig BuildOptimalChartConfig(object data, ChartType chartType)
        {
            var structure = _dataAnalyzer.AnalyzeDataStructure(data);
            var features = _dataAnalyzer.ExtractDataFeatures(data);
            
            var config = new ChartConfig
            {
                Type = chartType,
                Title = "数据分析图表"
            };
            
            // 设置数据源
            config.DataSource = new ChartDataSource
            {
                Type = DataSourceType.Current,
                StaticData = data
            };
            
            // 根据数据特性配置各项参数
            ConfigureBasedOnChartType(config, structure, features);
            
            // 优化配置
            OptimizeChartConfig(config, data);
            
            return config;
        }
        
        /// <summary>
        /// 根据图表类型配置参数
        /// </summary>
        private void ConfigureBasedOnChartType(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            switch (config.Type)
            {
                case ChartType.Line:
                    ConfigureLineChart(config, structure, features);
                    break;
                case ChartType.Bar:
                    ConfigureBarChart(config, structure, features);
                    break;
                case ChartType.Pie:
                    ConfigurePieChart(config, structure, features);
                    break;
                case ChartType.Scatter:
                    ConfigureScatterChart(config, structure, features);
                    break;
                case ChartType.Radar:
                    ConfigureRadarChart(config, structure, features);
                    break;
                case ChartType.Heatmap:
                    ConfigureHeatmapChart(config, structure, features);
                    break;
                default:
                    // 默认配置
                    ConfigureDefaultChart(config, structure, features);
                    break;
            }
        }
        
        /// <summary>
        /// 配置折线图
        /// </summary>
        private void ConfigureLineChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 选择第一个维度作为X轴
            var xField = structure.DimensionFields.FirstOrDefault();
            
            if (string.IsNullOrEmpty(xField) && structure.DimensionFields.Count > 0)
                xField = structure.DimensionFields[0];
            
            // 设置X轴配置
            config.XAxis = new AxisConfig
            {
                Type = features.IsTimeSeries ? "time" : "category",
                Name = xField
            };
            
            // 设置Y轴配置
            config.YAxis = new AxisConfig
            {
                Type = "value",
                Name = "值"
            };
            
            // 为每个指标字段创建一个系列
            foreach (var metricField in structure.MetricFields)
            {
                config.Series.Add(new SeriesConfig
                {
                    Name = metricField,
                    Type = "line"
                });
            }
            
            // 设置图例
            if (structure.MetricFields.Count > 1)
            {
                config.Legend = new LegendConfig
                {
                    Data = structure.MetricFields
                };
            }
        }
        
        /// <summary>
        /// 配置柱状图
        /// </summary>
        private void ConfigureBarChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 选择第一个维度作为X轴
            var xField = structure.DimensionFields.FirstOrDefault();
            
            if (string.IsNullOrEmpty(xField) && structure.DimensionFields.Count > 0)
                xField = structure.DimensionFields[0];
            
            // 设置X轴配置
            config.XAxis = new AxisConfig
            {
                Type = "category",
                Name = xField
            };
            
            // 设置Y轴配置
            config.YAxis = new AxisConfig
            {
                Type = "value",
                Name = "值"
            };
            
            // 为每个指标字段创建一个系列
            foreach (var metricField in structure.MetricFields)
            {
                config.Series.Add(new SeriesConfig
                {
                    Name = metricField,
                    Type = "bar"
                });
            }
            
            // 设置图例
            if (structure.MetricFields.Count > 1)
            {
                config.Legend = new LegendConfig
                {
                    Data = structure.MetricFields
                };
            }
        }
        
        /// <summary>
        /// 配置饼图
        /// </summary>
        private void ConfigurePieChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 选择第一个维度作为分类字段
            var categoryField = structure.DimensionFields.FirstOrDefault();
            
            // 选择第一个指标作为值字段
            var valueField = structure.MetricFields.FirstOrDefault();
            
            // 创建饼图系列
            config.Series.Add(new SeriesConfig
            {
                Type = "pie",
                Name = valueField,
                // 饼图特殊配置
                Label = new Dictionary<string, object>
                {
                    ["show"] = true,
                    ["formatter"] = "{b}: {c} ({d}%)"
                },
                ItemStyle = new Dictionary<string, object>
                {
                    ["borderRadius"] = 8,
                    ["borderWidth"] = 2
                }
            });
            
            // 饼图不需要坐标轴
            config.XAxis = null;
            config.YAxis = null;
            
            // 设置图例
            config.Legend = new LegendConfig
            {
                Orient = "vertical",
                Position = "right"
            };
        }
        
        /// <summary>
        /// 配置散点图
        /// </summary>
        private void ConfigureScatterChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 如果有两个以上的指标字段，使用前两个作为X和Y轴
            if (structure.MetricFields.Count >= 2)
            {
                var xField = structure.MetricFields[0];
                var yField = structure.MetricFields[1];
                
                // 设置X轴配置
                config.XAxis = new AxisConfig
                {
                    Type = "value",
                    Name = xField
                };
                
                // 设置Y轴配置
                config.YAxis = new AxisConfig
                {
                    Type = "value",
                    Name = yField
                };
                
                // 创建散点图系列
                config.Series.Add(new SeriesConfig
                {
                    Type = "scatter",
                    Name = $"{xField} vs {yField}"
                });
            }
        }
        
        /// <summary>
        /// 配置雷达图
        /// </summary>
        private void ConfigureRadarChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 雷达图需要特殊处理，这里简化实现
            config.Series.Add(new SeriesConfig
            {
                Type = "radar",
                Name = "数据分析"
            });
            
            // 雷达图不使用标准坐标轴
            config.XAxis = null;
            config.YAxis = null;
        }
        
        /// <summary>
        /// 配置热力图
        /// </summary>
        private void ConfigureHeatmapChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            if (structure.DimensionFields.Count >= 2)
            {
                var xField = structure.DimensionFields[0];
                var yField = structure.DimensionFields[1];
                
                // 设置X轴配置
                config.XAxis = new AxisConfig
                {
                    Type = "category",
                    Name = xField
                };
                
                // 设置Y轴配置
                config.YAxis = new AxisConfig
                {
                    Type = "category",
                    Name = yField
                };
                
                // 创建热力图系列
                config.Series.Add(new SeriesConfig
                {
                    Type = "heatmap",
                    Name = structure.MetricFields.FirstOrDefault() ?? "值"
                });
                
                // 配置视觉映射
                config.ExtraStyles = new Dictionary<string, object>
                {
                    ["visualMap"] = new
                    {
                        show = true,
                        calculable = true
                    }
                };
            }
        }
        
        /// <summary>
        /// 配置默认图表
        /// </summary>
        private void ConfigureDefaultChart(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 默认使用柱状图配置
            ConfigureBarChart(config, structure, features);
        }
        
        /// <summary>
        /// 优化坐标轴配置
        /// </summary>
        private void OptimizeAxisConfig(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            // 跳过不使用标准坐标轴的图表类型
            if (config.Type == ChartType.Pie || config.Type == ChartType.Radar)
                return;
            
            // 如果是时间序列数据，优化X轴的时间格式
            if (features.IsTimeSeries && config.XAxis != null)
            {
                config.XAxis.Type = "time";
            }
            
            // 如果有异常值，可以调整Y轴范围
            if (features.HasOutliers && config.YAxis != null && structure.MetricFields.Count > 0)
            {
                var metricField = structure.MetricFields[0];
                if (features.MetricStatistics.TryGetValue(metricField, out var stats))
                {
                    // 使用均值±2.5倍标准差作为Y轴范围
                    double min = Math.Max(stats.Min, stats.Average - 2.5 * stats.StdDev);
                    double max = Math.Min(stats.Max, stats.Average + 2.5 * stats.StdDev);
                    
                    // 只有在异常值超出很多时才调整
                    if (min > stats.Min * 1.5 || max < stats.Max * 0.7)
                    {
                        config.YAxis.ExtraOptions = new Dictionary<string, object>
                        {
                            ["min"] = min,
                            ["max"] = max
                        };
                    }
                }
            }
        }
        
        /// <summary>
        /// 优化系列配置
        /// </summary>
        private void OptimizeSeriesConfig(ChartConfig config, DataStructureInfo structure, DataFeatures features)
        {
            foreach (var series in config.Series)
            {
                // 为不同类型的图表添加适当的样式
                switch (series.Type)
                {
                    case "line":
                        // 如果是时间序列且有明显趋势，添加平滑曲线
                        if (features.IsTimeSeries && features.HasTrend)
                        {
                            series.ExtraOptions = new Dictionary<string, object>
                            {
                                ["smooth"] = true
                            };
                        }
                        break;
                        
                    case "bar":
                        // 如果数据点较多，使用较窄的柱子
                        if (structure.RowCount > 10)
                        {
                            series.ExtraOptions = new Dictionary<string, object>
                            {
                                ["barWidth"] = "50%"
                            };
                        }
                        break;
                        
                    case "pie":
                        // 优化饼图外观
                        series.ExtraOptions = new Dictionary<string, object>
                        {
                            ["radius"] = "60%",
                            ["center"] = new[] { "50%", "50%" }
                        };
                        break;
                }
            }
        }
        
        /// <summary>
        /// 优化交互配置
        /// </summary>
        private void OptimizeInteractionConfig(ChartConfig config, DataFeatures features)
        {
            // 添加基本的提示框配置
            config.Interaction = new InteractionConfig
            {
                Tooltip = new Dictionary<string, object>
                {
                    ["trigger"] = config.Type == ChartType.Pie ? "item" : "axis",
                    ["formatter"] = GetDefaultTooltipFormatter(config.Type)
                }
            };
            
            // 为时间序列数据添加数据区域缩放
            if (features.IsTimeSeries && config.Type == ChartType.Line && features.HasTrend)
            {
                config.Interaction.DataZoom = new Dictionary<string, object>
                {
                    ["show"] = true,
                    ["type"] = "slider",
                    ["start"] = 0,
                    ["end"] = 100
                };
            }
            
            // 为图表添加工具箱
            config.Toolbox = new ToolboxConfig();
        }
        
        /// <summary>
        /// 获取默认提示框格式化器
        /// </summary>
        private string GetDefaultTooltipFormatter(ChartType chartType)
        {
            switch (chartType)
            {
                case ChartType.Pie:
                    return "{a} <br/>{b} : {c} ({d}%)";
                case ChartType.Line:
                case ChartType.Bar:
                    return "{a} <br/>{b} : {c}";
                default:
                    return "";
            }
        }
        
        #endregion
    }
} 