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
                        ["left"] = config.Legend.Position ?? "center",
                        ["top"] = "bottom"
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
                            ["dataView"] = new Dictionary<string, bool> { ["show"] = true },
                            //["magicType"] = new Dictionary<string, object>
                            //{
                            //    ["show"] = true,
                            //    ["type"] = new[] { "line", "bar", "stack" }
                            //},
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
                    var xAxisType = config.XAxis.Type ?? "category";
                    var xAxisConfig = new Dictionary<string, object>
                    {
                        ["type"] = xAxisType,
                        ["name"] = config.XAxis.Name ?? string.Empty
                    };
                    
                    // 只有在非time类型轴才设置初始data属性
                    if (xAxisType != "time")
                    {
                        xAxisConfig["data"] = new object[0]; // 数据将在生成完整配置时填充
                    }
                    
                    if (config.XAxis.AxisLabel != null)
                    {
                        xAxisConfig["axisLabel"] = config.XAxis.AxisLabel;
                    }
                    
                    if (config.XAxis.ExtraOptions != null)
                    {
                        foreach (var option in config.XAxis.ExtraOptions)
                        {
                            xAxisConfig[option.Key] = option.Value;
                        }
                    }
                    
                    echartConfig["xAxis"] = xAxisConfig;
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
                    
                    if (seriesConfig.Encode != null)
                    {
                        seriesItem["encode"] = seriesConfig.Encode;
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
                
                // 记录轴类型与数据结构的匹配情况
                if (config.XAxis != null && echartConfig.ContainsKey("xAxis"))
                {
                    var xAxisType = config.XAxis.Type ?? "category";
                    _logger.LogInformation($"X轴配置的类型为: {xAxisType}");
                    
                    if (structure.DimensionFields.Count > 0)
                    {
                        var xField = structure.DimensionFields[0];
                        var fieldType = structure.FieldTypes.ContainsKey(xField) ? structure.FieldTypes[xField].Name : "未知";
                        _logger.LogInformation($"维度字段 '{xField}' 的数据类型为: {fieldType}");
                        
                        // 检查轴类型与数据类型是否匹配
                        if ((xAxisType == "time" && fieldType != "DateTime" && fieldType != "String") ||
                            ((xAxisType == "value" || xAxisType == "log") && 
                             fieldType != "Int32" && fieldType != "Int64" && fieldType != "Double" && 
                             fieldType != "Decimal" && fieldType != "Single"))
                        {
                            _logger.LogWarning($"X轴类型 '{xAxisType}' 与数据字段类型 '{fieldType}' 可能不匹配，这可能导致图表渲染异常");
                        }
                    }
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
            
            // 获取X轴配置
            var xAxisConfig = echartConfig.ContainsKey("xAxis") ? echartConfig["xAxis"] as Dictionary<string, object> : null;
            var axisType = xAxisConfig?.ContainsKey("type") == true ? xAxisConfig["type"].ToString() : "category";
            
            // 提取X轴数据
            var xAxisData = dataArray.Select(item => item[xField]?.ToObject<object>()).ToList();
            
            // 验证X轴数据是否符合指定的轴类型
            if (xAxisConfig != null)
            {
                var (isValid, errorMessage) = ValidateAxisData(axisType, xAxisData);
                if (!isValid)
                {
                    _logger.LogWarning(errorMessage);
                }
                
                // 对于类目轴，设置数据到X轴配置中
                if (axisType == "category")
                {
                    xAxisConfig["data"] = xAxisData.Where(x => x != null).ToList();
                }
            }
            
            // 处理系列数据
            if (echartConfig.ContainsKey("series") && echartConfig["series"] is List<Dictionary<string, object>> series && series.Count > 0)
            {
                for (int i = 0; i < Math.Min(series.Count, structure.MetricFields.Count); i++)
                {
                    var seriesItem = series[i];
                    var metricField = structure.MetricFields[i];
                    
                    // 提取系列数据
                    var seriesData = new List<object>();
                    
                    // 如果是时间轴，使用[时间,值]格式构建数据
                    if (axisType == "time")
                    {
                        // 获取正确的字段名称，优先使用encode中的配置
                        string xFieldName = xField;
                        string yFieldName = metricField;
                        
                        // 检查SeriesConfig中的Encode设置
                        if (i < config.Series.Count && config.Series[i].Encode != null)
                        {
                            if (config.Series[i].Encode.ContainsKey("x"))
                            {
                                xFieldName = config.Series[i].Encode["x"];
                            }
                            
                            if (config.Series[i].Encode.ContainsKey("y"))
                            {
                                yFieldName = config.Series[i].Encode["y"];
                            }
                            
                            // 确保series中也有encode配置
                            if (!seriesItem.ContainsKey("encode"))
                            {
                                seriesItem["encode"] = config.Series[i].Encode;
                            }
                        }
                        // 如果series中有encode配置，使用encode中的字段名
                        else if (seriesItem.ContainsKey("encode") && seriesItem["encode"] is Dictionary<string, string> encode)
                        {
                            if (encode.ContainsKey("x"))
                            {
                                xFieldName = encode["x"];
                            }
                            
                            if (encode.ContainsKey("y"))
                            {
                                yFieldName = encode["y"];
                            }
                        }
                        
                        // 对于时间轴，我们使用[时间,值]的格式
                        foreach (var item in dataArray)
                        {
                            // 尝试使用配置的字段名
                            if (item.TryGetValue(xFieldName, out var xValue) && item.TryGetValue(yFieldName, out var yValue))
                            {
                                // 如果x值或y值为null，跳过这个数据点
                                if (xValue.Type == JTokenType.Null || yValue.Type == JTokenType.Null)
                                    continue;
                                
                                var xObj = xValue.ToObject<object>();
                                var yObj = yValue.ToObject<object>();
                                
                                // 确保y值是数值类型
                                if (yObj != null && (yObj is int || yObj is long || yObj is float || yObj is double || yObj is decimal))
                                {
                                    seriesData.Add(new object[] { xObj, yObj });
                                }
                                else
                                {
                                    _logger.LogWarning($"跳过非数值类型的数据点: {yObj}");
                                }
                            }
                        }
                        
                        // 如果没有有效数据点，尝试使用测试数据中的日期和值
                        if (seriesData.Count == 0 && config.DataSource?.StaticData != null)
                        {
                            try
                            {
                                // 尝试从StaticData中提取数据
                                var staticData = config.DataSource.StaticData;
                                var staticDataType = staticData.GetType();
                                
                                if (staticDataType.IsArray)
                                {
                                    var array = (Array)staticData;
                                    
                                    foreach (var item in array)
                                    {
                                        var itemType = item.GetType();
                                        var dateProperty = itemType.GetProperty(xFieldName);
                                        var valueProperty = itemType.GetProperty(yFieldName);
                                        
                                        if (dateProperty != null && valueProperty != null)
                                        {
                                            var date = dateProperty.GetValue(item);
                                            var value = valueProperty.GetValue(item);
                                            
                                            if (date != null && value != null)
                                            {
                                                seriesData.Add(new object[] { date, value });
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "从StaticData提取数据失败");
                            }
                        }
                    }
                    else
                    {
                        // 对于非时间轴图表，使用原来的方式
                        seriesData = dataArray.Select(item => 
                        {
                            if (item.TryGetValue(metricField, out var value))
                            {
                                return value.Type == JTokenType.Null ? null : value.ToObject<object>();
                            }
                            return null;
                        }).ToList();
                    }
                    
                    seriesItem["data"] = seriesData;
                    
                    // 确保保留encode配置 - 从SeriesConfig中获取
                    if (i < config.Series.Count && config.Series[i].Encode != null)
                    {
                        seriesItem["encode"] = config.Series[i].Encode;
                    }
                    
                    // 只有在未设置名称或名称为空时才使用metricField作为系列名称
                    if (!seriesItem.ContainsKey("name") || string.IsNullOrEmpty(seriesItem["name"]?.ToString()))
                    {
                        seriesItem["name"] = metricField;
                    }
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

        #region 私有方法
        
        /// <summary>
        /// 验证坐标轴数据是否符合指定的轴类型
        /// </summary>
        /// <param name="axisType">轴类型(category, value, time, log)</param>
        /// <param name="data">要验证的数据</param>
        /// <returns>验证结果，包括是否有效和错误消息</returns>
        public (bool IsValid, string? ErrorMessage) ValidateAxisData(string axisType, IEnumerable<object?> data)
        {
            if (data == null || !data.Any())
            {
                return (true, null); // 空数据被视为有效，因为可能在后期填充
            }
            
            switch (axisType.ToLower())
            {
                case "category":
                    // 类目轴：任何数据类型都可以，通常是字符串
                    return (true, null);
                    
                case "value":
                    // 数值轴：应该是数值类型
                    foreach (var item in data)
                    {
                        if (item != null && !(item is int || item is long || item is float || item is double || item is decimal))
                        {
                            return (false, $"数值轴(value)要求数据必须是数值类型，但发现了非数值数据: {item}");
                        }
                    }
                    return (true, null);
                    
                case "time":
                    // 时间轴：可以是DateTime对象或可以解析为日期时间的字符串
                    foreach (var item in data)
                    {
                        if (item == null) continue;
                        
                        if (item is DateTime) continue;
                        
                        if (item is string strValue)
                        {
                            if (!DateTime.TryParse(strValue, out _))
                            {
                                return (false, $"时间轴(time)要求数据必须是日期时间格式，但发现了无法解析为日期的字符串: {strValue}");
                            }
                        }
                        else
                        {
                            return (false, $"时间轴(time)要求数据必须是日期时间格式，但发现了非日期时间类型的数据: {item}");
                        }
                    }
                    return (true, null);
                    
                case "log":
                    // 对数轴：应该是正数值
                    foreach (var item in data)
                    {
                        if (item == null) continue;
                        
                        bool isValidValue = false;
                        double numValue = 0;
                        
                        if (item is int intValue) { numValue = intValue; isValidValue = true; }
                        else if (item is long longValue) { numValue = longValue; isValidValue = true; }
                        else if (item is float floatValue) { numValue = floatValue; isValidValue = true; }
                        else if (item is double doubleValue) { numValue = doubleValue; isValidValue = true; }
                        else if (item is decimal decimalValue) { numValue = (double)decimalValue; isValidValue = true; }
                        
                        if (!isValidValue)
                        {
                            return (false, $"对数轴(log)要求数据必须是数值类型，但发现了非数值数据: {item}");
                        }
                        
                        if (numValue <= 0)
                        {
                            return (false, $"对数轴(log)要求数据必须是正数，但发现了非正数值: {numValue}");
                        }
                    }
                    return (true, null);
                    
                default:
                    return (false, $"不支持的坐标轴类型: {axisType}，支持的类型有：category, value, time, log");
            }
        }
        
        #endregion
    }
} 