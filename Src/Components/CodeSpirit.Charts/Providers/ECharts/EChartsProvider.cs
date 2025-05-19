using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodeSpirit.Charts.Providers.ECharts;

/// <summary>
/// ECharts 图表提供者
/// </summary>
public class EChartsProvider : IChartProvider
{
    private readonly ILogger<EChartsProvider> _logger;
    private readonly HashSet<string> _supportedChartTypes;

    public EChartsProvider(ILogger<EChartsProvider> logger)
    {
        _logger = logger;
        _supportedChartTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "line", "bar", "pie", "scatter", "radar", "tree", "treemap", "sunburst",
            "boxplot", "candlestick", "heatmap", "map", "parallel", "lines", "graph",
            "sankey", "funnel", "gauge", "pictorialBar", "themeRiver", "custom"
        };
    }

    /// <inheritdoc />
    public string Name => "echarts";

    /// <inheritdoc />
    public IEnumerable<string> SupportedChartTypes => _supportedChartTypes;

    /// <inheritdoc />
    public bool SupportsChartType(string chartType)
    {
        return _supportedChartTypes.Contains(chartType);
    }

    /// <inheritdoc />
    public object GenerateChartConfig(string chartType, object data, object? options = null)
    {
        try
        {
            if (!SupportsChartType(chartType))
            {
                throw new ArgumentException($"Chart type '{chartType}' is not supported by ECharts provider.");
            }

            // 根据图表类型生成 ECharts 配置
            var config = chartType.ToLowerInvariant() switch
            {
                "line" => GenerateLineChartConfig(data, options),
                "bar" => GenerateBarChartConfig(data, options),
                "pie" => GeneratePieChartConfig(data, options),
                "scatter" => GenerateScatterChartConfig(data, options),
                "radar" => GenerateRadarChartConfig(data, options),
                "tree" => GenerateTreeChartConfig(data, options),
                "treemap" => GenerateTreemapChartConfig(data, options),
                "heatmap" => GenerateHeatmapChartConfig(data, options),
                "sankey" => GenerateSankeyChartConfig(data, options),
                _ => throw new NotImplementedException($"Chart type '{chartType}' is supported but not implemented.")
            };

            // 应用通用选项
            ApplyCommonOptions(config, options);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ECharts config for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    #region Private Methods

    private Dictionary<string, object> GenerateLineChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        try
        {
            _logger.LogInformation("生成折线图配置，数据类型: {DataType}", data?.GetType().FullName);
            
            // 提取字段信息
            string xField = "category";
            string yField = "value";
            
            // 从options中获取字段配置
            if (options != null)
            {
                _logger.LogInformation("处理折线图options，类型: {OptionsType}", options.GetType().FullName);
                
                Dictionary<string, object>? optionsDict = null;
                
                if (options is Dictionary<string, object> dictOptions)
                {
                    optionsDict = dictOptions;
                }
                else if (options is IDictionary<string, object> idictOptions)
                {
                    optionsDict = idictOptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                
                if (optionsDict != null)
                {
                    // 记录所有选项键
                    _logger.LogInformation("折线图选项键: {Keys}", string.Join(", ", optionsDict.Keys));
                    
                    // 尝试获取xField
                    if (optionsDict.TryGetValue("xField", out var xFieldObj) && xFieldObj is string xf)
                    {
                        xField = xf;
                        _logger.LogInformation("使用xField: {XField}", xField);
                    }
                    
                    // 尝试获取yField
                    if (optionsDict.TryGetValue("yField", out var yFieldObj) && yFieldObj is string yf)
                    {
                        yField = yf;
                        _logger.LogInformation("使用yField: {YField}", yField);
                    }
                }
            }
            
            // 提取数据点
            var xValues = new List<object>();
            var yValues = new List<object>();
            
            // 根据数据类型处理
            ProcessLineChartData(data, xField, yField, xValues, yValues);
            
            // 如果是标题选项，设置标题
            if (options is IDictionary<string, object> titleOptions && 
                titleOptions.TryGetValue("title", out var titleObj) && 
                titleObj is string title && !string.IsNullOrEmpty(title))
            {
                config["title"] = new Dictionary<string, object>
                {
                    ["text"] = title
                };
            }
            
            // 设置坐标轴和系列
            config["xAxis"] = new Dictionary<string, object>
            {
                ["type"] = "category",
                ["data"] = xValues.Count > 0 ? xValues.ToArray() : Array.Empty<object>()
            };
            
            config["yAxis"] = new Dictionary<string, object>
            {
                ["type"] = "value"
            };
            
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = yValues.Count > 0 ? yValues.ToArray() : Array.Empty<object>()
                }
            };
            
            // 保存字段名，方便后续处理
            if (!string.IsNullOrEmpty(xField))
                config["xField"] = xField;
            
            if (!string.IsNullOrEmpty(yField))
                config["yField"] = yField;
            
            // 添加自定义颜色配置
            var customColors = GetCustomColors(options);
            if (customColors.Any())
            {
                config["color"] = customColors;
            }
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成折线图配置时出错");
            
            // 返回基本配置
            config["xAxis"] = new Dictionary<string, object>
            {
                ["type"] = "category",
                ["data"] = Array.Empty<object>()
            };
            
            config["yAxis"] = new Dictionary<string, object>
            {
                ["type"] = "value"
            };
            
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = Array.Empty<object>()
                }
            };
            
            return config;
        }
    }
    
    private void ProcessLineChartData(object data, string xField, string yField, List<object> xValues, List<object> yValues)
    {
        // 如果数据为空，直接返回
        if (data == null)
        {
            _logger.LogWarning("ProcessLineChartData - 数据为空");
            return;
        }
        
        // 处理JSON字符串
        if (data is string jsonString)
        {
            try
            {
                var jsonData = JsonConvert.DeserializeObject(jsonString);
                if (jsonData != null)
                {
                    ProcessLineChartData(jsonData, xField, yField, xValues, yValues);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("解析JSON字符串失败: {Error}", ex.Message);
            }
        }
        
        // 处理JArray类型
        if (data != null && data.GetType().FullName == "Newtonsoft.Json.Linq.JArray")
        {
            try
            {
                var jArrayStr = data.ToString();
                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jArrayStr);
                if (items != null)
                {
                    ProcessLineChartData(items, xField, yField, xValues, yValues);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("处理JArray失败: {Error}", ex.Message);
            }
        }
        
        // 处理集合类型数据
        if (data is IEnumerable<object> collection)
        {
            foreach (var item in collection)
            {
                // 跳过空项
                if (item == null) continue;
                
                // 处理字典类型
                if (item is IDictionary<string, object> itemDict)
                {
                    if (itemDict.TryGetValue(xField, out var xValue) && xValue != null)
                    {
                        xValues.Add(xValue);
                    }
                    
                    if (itemDict.TryGetValue(yField, out var yValue) && yValue != null)
                    {
                        yValues.Add(yValue);
                    }
                }
                // 处理其他类型，尝试通过反射获取属性值
                else
                {
                    var props = item.GetType().GetProperties();
                    
                    var xProp = props.FirstOrDefault(p => string.Equals(p.Name, xField, StringComparison.OrdinalIgnoreCase));
                    if (xProp != null)
                    {
                        var xValue = xProp.GetValue(item);
                        if (xValue != null)
                        {
                            xValues.Add(xValue);
                        }
                    }
                    
                    var yProp = props.FirstOrDefault(p => string.Equals(p.Name, yField, StringComparison.OrdinalIgnoreCase));
                    if (yProp != null)
                    {
                        var yValue = yProp.GetValue(item);
                        if (yValue != null)
                        {
                            yValues.Add(yValue);
                        }
                    }
                }
            }
            
            _logger.LogInformation("提取了{XCount}个X值和{YCount}个Y值", xValues.Count, yValues.Count);
        }
    }

    private Dictionary<string, object> GenerateBarChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        try
        {
            _logger.LogInformation("生成条形图配置，数据类型: {DataType}", data?.GetType().FullName);
            
            // 提取字段信息
            string xField = "category";
            string yField = "value";
            
            // 从options中获取字段配置
            if (options != null)
            {
                _logger.LogInformation("处理条形图options，类型: {OptionsType}", options.GetType().FullName);
                
                Dictionary<string, object>? optionsDict = null;
                
                if (options is Dictionary<string, object> dictOptions)
                {
                    optionsDict = dictOptions;
                }
                else if (options is IDictionary<string, object> idictOptions)
                {
                    optionsDict = idictOptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                
                if (optionsDict != null)
                {
                    // 记录所有选项键
                    _logger.LogInformation("条形图选项键: {Keys}", string.Join(", ", optionsDict.Keys));
                    
                    // 尝试获取xField
                    if (optionsDict.TryGetValue("xField", out var xFieldObj) && xFieldObj is string xf)
                    {
                        xField = xf;
                        _logger.LogInformation("使用xField: {XField}", xField);
                    }
                    
                    // 尝试获取yField
                    if (optionsDict.TryGetValue("yField", out var yFieldObj) && yFieldObj is string yf)
                    {
                        yField = yf;
                        _logger.LogInformation("使用yField: {YField}", yField);
                    }
                }
            }
            
            // 提取数据点
            var xValues = new List<object>();
            var yValues = new List<object>();
            
            // 根据数据类型处理
            ProcessLineChartData(data, xField, yField, xValues, yValues);
            
            // 如果是标题选项，设置标题
            if (options is IDictionary<string, object> titleOptions && 
                titleOptions.TryGetValue("title", out var titleObj) && 
                titleObj is string title && !string.IsNullOrEmpty(title))
            {
                config["title"] = new Dictionary<string, object>
                {
                    ["text"] = title
                };
            }
            
            // 设置坐标轴和系列
            config["xAxis"] = new Dictionary<string, object>
            {
                ["type"] = "category",
                ["data"] = xValues.Count > 0 ? xValues.ToArray() : Array.Empty<object>()
            };
            
            config["yAxis"] = new Dictionary<string, object>
            {
                ["type"] = "value"
            };
            
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "bar",
                    ["data"] = yValues.Count > 0 ? yValues.ToArray() : Array.Empty<object>()
                }
            };
            
            // 保存字段名，方便后续处理
            if (!string.IsNullOrEmpty(xField))
                config["xField"] = xField;
            
            if (!string.IsNullOrEmpty(yField))
                config["yField"] = yField;
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成条形图配置时出错");
            
            // 返回基本配置
            config["xAxis"] = new Dictionary<string, object>
            {
                ["type"] = "category",
                ["data"] = Array.Empty<object>()
            };
            
            config["yAxis"] = new Dictionary<string, object>
            {
                ["type"] = "value"
            };
            
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "bar",
                    ["data"] = Array.Empty<object>()
                }
            };
            
            return config;
        }
    }

    private Dictionary<string, object> GeneratePieChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        try 
        {
            _logger.LogInformation("生成饼图配置，数据类型: {DataType}", data?.GetType().FullName);
            
            // 直接构造示例数据如果数据为空
            if (data == null)
            {
                _logger.LogWarning("传入的数据为null，使用示例数据");
                data = new[] 
                { 
                    new { name = "示例数据", value = 100 } 
                };
            }
            
            // 设置基本配置
            config["tooltip"] = new Dictionary<string, object>
            {
                ["trigger"] = "item",
                ["formatter"] = "{a} <br/>{b}: {c} ({d}%)"
            };
            
            config["legend"] = new Dictionary<string, object>
            {
                ["orient"] = "vertical",
                ["right"] = 10,
                ["top"] = "middle",
                ["data"] = new[] { "暂无数据" } // 默认图例
            };
            
            // 从options中提取标题
            if (options is IDictionary<string, object> optionsDict && 
                optionsDict.TryGetValue("title", out var titleObj) && 
                titleObj is string title && !string.IsNullOrEmpty(title))
            {
                config["title"] = new Dictionary<string, object>
                {
                    ["text"] = title,
                    ["left"] = "center",
                    ["top"] = 0
                };
            }
            
            // 提取饼图数据
            var pieData = ExtractPieData(data, options);
            
            // 如果提取失败，使用默认数据
            if (pieData == null || !pieData.Any())
            {
                _logger.LogWarning("无法从数据中提取有效的饼图数据，使用默认数据");
                pieData = new[]
                {
                    new Dictionary<string, object> { ["name"] = "暂无数据", ["value"] = 100 }
                };
            }
            
            // 生成图例数据
            var legendData = pieData.Select(item => 
            {
                if (item is IDictionary<string, object> dict && dict.TryGetValue("name", out var name))
                    return name?.ToString() ?? "未知";
                return "未知";
            }).ToArray();
            
            if (legendData.Any())
            {
                config["legend"] = new Dictionary<string, object>
                {
                    ["orient"] = "vertical",
                    ["right"] = 10,
                    ["top"] = "middle",
                    ["data"] = legendData
                };
            }
            
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = options is IDictionary<string, object> opts && 
                               opts.TryGetValue("title", out var seriesTitle) ? 
                               seriesTitle?.ToString() : "数据统计",
                    ["type"] = "pie",
                    ["radius"] = "50%",
                    ["center"] = new[] { "50%", "55%" },
                    ["data"] = pieData,
                    ["emphasis"] = new Dictionary<string, object>
                    {
                        ["itemStyle"] = new Dictionary<string, object>
                        {
                            ["shadowBlur"] = 10,
                            ["shadowOffsetX"] = 0,
                            ["shadowColor"] = "rgba(0, 0, 0, 0.5)"
                        }
                    }
                }
            };

            // 添加自定义颜色配置
            var customColors = GetCustomColors(options);
            if (customColors.Any())
            {
                config["color"] = customColors;
            }
            else
            {
                // 默认配色方案
                config["color"] = new[] { 
                    "#5470c6", "#91cc75", "#fac858", "#ee6666", 
                    "#73c0de", "#3ba272", "#fc8452", "#9a60b4", "#ea7ccc" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成饼图配置时出错");
            
            // 出错时返回默认配置
            config["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "pie",
                    ["radius"] = "50%",
                    ["data"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["name"] = "数据加载错误",
                            ["value"] = 100
                        }
                    }
                }
            };
        }
        
        return config;
    }

    private Dictionary<string, object> GenerateScatterChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 设置基本配置
        config["xAxis"] = new Dictionary<string, object>
        {
            ["type"] = "value"
        };
        
        config["yAxis"] = new Dictionary<string, object>
        {
            ["type"] = "value"
        };
        
        config["series"] = ExtractSeriesData(data, "scatter");
        
        return config;
    }

    private Dictionary<string, object> GenerateRadarChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 提取雷达图指标
        var indicators = ExtractRadarIndicators(data);
        
        // 设置基本配置
        config["radar"] = new Dictionary<string, object>
        {
            ["indicator"] = indicators
        };
        
        config["series"] = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "radar",
                ["data"] = ExtractRadarData(data)
            }
        };
        
        return config;
    }

    private Dictionary<string, object> GenerateTreeChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 设置基本配置
        config["series"] = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "tree",
                ["data"] = ExtractTreeData(data),
                ["layout"] = "orthogonal",
                ["orient"] = "vertical"
            }
        };
        
        return config;
    }

    private Dictionary<string, object> GenerateTreemapChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 设置基本配置
        config["series"] = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "treemap",
                ["data"] = ExtractTreemapData(data)
            }
        };
        
        return config;
    }

    private Dictionary<string, object> GenerateHeatmapChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 设置基本配置
        config["xAxis"] = new Dictionary<string, object>
        {
            ["type"] = "category",
            ["data"] = ExtractHeatmapXData(data)
        };
        
        config["yAxis"] = new Dictionary<string, object>
        {
            ["type"] = "category",
            ["data"] = ExtractHeatmapYData(data)
        };
        
        config["visualMap"] = new Dictionary<string, object>
        {
            ["min"] = 0,
            ["max"] = 10,
            ["calculable"] = true
        };
        
        config["series"] = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "heatmap",
                ["data"] = ExtractHeatmapData(data)
            }
        };
        
        return config;
    }

    private Dictionary<string, object> GenerateSankeyChartConfig(object data, object? options)
    {
        var config = new Dictionary<string, object>();
        
        // 设置基本配置
        config["series"] = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "sankey",
                ["data"] = ExtractSankeyNodes(data),
                ["links"] = ExtractSankeyLinks(data)
            }
        };
        
        return config;
    }

    private void ApplyCommonOptions(Dictionary<string, object> config, object? options)
    {
        if (options == null)
        {
            return;
        }

        // 应用通用选项
        if (options is Dictionary<string, object> optionsDict)
        {
            // 应用标题
            if (optionsDict.TryGetValue("title", out var titleObj) && titleObj is string title)
            {
                config["title"] = new Dictionary<string, object>
                {
                    ["text"] = title
                };
            }

            // 应用主题
            if (optionsDict.TryGetValue("theme", out var themeObj) && themeObj is string theme)
            {
                // 主题通常在 ECharts 初始化时应用，这里可以设置一些与主题相关的配置
            }

            // 应用图例
            if (optionsDict.TryGetValue("legend", out var legendObj))
            {
                config["legend"] = legendObj;
            }

            // 应用工具提示
            if (optionsDict.TryGetValue("tooltip", out var tooltipObj))
            {
                config["tooltip"] = tooltipObj;
            }

            // 应用网格
            if (optionsDict.TryGetValue("grid", out var gridObj))
            {
                config["grid"] = gridObj;
            }

            // 应用其他选项
            foreach (var kvp in optionsDict)
            {
                if (!config.ContainsKey(kvp.Key) && 
                    kvp.Key != "title" && 
                    kvp.Key != "theme" && 
                    kvp.Key != "legend" && 
                    kvp.Key != "tooltip" && 
                    kvp.Key != "grid")
                {
                    config[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    #region Data Extraction Methods

    private object[] ExtractCategoryData(object data)
    {
        _logger.LogInformation("ExtractCategoryData - 数据类型: {DataType}", data?.GetType().FullName);
        
        // 如果数据为空，返回空数组
        if (data == null)
        {
            _logger.LogWarning("ExtractCategoryData - 数据为空");
            return Array.Empty<object>();
        }
        
        // 从数据中提取类别数据
        // 首先处理常规情况
        if (data is Dictionary<string, object> dict && dict.TryGetValue("categories", out var categories))
        {
            _logger.LogInformation("从字典的'categories'键中提取类别数据");
            return categories as object[] ?? Array.Empty<object>();
        }
        
        // 如果是JSON字符串，尝试解析
        if (data is string jsonString)
        {
            try
            {
                _logger.LogInformation("尝试解析JSON字符串提取类别数据");
                var jsonData = JsonConvert.DeserializeObject(jsonString);
                if (jsonData != null)
                {
                    return ExtractCategoryData(jsonData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("解析JSON失败: {Error}", ex.Message);
            }
        }
        
        // 如果是集合或数组，尝试提取每个项目中的第一个字段值
        if (data is IEnumerable<object> collection)
        {
            _logger.LogInformation("从集合中提取类别数据");
            var result = new List<object>();
            
            foreach (var item in collection)
            {
                // 处理字典类型
                if (item is IDictionary<string, object> itemDict && itemDict.Keys.Any())
                {
                    var firstKey = itemDict.Keys.FirstOrDefault();
                    if (firstKey != null && itemDict.TryGetValue(firstKey, out var value))
                    {
                        result.Add(value);
                    }
                }
                // 处理其他类型，尝试通过反射获取第一个属性值
                else if (item != null)
                {
                    var props = item.GetType().GetProperties();
                    if (props.Length > 0)
                    {
                        result.Add(props[0].GetValue(item));
                    }
                }
            }
            
            if (result.Any())
            {
                _logger.LogInformation("从集合中提取了{Count}个类别值", result.Count);
                return result.ToArray();
            }
        }
        
        // 处理JArray类型
        if (data != null && data.GetType().FullName == "Newtonsoft.Json.Linq.JArray")
        {
            try
            {
                _logger.LogInformation("尝试处理JArray类型提取类别数据");
                var jArrayStr = data.ToString();
                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jArrayStr);
                if (items != null && items.Any())
                {
                    return ExtractCategoryData(items);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("处理JArray失败: {Error}", ex.Message);
            }
        }
        
        _logger.LogWarning("无法提取类别数据");
        return Array.Empty<object>();
    }

    private object[] ExtractSeriesData(object data, string seriesType)
    {
        _logger.LogInformation("ExtractSeriesData - 数据类型: {DataType}, 系列类型: {SeriesType}", data?.GetType().FullName, seriesType);
        
        // 如果数据为空，返回默认配置
        if (data == null)
        {
            _logger.LogWarning("ExtractSeriesData - 数据为空");
            return new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = seriesType,
                    ["data"] = Array.Empty<object>()
                }
            };
        }
        
        // 常规情况：从数据中直接获取series字段
        if (data is Dictionary<string, object> dict && dict.TryGetValue("series", out var series))
        {
            _logger.LogInformation("从字典的'series'键中提取系列数据");
            if (series is IEnumerable<object> seriesArray)
            {
                var result = new List<Dictionary<string, object>>();
                
                foreach (var item in seriesArray)
                {
                    if (item is Dictionary<string, object> seriesDict)
                    {
                        var newSeries = new Dictionary<string, object>(seriesDict)
                        {
                            ["type"] = seriesType
                        };
                        
                        result.Add(newSeries);
                    }
                }
                
                if (result.Any())
                {
                    _logger.LogInformation("提取了{Count}个系列配置", result.Count);
                    return result.ToArray();
                }
            }
        }
        
        // 如果是JSON字符串，尝试解析
        if (data is string jsonString)
        {
            try
            {
                _logger.LogInformation("尝试解析JSON字符串提取系列数据");
                var jsonData = JsonConvert.DeserializeObject(jsonString);
                if (jsonData != null)
                {
                    return ExtractSeriesData(jsonData, seriesType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("解析JSON失败: {Error}", ex.Message);
            }
        }
        
        // 处理JArray类型
        if (data != null && data.GetType().FullName == "Newtonsoft.Json.Linq.JArray")
        {
            try
            {
                _logger.LogInformation("尝试处理JArray类型提取系列数据");
                var jArrayStr = data.ToString();
                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jArrayStr);
                if (items != null && items.Any())
                {
                    return ExtractSeriesData(items, seriesType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("处理JArray失败: {Error}", ex.Message);
            }
        }
        
        // 如果是集合或数组，尝试提取每个项目中的第二个字段值作为数据
        if (data is IEnumerable<object> collection)
        {
            _logger.LogInformation("从集合中提取系列数据");
            var result = new List<object>();
            
            foreach (var item in collection)
            {
                // 处理字典类型
                if (item is IDictionary<string, object> itemDict && itemDict.Keys.Count >= 2)
                {
                    var secondKey = itemDict.Keys.Skip(1).FirstOrDefault();
                    if (secondKey != null && itemDict.TryGetValue(secondKey, out var value))
                    {
                        result.Add(value);
                    }
                }
                // 处理其他类型，尝试通过反射获取第二个属性值
                else if (item != null)
                {
                    var props = item.GetType().GetProperties();
                    if (props.Length >= 2)
                    {
                        result.Add(props[1].GetValue(item));
                    }
                }
            }
            
            if (result.Any())
            {
                _logger.LogInformation("从集合中提取了{Count}个系列数据项", result.Count);
                return new[]
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = seriesType,
                        ["data"] = result.ToArray()
                    }
                };
            }
        }
        
        // 返回默认空配置
        _logger.LogWarning("无法提取系列数据，返回默认空配置");
        return new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = seriesType,
                ["data"] = Array.Empty<object>()
            }
        };
    }

    private object[] ExtractPieData(object data, object? options)
    {
        // 记录详细的数据类型和格式
        _logger.LogInformation("ExtractPieData - 数据类型: {DataType}", data?.GetType().FullName);
        
        // 如果数据为空，返回空数组
        if (data == null)
        {
            _logger.LogWarning("ExtractPieData - 数据为空");
            return Array.Empty<object>();
        }
        
        // 详细记录数据内容(限制长度以避免日志过大)
        var dataStr = data.ToString();
        if (dataStr != null && dataStr.Length > 500)
        {
            dataStr = dataStr.Substring(0, 497) + "...";
        }
        _logger.LogInformation("ExtractPieData - 数据内容: {Data}", dataStr);
        
        // 提取字段名
        string categoryField = "category";  // 默认分类字段名
        string valueField = "value";        // 默认值字段名
        
        // 从options中提取字段信息
        if (options != null)
        {
            _logger.LogInformation("处理options，类型: {OptionsType}", options.GetType().FullName);
            
            // 转换选项为字典
            Dictionary<string, object>? optionsDict = null;
            
            if (options is Dictionary<string, object> dictOptions)
            {
                optionsDict = dictOptions;
            }
            else if (options is IDictionary<string, object> idictOptions)
            {
                optionsDict = idictOptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            
            if (optionsDict != null)
            {
                // 记录所有选项键
                _logger.LogInformation("选项键: {Keys}", string.Join(", ", optionsDict.Keys));
                
                // 尝试获取CategoryField
                if (optionsDict.TryGetValue("categoryField", out var categoryObj) && categoryObj is string category)
                {
                    categoryField = category;
                    _logger.LogInformation("使用categoryField: {Category}", categoryField);
                }
                
                // 尝试获取ValueField
                if (optionsDict.TryGetValue("valueField", out var valueObj) && valueObj is string value)
                {
                    valueField = value;
                    _logger.LogInformation("使用valueField: {Value}", valueField);
                }
                
                // 如果有userOptions，从中获取字段信息
                if (optionsDict.TryGetValue("userOptions", out var userOptionsObj))
                {
                    _logger.LogInformation("发现userOptions，类型: {Type}", userOptionsObj?.GetType().FullName);
                    
                    if (userOptionsObj is Dictionary<string, object> userOptions)
                    {
                        if (userOptions.TryGetValue("categoryField", out var catField) && catField is string cf)
                        {
                            categoryField = cf;
                            _logger.LogInformation("从userOptions中使用categoryField: {Category}", categoryField);
                        }
                        
                        if (userOptions.TryGetValue("valueField", out var valField) && valField is string vf)
                        {
                            valueField = vf;
                            _logger.LogInformation("从userOptions中使用valueField: {Value}", valueField);
                        }
                    }
                }
            }
        }
        
        // 处理匿名类型
        if (data.GetType().Name.Contains("AnonymousType"))
        {
            _logger.LogInformation("处理匿名类型数据");
            try
            {
                // 将匿名类型转换为字典集合
                var properties = data.GetType().GetProperties();
                var dictData = new Dictionary<string, object>();
                
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data);
                    if (value != null)
                    {
                        dictData[prop.Name] = value;
                    }
                }
                
                _logger.LogInformation("成功将匿名类型转换为字典，包含 {Count} 个属性", dictData.Count);
                
                // 如果是集合数据，处理成饼图数据格式
                if (dictData.TryGetValue("Items", out var items) && items is IEnumerable<object> itemsCollection)
                {
                    _logger.LogInformation("从匿名类型中提取Items集合，类型: {Type}", items.GetType().FullName);
                    return ExtractPieData(items, options);
                }
                
                // 将字典转换为单条目饼图数据
                var result = new List<Dictionary<string, object>>();
                foreach (var kvp in dictData)
                {
                    if (kvp.Value is IConvertible || kvp.Value is decimal)
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["name"] = kvp.Key,
                            ["value"] = ConvertToNumeric(kvp.Value)
                        });
                    }
                }
                
                if (result.Any())
                {
                    _logger.LogInformation("从匿名类型字典中提取了 {Count} 项饼图数据", result.Count);
                    return result.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("处理匿名类型失败: {Error}", ex.Message);
            }
        }
        
        // 判断是否是字符串类型（可能是JSON）
        if (data is string jsonString)
        {
            try
            {
                // 尝试解析JSON字符串
                var jsonData = JsonConvert.DeserializeObject(jsonString);
                if (jsonData != null)
                {
                    _logger.LogInformation("成功解析JSON字符串为：{JsonType}", jsonData.GetType().FullName);
                    
                    // 递归调用，处理解析后的数据
                    return ExtractPieData(jsonData, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("解析JSON字符串失败: {Error}", ex.Message);
            }
        }
        
        // 处理Newtonsoft.Json.Linq.JArray类型
        if (data != null && data.GetType().FullName == "Newtonsoft.Json.Linq.JArray")
        {
            try
            {
                // 转换为.NET对象
                var jArrayStr = data.ToString();
                var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jArrayStr);
                if (items != null)
                {
                    _logger.LogInformation("成功将JArray转换为List<Dictionary<string, object>>，共{Count}项", items.Count);
                    return ExtractPieData(items, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("转换JArray失败: {Error}", ex.Message);
            }
        }
        
        // 如果数据是字典类型，尝试从"data"键中获取饼图数据
        if (data is Dictionary<string, object> dict && dict.TryGetValue("data", out var pieData))
        {
            _logger.LogInformation("从字典中获取数据，类型: {DataType}", pieData?.GetType().FullName);
            return pieData as object[] ?? Array.Empty<object>();
        }
        
        // 如果是直接的集合数据
        if (data is IEnumerable<object> collection)
        {
            var result = new List<Dictionary<string, object>>();
            int count = 0;
            bool hasValidData = false;
            
            // 遍历集合，转换为饼图所需的数据格式
            foreach (var item in collection)
            {
                count++;
                if (item == null)
                {
                    _logger.LogWarning("项 #{Index} 为null，跳过", count);
                    continue;
                }
                
                _logger.LogInformation("处理集合项 #{Index}，类型: {ItemType}", count, item.GetType().FullName);
                
                // 创建饼图数据项
                var pieItem = new Dictionary<string, object>();
                bool isValidItem = false;
                
                // 处理字典类型的项
                if (item is IDictionary<string, object> itemDict)
                {
                    // 记录可用的键
                    _logger.LogInformation("项 #{Index} 的键: {Keys}", count, string.Join(", ", itemDict.Keys));
                    
                    // 设置name和value
                    if (itemDict.TryGetValue(categoryField, out var category))
                    {
                        pieItem["name"] = category?.ToString() ?? "未知";
                        _logger.LogInformation("项 #{Index} 设置name: {Name}", count, pieItem["name"]);
                        isValidItem = true;
                    }
                    else
                    {
                        _logger.LogWarning("项 #{Index} 未找到categoryField: {Field}", count, categoryField);
                    }
                    
                    if (itemDict.TryGetValue(valueField, out var value))
                    {
                        // 尝试将值转换为数字
                        pieItem["value"] = ConvertToNumeric(value);
                        _logger.LogInformation("项 #{Index} 设置value: {Value}", count, pieItem["value"]);
                        isValidItem = true;
                    }
                    else
                    {
                        _logger.LogWarning("项 #{Index} 未找到valueField: {Field}", count, valueField);
                    }
                }
                // 处理JObject类型的项
                else if (item != null && item.GetType().FullName == "Newtonsoft.Json.Linq.JObject")
                {
                    try
                    {
                        var jObjectStr = item.ToString();
                        var jObjectDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jObjectStr);
                        if (jObjectDict != null)
                        {
                            _logger.LogInformation("项 #{Index} 的JObject键: {Keys}", count, string.Join(", ", jObjectDict.Keys));
                            
                            // 设置name和value
                            if (jObjectDict.TryGetValue(categoryField, out var category))
                            {
                                pieItem["name"] = category?.ToString() ?? "未知";
                                _logger.LogInformation("JObject项 #{Index} 设置name: {Name}", count, pieItem["name"]);
                                isValidItem = true;
                            }
                            
                            if (jObjectDict.TryGetValue(valueField, out var value))
                            {
                                // 尝试将值转换为数字
                                pieItem["value"] = ConvertToNumeric(value);
                                _logger.LogInformation("JObject项 #{Index} 设置value: {Value}", count, pieItem["value"]);
                                isValidItem = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("处理JObject项 #{Index} 失败: {Error}", count, ex.Message);
                    }
                }
                // 处理匿名类型项
                else if (item.GetType().Name.Contains("AnonymousType"))
                {
                    try
                    {
                        _logger.LogInformation("处理匿名类型项 #{Index}", count);
                        var properties = item.GetType().GetProperties();
                        
                        // 记录可用的属性
                        _logger.LogInformation("匿名类型项 #{Index} 的属性: {Properties}", count, 
                            string.Join(", ", properties.Select(p => p.Name)));
                            
                        // 寻找分类和值属性
                        var categoryProp = properties.FirstOrDefault(p => 
                            string.Equals(p.Name, categoryField, StringComparison.OrdinalIgnoreCase));
                            
                        var valueProp = properties.FirstOrDefault(p => 
                            string.Equals(p.Name, valueField, StringComparison.OrdinalIgnoreCase));
                            
                        // 如果没有找到精确匹配，尝试模糊匹配
                        if (categoryProp == null)
                        {
                            categoryProp = properties.FirstOrDefault(p => 
                                p.Name.Contains("name", StringComparison.OrdinalIgnoreCase) || 
                                p.Name.Contains("category", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Contains("label", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Contains("key", StringComparison.OrdinalIgnoreCase));
                        }
                        
                        if (valueProp == null)
                        {
                            valueProp = properties.FirstOrDefault(p => 
                                p.Name.Contains("value", StringComparison.OrdinalIgnoreCase) || 
                                p.Name.Contains("count", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Contains("sum", StringComparison.OrdinalIgnoreCase));
                        }
                        
                        // 如果仍然没有找到，使用前两个属性
                        if (categoryProp == null && properties.Length > 0)
                        {
                            categoryProp = properties[0];
                        }
                        
                        if (valueProp == null && properties.Length > 1)
                        {
                            valueProp = properties[1];
                        }
                        
                        // 设置name和value
                        if (categoryProp != null)
                        {
                            var categoryValue = categoryProp.GetValue(item);
                            pieItem["name"] = categoryValue?.ToString() ?? "未知";
                            _logger.LogInformation("匿名类型项 #{Index} 设置name: {Name}，来自属性: {Prop}", 
                                count, pieItem["name"], categoryProp.Name);
                            isValidItem = true;
                        }
                        
                        if (valueProp != null)
                        {
                            var valueObj = valueProp.GetValue(item);
                            pieItem["value"] = ConvertToNumeric(valueObj);
                            _logger.LogInformation("匿名类型项 #{Index} 设置value: {Value}，来自属性: {Prop}", 
                                count, pieItem["value"], valueProp.Name);
                            isValidItem = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("处理匿名类型项 #{Index} 失败: {Error}", count, ex.Message);
                    }
                }
                // 处理其他类型的项 - 尝试通过反射获取属性
                else
                {
                    try
                    {
                        var itemType = item.GetType();
                        var properties = itemType.GetProperties();
                        
                        // 记录可用的属性
                        _logger.LogInformation("项 #{Index} 的属性: {Properties}", count, 
                            string.Join(", ", properties.Select(p => p.Name)));
                        
                        // 查找分类字段属性
                        var categoryProp = properties.FirstOrDefault(p => 
                            string.Equals(p.Name, categoryField, StringComparison.OrdinalIgnoreCase));
                            
                        if (categoryProp != null)
                        {
                            var categoryValue = categoryProp.GetValue(item);
                            pieItem["name"] = categoryValue?.ToString() ?? "未知";
                            _logger.LogInformation("项 #{Index} 通过反射设置name: {Name}", count, pieItem["name"]);
                            isValidItem = true;
                        }
                        
                        // 查找值字段属性
                        var valueProp = properties.FirstOrDefault(p => 
                            string.Equals(p.Name, valueField, StringComparison.OrdinalIgnoreCase));
                            
                        if (valueProp != null)
                        {
                            var valueObj = valueProp.GetValue(item);
                            pieItem["value"] = ConvertToNumeric(valueObj);
                            _logger.LogInformation("项 #{Index} 通过反射设置value: {Value}", count, pieItem["value"]);
                            isValidItem = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("通过反射处理项 #{Index} 失败: {Error}", count, ex.Message);
                    }
                }
                
                // 只添加有效的数据项
                if (isValidItem)
                {
                    result.Add(pieItem);
                    hasValidData = true;
                }
            }
            
            _logger.LogInformation("从{TotalCount}个项中提取了{ValidCount}个有效的饼图数据项", count, result.Count);
            
            if (hasValidData)
            {
                return result.ToArray();
            }
        }
        
        // 如果没有有效数据，返回空数组
        _logger.LogWarning("无法从给定数据中提取有效的饼图数据");
        return Array.Empty<object>();
    }
    
    private object ConvertToNumeric(object? value)
    {
        if (value == null) return 0;
        
        // 已经是数值类型
        if (value is int intValue) return intValue;
        if (value is double doubleValue) return doubleValue;
        if (value is long longValue) return longValue;
        if (value is decimal decimalValue) return (double)decimalValue;
        if (value is float floatValue) return (double)floatValue;
        
        // 尝试转换字符串
        if (value is string strValue)
        {
            if (int.TryParse(strValue, out var parsedInt))
                return parsedInt;
            if (double.TryParse(strValue, out var parsedDouble))
                return parsedDouble;
        }
        
        // 默认返回0
        return 0;
    }

    private object[] ExtractRadarIndicators(object data)
    {
        // 从数据中提取雷达图指标
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("indicators", out var indicators))
        {
            return indicators as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractRadarData(object data)
    {
        // 从数据中提取雷达图数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("data", out var radarData))
        {
            return radarData as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractTreeData(object data)
    {
        // 从数据中提取树图数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("data", out var treeData))
        {
            return new[] { treeData };
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractTreemapData(object data)
    {
        // 从数据中提取矩形树图数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("data", out var treemapData))
        {
            return treemapData as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractHeatmapXData(object data)
    {
        // 从数据中提取热力图 X 轴数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("xAxis", out var xAxis))
        {
            return xAxis as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractHeatmapYData(object data)
    {
        // 从数据中提取热力图 Y 轴数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("yAxis", out var yAxis))
        {
            return yAxis as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractHeatmapData(object data)
    {
        // 从数据中提取热力图数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("data", out var heatmapData))
        {
            return heatmapData as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractSankeyNodes(object data)
    {
        // 从数据中提取桑基图节点数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("nodes", out var nodes))
        {
            return nodes as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private object[] ExtractSankeyLinks(object data)
    {
        // 从数据中提取桑基图链接数据
        // 这里是一个简单的示例，实际实现可能需要更复杂的逻辑
        if (data is Dictionary<string, object> dict && dict.TryGetValue("links", out var links))
        {
            return links as object[] ?? Array.Empty<object>();
        }
        
        return Array.Empty<object>();
    }

    private string[] GetCustomColors(object? options)
    {
        if (options is Dictionary<string, object> optionsDict && 
            optionsDict.TryGetValue("colors", out var colorsObj) && 
            colorsObj is IEnumerable<string> colors)
        {
            return colors.ToArray();
        }
        
        return Array.Empty<string>();
    }

    #endregion

    #endregion
}