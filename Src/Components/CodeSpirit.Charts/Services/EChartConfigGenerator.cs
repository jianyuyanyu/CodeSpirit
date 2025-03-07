using System.Text.Json;
using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Charts.Services
{
    /// <summary>
    /// ECharts配置生成器实现
    /// </summary>
    public class EChartConfigGenerator : IEChartConfigGenerator, ISingletonDependency
    {
        private readonly IDataAnalyzer _dataAnalyzer;
        private readonly ILogger<EChartConfigGenerator> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EChartConfigGenerator(IDataAnalyzer dataAnalyzer, ILogger<EChartConfigGenerator> logger)
        {
            _dataAnalyzer = dataAnalyzer;
            _logger = logger;
        }
        
        /// <summary>
        /// 将ChartConfig转换为ECharts配置对象
        /// </summary>
        public object GenerateEChartConfig(ChartConfig config)
        {
            try
            {
                var echartConfig = new Dictionary<string, object>();
                
                // 设置标题
                if (!string.IsNullOrEmpty(config.Title))
                {
                    echartConfig["title"] = new Dictionary<string, object>
                    {
                        ["text"] = config.Title,
                        ["subtext"] = config.Subtitle ?? string.Empty,
                        ["left"] = "center"
                    };
                }
                
                // 设置图例
                if (config.Legend != null)
                {
                    echartConfig["legend"] = new Dictionary<string, object>
                    {
                        ["data"] = config.Legend.Data ?? new List<string>(),
                        ["orient"] = config.Legend.Orient ?? "horizontal",
                        ["left"] = config.Legend.Position ?? "center"
                    };
                }
                
                // 设置工具箱
                if (config.Toolbox != null)
                {
                    echartConfig["toolbox"] = new Dictionary<string, object>
                    {
                        ["show"] = true,
                        ["feature"] = new Dictionary<string, object>
                        {
                            ["dataZoom"] = new Dictionary<string, bool> { ["show"] = true },
                            ["dataView"] = new Dictionary<string, bool> { ["show"] = true },
                            ["magicType"] = new Dictionary<string, object>
                            {
                                ["show"] = true,
                                ["type"] = new[] { "line", "bar", "stack" }
                            },
                            ["restore"] = new Dictionary<string, bool> { ["show"] = true },
                            ["saveAsImage"] = new Dictionary<string, bool> { ["show"] = true }
                        }
                    };
                }
                
                // 设置提示框
                if (config.Interaction?.Tooltip != null)
                {
                    echartConfig["tooltip"] = config.Interaction.Tooltip;
                }
                else
                {
                    echartConfig["tooltip"] = new Dictionary<string, object>
                    {
                        ["trigger"] = GetTooltipTrigger(config.Type),
                        ["formatter"] = GetTooltipFormatter(config.Type)
                    };
                }
                
                // 设置X轴
                if (config.XAxis != null && NeedsAxis(config.Type))
                {
                    echartConfig["xAxis"] = new Dictionary<string, object>
                    {
                        ["type"] = config.XAxis.Type ?? "category",
                        ["name"] = config.XAxis.Name ?? string.Empty,
                        ["data"] = new object[0] // 数据将在生成完整配置时填充
                    };
                    
                    if (config.XAxis.AxisLabel != null)
                    {
                        ((Dictionary<string, object>)echartConfig["xAxis"])["axisLabel"] = config.XAxis.AxisLabel;
                    }
                    
                    if (config.XAxis.ExtraOptions != null)
                    {
                        foreach (var option in config.XAxis.ExtraOptions)
                        {
                            ((Dictionary<string, object>)echartConfig["xAxis"])[option.Key] = option.Value;
                        }
                    }
                }
                
                // 设置Y轴
                if (config.YAxis != null && NeedsAxis(config.Type))
                {
                    echartConfig["yAxis"] = new Dictionary<string, object>
                    {
                        ["type"] = config.YAxis.Type ?? "value",
                        ["name"] = config.YAxis.Name ?? string.Empty
                    };
                    
                    if (config.YAxis.AxisLabel != null)
                    {
                        ((Dictionary<string, object>)echartConfig["yAxis"])["axisLabel"] = config.YAxis.AxisLabel;
                    }
                    
                    if (config.YAxis.ExtraOptions != null)
                    {
                        foreach (var option in config.YAxis.ExtraOptions)
                        {
                            ((Dictionary<string, object>)echartConfig["yAxis"])[option.Key] = option.Value;
                        }
                    }
                }
                
                // 设置雷达图配置
                if (config.Type == ChartType.Radar)
                {
                    echartConfig["radar"] = new Dictionary<string, object>
                    {
                        ["indicator"] = new object[0] // 雷达图指标将在生成完整配置时填充
                    };
                }
                
                // 设置系列
                var series = new List<Dictionary<string, object>>();
                foreach (var seriesConfig in config.Series)
                {
                    var seriesItem = new Dictionary<string, object>
                    {
                        ["name"] = seriesConfig.Name ?? string.Empty,
                        ["type"] = ConvertToEChartsType(config.Type.ToString(), seriesConfig.Type)
                    };
                    
                    if (seriesConfig.Label != null)
                    {
                        seriesItem["label"] = seriesConfig.Label;
                    }
                    
                    if (seriesConfig.ItemStyle != null)
                    {
                        seriesItem["itemStyle"] = seriesConfig.ItemStyle;
                    }
                    
                    if (seriesConfig.ExtraOptions != null)
                    {
                        foreach (var option in seriesConfig.ExtraOptions)
                        {
                            seriesItem[option.Key] = option.Value;
                        }
                    }
                    
                    series.Add(seriesItem);
                }
                echartConfig["series"] = series;
                
                // 设置数据缩放
                if (config.Interaction?.DataZoom != null)
                {
                    echartConfig["dataZoom"] = config.Interaction.DataZoom;
                }
                
                // 设置视觉映射
                if (config.ExtraStyles != null && config.ExtraStyles.ContainsKey("visualMap"))
                {
                    echartConfig["visualMap"] = config.ExtraStyles["visualMap"];
                }
                
                // 设置其他额外样式
                if (config.ExtraStyles != null)
                {
                    foreach (var style in config.ExtraStyles)
                    {
                        if (style.Key != "visualMap" && !echartConfig.ContainsKey(style.Key))
                        {
                            echartConfig[style.Key] = style.Value;
                        }
                    }
                }
                
                return echartConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成ECharts配置时出错");
                return new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["text"] = "配置生成错误" }
                };
            }
        }
        
        /// <summary>
        /// 将ChartConfig转换为ECharts配置JSON字符串
        /// </summary>
        public string GenerateEChartConfigJson(ChartConfig config)
        {
            var echartConfig = GenerateEChartConfig(config);
            return JsonConvert.SerializeObject(echartConfig);
        }
        
        /// <summary>
        /// 将图表配置和数据一起转换为完整的ECharts配置对象
        /// </summary>
        public object GenerateCompleteEChartConfig(ChartConfig config, object data)
        {
            try
            {
                var echartConfig = GenerateEChartConfig(config) as Dictionary<string, object>;
                if (echartConfig == null)
                {
                    return new Dictionary<string, object>();
                }
                
                // 分析数据结构
                var structure = _dataAnalyzer.AnalyzeDataStructure(data);
                
                // 获取数据数组
                var dataArray = ExtractDataArray(data);
                if (dataArray.Count == 0)
                {
                    return echartConfig;
                }
                
                // 处理不同类型图表的数据
                switch (config.Type)
                {
                    case ChartType.Bar:
                    case ChartType.Line:
                        ProcessAxisChartData(echartConfig, dataArray, structure, config);
                        break;
                    case ChartType.Pie:
                        ProcessPieChartData(echartConfig, dataArray, structure, config);
                        break;
                    case ChartType.Scatter:
                        ProcessScatterChartData(echartConfig, dataArray, structure, config);
                        break;
                    case ChartType.Radar:
                        ProcessRadarChartData(echartConfig, dataArray, structure, config);
                        break;
                    case ChartType.Heatmap:
                        ProcessHeatmapChartData(echartConfig, dataArray, structure, config);
                        break;
                    default:
                        ProcessDefaultChartData(echartConfig, dataArray, structure, config);
                        break;
                }
                
                return echartConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成完整ECharts配置时出错");
                return new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, string> { ["text"] = "配置生成错误" }
                };
            }
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 获取工具提示触发类型
        /// </summary>
        private string GetTooltipTrigger(ChartType chartType)
        {
            switch (chartType)
            {
                case ChartType.Pie:
                    return "item";
                case ChartType.Scatter:
                    return "item";
                default:
                    return "axis";
            }
        }
        
        /// <summary>
        /// 获取工具提示格式化器
        /// </summary>
        private string GetTooltipFormatter(ChartType chartType)
        {
            switch (chartType)
            {
                case ChartType.Pie:
                    return "{a} <br/>{b}: {c} ({d}%)";
                case ChartType.Line:
                case ChartType.Bar:
                    return "{a} <br/>{b}: {c}";
                case ChartType.Scatter:
                    return "{a} <br/>{b}: {c}";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 检查图表类型是否需要坐标轴
        /// </summary>
        private bool NeedsAxis(ChartType chartType)
        {
            return chartType != ChartType.Pie && chartType != ChartType.Radar;
        }
        
        /// <summary>
        /// 转换为ECharts图表类型
        /// </summary>
        private string ConvertToEChartsType(string chartType, string? seriesType)
        {
            if (!string.IsNullOrEmpty(seriesType))
            {
                return seriesType.ToLower();
            }
            
            return chartType.ToLower();
        }
        
        /// <summary>
        /// 提取数据数组
        /// </summary>
        private List<JObject> ExtractDataArray(object data)
        {
            var result = new List<JObject>();
            
            try
            {
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonObject = JToken.Parse(jsonData);
                
                if (jsonObject is JArray jsonArray)
                {
                    foreach (var item in jsonArray)
                    {
                        if (item is JObject jObject)
                        {
                            result.Add(jObject);
                        }
                    }
                }
                else if (jsonObject is JObject jObject)
                {
                    // 寻找可能的数据数组属性
                    var dataArrayProperty = FindDataArrayProperty(jObject);
                    if (dataArrayProperty != null && dataArrayProperty is JArray arrayProperty)
                    {
                        foreach (var item in arrayProperty)
                        {
                            if (item is JObject itemObject)
                            {
                                result.Add(itemObject);
                            }
                        }
                    }
                    else
                    {
                        // 如果没有找到数组，就把当前对象作为单条数据
                        result.Add(jObject);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取数据数组时出错");
            }
            
            return result;
        }
        
        /// <summary>
        /// 查找数据数组属性
        /// </summary>
        private JToken? FindDataArrayProperty(JObject jObject)
        {
            // 寻找最大的数组属性
            JToken? largestArray = null;
            int maxCount = 0;
            
            foreach (var property in jObject.Properties())
            {
                if (property.Value is JArray array && array.Count > maxCount)
                {
                    maxCount = array.Count;
                    largestArray = property.Value;
                }
            }
            
            return largestArray;
        }
        
        /// <summary>
        /// 处理坐标轴图表数据（柱状图、折线图）
        /// </summary>
        private void ProcessAxisChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            if (structure.DimensionFields.Count == 0 || structure.MetricFields.Count == 0)
            {
                return;
            }
            
            // 确定X轴字段
            var xField = structure.DimensionFields[0];
            
            // 提取X轴数据
            var xAxisData = dataArray.Select(item => item[xField]?.ToString()).Where(x => x != null).ToList();
            if (echartConfig.ContainsKey("xAxis"))
            {
                ((Dictionary<string, object>)echartConfig["xAxis"])["data"] = xAxisData;
            }
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series)
            {
                for (int i = 0; i < Math.Min(series.Count, structure.MetricFields.Count); i++)
                {
                    var seriesItem = series[i];
                    var metricField = structure.MetricFields[i];
                    
                    // 提取系列数据
                    var seriesData = dataArray.Select(item => 
                    {
                        if (item.TryGetValue(metricField, out var value))
                        {
                            return value.Type == JTokenType.Null ? null : value.ToObject<object>();
                        }
                        return null;
                    }).ToList();
                    
                    seriesItem["data"] = seriesData;
                    seriesItem["name"] = metricField;
                }
            }
        }
        
        /// <summary>
        /// 处理饼图数据
        /// </summary>
        private void ProcessPieChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            if (structure.DimensionFields.Count == 0 || structure.MetricFields.Count == 0)
            {
                return;
            }
            
            var categoryField = structure.DimensionFields[0];
            var valueField = structure.MetricFields[0];
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series && series.Count > 0)
            {
                var seriesItem = series[0];
                
                // 构建饼图数据
                var pieData = dataArray.Select(item => 
                {
                    var name = item[categoryField]?.ToString() ?? "未知";
                    var value = item[valueField]?.ToObject<double>() ?? 0;
                    
                    return new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["value"] = value
                    };
                }).ToList<object>();
                
                seriesItem["data"] = pieData;
                seriesItem["name"] = valueField;
            }
            
            // 设置图例数据
            if (echartConfig.ContainsKey("legend") && echartConfig["legend"] is Dictionary<string, object> legend)
            {
                var categories = dataArray.Select(item => item[categoryField]?.ToString()).Where(x => x != null).ToList();
                legend["data"] = categories;
            }
        }
        
        /// <summary>
        /// 处理散点图数据
        /// </summary>
        private void ProcessScatterChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            if (structure.MetricFields.Count < 2)
            {
                return;
            }
            
            var xField = structure.MetricFields[0];
            var yField = structure.MetricFields[1];
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series && series.Count > 0)
            {
                var seriesItem = series[0];
                
                // 构建散点图数据
                var scatterData = dataArray.Select(item => 
                {
                    var x = item[xField]?.ToObject<double>() ?? 0;
                    var y = item[yField]?.ToObject<double>() ?? 0;
                    
                    return new[] { x, y };
                }).ToList<object>();
                
                seriesItem["data"] = scatterData;
                seriesItem["name"] = $"{xField} vs {yField}";
            }
        }
        
        /// <summary>
        /// 处理雷达图数据
        /// </summary>
        private void ProcessRadarChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            if (structure.MetricFields.Count == 0)
            {
                return;
            }
            
            // 设置雷达图指示器
            if (echartConfig.ContainsKey("radar") && echartConfig["radar"] is Dictionary<string, object> radar)
            {
                var indicators = structure.MetricFields.Select(field => 
                {
                    // 计算最大值
                    double max = 0;
                    foreach (var item in dataArray)
                    {
                        if (item.TryGetValue(field, out var value) && value.Type != JTokenType.Null)
                        {
                            var val = value.ToObject<double>();
                            if (val > max) max = val;
                        }
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["name"] = field,
                        ["max"] = max > 0 ? Math.Ceiling(max * 1.2) : 100
                    };
                }).ToList<object>();
                
                radar["indicator"] = indicators;
            }
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series && series.Count > 0)
            {
                var seriesItem = series[0];
                var seriesData = new List<object>();
                
                // 为每个数据项构建雷达图数据
                if (structure.DimensionFields.Count > 0)
                {
                    var categoryField = structure.DimensionFields[0];
                    
                    foreach (var item in dataArray)
                    {
                        var name = item[categoryField]?.ToString() ?? "未知";
                        var values = structure.MetricFields.Select(field => 
                        {
                            if (item.TryGetValue(field, out var value) && value.Type != JTokenType.Null)
                            {
                                return value.ToObject<double>();
                            }
                            return 0d;
                        }).ToList();
                        
                        seriesData.Add(new Dictionary<string, object>
                        {
                            ["name"] = name,
                            ["value"] = values
                        });
                    }
                }
                else
                {
                    // 如果没有分类字段，将每个记录作为单独的雷达图
                    for (int i = 0; i < Math.Min(dataArray.Count, 5); i++)
                    {
                        var item = dataArray[i];
                        var values = structure.MetricFields.Select(field => 
                        {
                            if (item.TryGetValue(field, out var value) && value.Type != JTokenType.Null)
                            {
                                return value.ToObject<double>();
                            }
                            return 0d;
                        }).ToList();
                        
                        seriesData.Add(new Dictionary<string, object>
                        {
                            ["name"] = $"数据{i+1}",
                            ["value"] = values
                        });
                    }
                }
                
                seriesItem["data"] = seriesData;
            }
        }
        
        /// <summary>
        /// 处理热力图数据
        /// </summary>
        private void ProcessHeatmapChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            if (structure.DimensionFields.Count < 2 || structure.MetricFields.Count == 0)
            {
                return;
            }
            
            var xField = structure.DimensionFields[0];
            var yField = structure.DimensionFields[1];
            var valueField = structure.MetricFields[0];
            
            // 提取X轴和Y轴的唯一值
            var xValues = dataArray.Select(item => item[xField]?.ToString()).Where(x => x != null).Distinct().ToList();
            var yValues = dataArray.Select(item => item[yField]?.ToString()).Where(y => y != null).Distinct().ToList();
            
            // 设置坐标轴数据
            if (echartConfig.ContainsKey("xAxis"))
            {
                ((Dictionary<string, object>)echartConfig["xAxis"])["data"] = xValues;
            }
            
            if (echartConfig.ContainsKey("yAxis"))
            {
                ((Dictionary<string, object>)echartConfig["yAxis"])["data"] = yValues;
            }
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series && series.Count > 0)
            {
                var seriesItem = series[0];
                
                // 构建热力图数据
                var heatmapData = new List<object>();
                
                // 构建值映射
                var valueMap = new Dictionary<string, Dictionary<string, double>>();
                foreach (var item in dataArray)
                {
                    var xVal = item[xField]?.ToString();
                    var yVal = item[yField]?.ToString();
                    var val = item[valueField]?.ToObject<double>() ?? 0;
                    
                    if (xVal != null && yVal != null)
                    {
                        if (!valueMap.ContainsKey(xVal))
                        {
                            valueMap[xVal] = new Dictionary<string, double>();
                        }
                        valueMap[xVal][yVal] = val;
                    }
                }
                
                // 构建热力图数据
                for (int x = 0; x < xValues.Count; x++)
                {
                    for (int y = 0; y < yValues.Count; y++)
                    {
                        double value = 0;
                        if (valueMap.ContainsKey(xValues[x]) && valueMap[xValues[x]].ContainsKey(yValues[y]))
                        {
                            value = valueMap[xValues[x]][yValues[y]];
                        }
                        
                        heatmapData.Add(new object[] { x, y, value });
                    }
                }
                
                seriesItem["data"] = heatmapData;
                seriesItem["name"] = valueField;
            }
            
            // 设置视觉映射
            if (!echartConfig.ContainsKey("visualMap"))
            {
                var min = 0d;
                var max = 0d;
                
                foreach (var item in dataArray)
                {
                    if (item.TryGetValue(valueField, out var value) && value.Type != JTokenType.Null)
                    {
                        var val = value.ToObject<double>();
                        if (val > max) max = val;
                        if (val < min) min = val;
                    }
                }
                
                echartConfig["visualMap"] = new Dictionary<string, object>
                {
                    ["min"] = min,
                    ["max"] = max,
                    ["calculable"] = true,
                    ["orient"] = "horizontal",
                    ["left"] = "center",
                    ["bottom"] = "5%"
                };
            }
        }
        
        /// <summary>
        /// 处理默认图表数据
        /// </summary>
        private void ProcessDefaultChartData(Dictionary<string, object> echartConfig, List<JObject> dataArray, DataStructureInfo structure, ChartConfig config)
        {
            // 默认使用柱状图处理方式
            ProcessAxisChartData(echartConfig, dataArray, structure, config);
        }
        
        #endregion
    }
} 