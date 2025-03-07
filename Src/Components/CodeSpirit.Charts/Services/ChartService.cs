using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace CodeSpirit.Charts.Services
{
    /// <summary>
    /// 图表服务实现
    /// </summary>
    public class ChartService : IChartService
    {
        private readonly IChartRecommender _chartRecommender;
        private readonly IDataAnalyzer _dataAnalyzer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChartService> _logger;
        
        // 内存中缓存图表配置（生产环境应改为持久化存储）
        private readonly Dictionary<string, ChartConfig> _chartConfigCache = new();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartService(
            IChartRecommender chartRecommender,
            IDataAnalyzer dataAnalyzer,
            IHttpClientFactory httpClientFactory,
            ILogger<ChartService> logger)
        {
            _chartRecommender = chartRecommender;
            _dataAnalyzer = dataAnalyzer;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        
        /// <summary>
        /// 根据控制器方法生成图表配置
        /// </summary>
        public async Task<ChartConfig> GenerateChartConfigAsync(MethodInfo methodInfo)
        {
            try
            {
                _logger.LogInformation("开始根据方法 {MethodName} 生成图表配置", methodInfo.Name);
                
                // 获取Chart特性
                var chartAttr = methodInfo.GetCustomAttribute<ChartAttribute>();
                if (chartAttr == null)
                {
                    _logger.LogWarning("方法 {MethodName} 未标记Chart特性", methodInfo.Name);
                    return new ChartConfig { Title = methodInfo.Name };
                }
                
                // 创建基本配置
                var config = new ChartConfig
                {
                    Title = !string.IsNullOrEmpty(chartAttr.Title) ? chartAttr.Title : methodInfo.Name,
                    AutoRefresh = chartAttr.AutoRefresh,
                    RefreshInterval = chartAttr.RefreshInterval,
                    Theme = chartAttr.Theme
                };
                
                // 获取图表类型特性
                var chartTypeAttr = methodInfo.GetCustomAttribute<ChartTypeAttribute>();
                if (chartTypeAttr != null)
                {
                    config.Type = chartTypeAttr.Type;
                    config.SubType = chartTypeAttr.SubType;
                }
                
                // 获取数据特性
                var chartDataAttr = methodInfo.GetCustomAttribute<ChartDataAttribute>();
                if (chartDataAttr != null)
                {
                    config.DataSource = new ChartDataSource();
                    
                    // 设置数据映射
                    if (!string.IsNullOrEmpty(chartDataAttr.DimensionField) || chartDataAttr.DimensionFields?.Length > 0)
                    {
                        config.DataSource.Mapping = new DataMapping
                        {
                            DimensionField = chartDataAttr.DimensionField,
                            DimensionFields = chartDataAttr.DimensionFields?.ToList(),
                            MetricFields = chartDataAttr.MetricFields?.ToList()
                        };
                    }
                    
                    // 设置数据分析配置
                    if (chartDataAttr.EnableTrendAnalysis || chartDataAttr.EnableAnomalyDetection || chartDataAttr.EnableForecasting)
                    {
                        config.DataSource.Analysis = new DataAnalysisConfig
                        {
                            EnableTrendAnalysis = chartDataAttr.EnableTrendAnalysis,
                            EnableAnomalyDetection = chartDataAttr.EnableAnomalyDetection,
                            EnableForecasting = chartDataAttr.EnableForecasting,
                            ForecastSteps = chartDataAttr.ForecastSteps
                        };
                    }
                }
                
                // 添加工具箱配置
                if (chartAttr.ShowToolbox)
                {
                    config.Toolbox = new ToolboxConfig
                    {
                        Features = new Dictionary<string, bool>
                        {
                            { "saveAsImage", chartAttr.EnableExport },
                            { "dataView", true },
                            { "restore", true },
                            { "dataZoom", true },
                            { "magicType", true }
                        }
                    };
                }
                
                // 添加交互配置
                if (chartAttr.EnableInteraction)
                {
                    config.Interaction = new InteractionConfig
                    {
                        Tooltip = new Dictionary<string, object>
                        {
                            { "show", true }
                        }
                    };
                }
                
                _logger.LogInformation("成功根据方法 {MethodName} 生成图表配置", methodInfo.Name);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据方法 {MethodName} 生成图表配置时出错", methodInfo.Name);
                throw;
            }
        }
        
        /// <summary>
        /// 根据数据自动推荐图表类型
        /// </summary>
        public async Task<ChartType> RecommendChartTypeAsync(object data)
        {
            try
            {
                _logger.LogInformation("开始根据数据推荐图表类型");
                var chartType = _chartRecommender.RecommendChartType(data);
                _logger.LogInformation("推荐的图表类型为: {ChartType}", chartType);
                return chartType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推荐图表类型时出错");
                return ChartType.Bar; // 默认返回柱状图
            }
        }
        
        /// <summary>
        /// 获取图表数据
        /// </summary>
        public async Task<object> GetChartDataAsync(ChartDataSource dataSource)
        {
            try
            {
                _logger.LogInformation("开始获取图表数据，数据源类型: {DataSourceType}", dataSource.Type);
                
                object result;
                
                switch (dataSource.Type)
                {
                    case DataSourceType.Static:
                        result = dataSource.StaticData;
                        break;
                        
                    case DataSourceType.Api:
                        if (string.IsNullOrEmpty(dataSource.ApiUrl))
                        {
                            throw new ArgumentException("API URL不能为空");
                        }
                        
                        result = await FetchApiDataAsync(dataSource.ApiUrl, dataSource.Method, dataSource.Parameters);
                        break;
                        
                    case DataSourceType.Current:
                        result = dataSource.StaticData;
                        break;
                        
                    default:
                        throw new NotSupportedException($"不支持的数据源类型: {dataSource.Type}");
                }
                
                // 应用数据转换
                if (dataSource.Transformers?.Count > 0)
                {
                    result = ApplyTransformers(result, dataSource.Transformers);
                }
                
                _logger.LogInformation("成功获取图表数据");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图表数据时出错");
                throw;
            }
        }
        
        /// <summary>
        /// 导出图表为图片
        /// </summary>
        public async Task<byte[]> ExportChartAsImageAsync(ChartConfig config)
        {
            _logger.LogInformation("导出图表为图片功能尚未实现");
            throw new NotImplementedException("导出图表为图片功能尚未实现");
        }
        
        /// <summary>
        /// 导出图表数据为Excel
        /// </summary>
        public async Task<byte[]> ExportChartDataAsExcelAsync(ChartConfig config)
        {
            _logger.LogInformation("导出图表数据为Excel功能尚未实现");
            throw new NotImplementedException("导出图表数据为Excel功能尚未实现");
        }
        
        /// <summary>
        /// 分析数据自动生成图表
        /// </summary>
        public async Task<ChartConfig> AnalyzeAndGenerateChartAsync(object data)
        {
            try
            {
                _logger.LogInformation("开始分析数据并生成图表");
                var config = _chartRecommender.GenerateChartConfig(data);
                _logger.LogInformation("成功分析数据并生成图表，类型: {ChartType}", config.Type);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析数据并生成图表时出错");
                throw;
            }
        }
        
        /// <summary>
        /// 保存图表配置
        /// </summary>
        public async Task<string> SaveChartConfigAsync(ChartConfig config)
        {
            try
            {
                _logger.LogInformation("保存图表配置，ID: {ChartId}", config.Id);
                
                if (string.IsNullOrEmpty(config.Id))
                {
                    config.Id = Guid.NewGuid().ToString();
                }
                
                // 保存到缓存（生产环境应保存到数据库）
                _chartConfigCache[config.Id] = config;
                
                _logger.LogInformation("成功保存图表配置，ID: {ChartId}", config.Id);
                return config.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存图表配置时出错");
                throw;
            }
        }
        
        /// <summary>
        /// 获取图表配置
        /// </summary>
        public async Task<ChartConfig> GetChartConfigAsync(string id)
        {
            try
            {
                _logger.LogInformation("获取图表配置，ID: {ChartId}", id);
                
                if (string.IsNullOrEmpty(id) || !_chartConfigCache.TryGetValue(id, out var config))
                {
                    throw new KeyNotFoundException($"未找到ID为 {id} 的图表配置");
                }
                
                _logger.LogInformation("成功获取图表配置，ID: {ChartId}", id);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取图表配置时出错，ID: {ChartId}", id);
                throw;
            }
        }
        
        /// <summary>
        /// 生成图表JSON配置
        /// </summary>
        public async Task<JObject> GenerateChartJsonAsync(ChartConfig config)
        {
            try
            {
                _logger.LogInformation("开始生成图表JSON配置，类型: {ChartType}", config.Type);
                
                var chartJson = new JObject
                {
                    ["title"] = new JObject
                    {
                        ["text"] = config.Title,
                        ["left"] = "center"
                    },
                    
                    ["tooltip"] = CreateTooltipJson(config)
                };
                
                // 添加工具箱
                if (config.Toolbox != null)
                {
                    chartJson["toolbox"] = CreateToolboxJson(config.Toolbox);
                }
                
                // 添加图例
                if (config.Legend != null)
                {
                    chartJson["legend"] = CreateLegendJson(config.Legend);
                }
                
                // 添加坐标轴（饼图不需要）
                if (config.Type != ChartType.Pie && config.Type != ChartType.Radar)
                {
                    if (config.XAxis != null)
                    {
                        chartJson["xAxis"] = CreateAxisJson(config.XAxis);
                    }
                    
                    if (config.YAxis != null)
                    {
                        chartJson["yAxis"] = CreateAxisJson(config.YAxis);
                    }
                }
                
                // 添加系列
                if (config.Series.Count > 0)
                {
                    chartJson["series"] = new JArray(config.Series.Select(CreateSeriesJson));
                }
                
                // 添加其他选项
                if (config.ExtraStyles != null)
                {
                    foreach (var kvp in config.ExtraStyles)
                    {
                        chartJson[kvp.Key] = JToken.FromObject(kvp.Value);
                    }
                }
                
                _logger.LogInformation("成功生成图表JSON配置");
                return chartJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成图表JSON配置时出错");
                throw;
            }
        }
        
        /// <summary>
        /// 获取推荐的多个图表类型
        /// </summary>
        public async Task<Dictionary<ChartType, double>> GetRecommendedChartTypesAsync(object data, int maxCount = 3)
        {
            try
            {
                _logger.LogInformation("开始获取推荐的图表类型");
                var chartTypes = _chartRecommender.RecommendChartTypes(data, maxCount);
                _logger.LogInformation("成功获取推荐的图表类型，数量: {Count}", chartTypes.Count);
                return chartTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐的图表类型时出错");
                throw;
            }
        }
        
        #region 辅助方法
        
        /// <summary>
        /// 从API获取数据
        /// </summary>
        private async Task<object> FetchApiDataAsync(string apiUrl, string method, Dictionary<string, object>? parameters)
        {
            var client = _httpClientFactory.CreateClient();
            
            switch (method.ToUpperInvariant())
            {
                case "GET":
                    if (parameters != null && parameters.Count > 0)
                    {
                        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString() ?? string.Empty)}"));
                        apiUrl = $"{apiUrl}?{queryString}";
                    }
                    
                    var getResponse = await client.GetAsync(apiUrl);
                    getResponse.EnsureSuccessStatusCode();
                    return await getResponse.Content.ReadFromJsonAsync<object>();
                    
                case "POST":
                    var postContent = parameters != null
                        ? new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json")
                        : null;
                        
                    var postResponse = await client.PostAsync(apiUrl, postContent);
                    postResponse.EnsureSuccessStatusCode();
                    return await postResponse.Content.ReadFromJsonAsync<object>();
                    
                default:
                    throw new NotSupportedException($"不支持的HTTP方法: {method}");
            }
        }
        
        /// <summary>
        /// 应用数据转换器
        /// </summary>
        private object ApplyTransformers(object data, List<DataTransformer> transformers)
        {
            // 此处简单实现，生产环境应支持复杂转换
            return data;
        }
        
        /// <summary>
        /// 创建提示框JSON
        /// </summary>
        private JObject CreateTooltipJson(ChartConfig config)
        {
            var tooltip = new JObject
            {
                ["trigger"] = config.Type == ChartType.Pie ? "item" : "axis"
            };
            
            if (config.Interaction?.Tooltip != null)
            {
                foreach (var kvp in config.Interaction.Tooltip)
                {
                    tooltip[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }
            
            return tooltip;
        }
        
        /// <summary>
        /// 创建工具箱JSON
        /// </summary>
        private JObject CreateToolboxJson(ToolboxConfig toolbox)
        {
            var toolboxJson = new JObject
            {
                ["show"] = toolbox.Show,
                ["orient"] = toolbox.Orient
            };
            
            var features = new JObject();
            foreach (var feature in toolbox.Features)
            {
                features[feature.Key] = feature.Value;
            }
            
            toolboxJson["feature"] = features;
            
            return toolboxJson;
        }
        
        /// <summary>
        /// 创建图例JSON
        /// </summary>
        private JObject CreateLegendJson(LegendConfig legend)
        {
            var legendJson = new JObject
            {
                ["show"] = legend.Show,
                ["orient"] = legend.Orient
            };
            
            if (!string.IsNullOrEmpty(legend.Position))
            {
                legendJson["right"] = legend.Position;
            }
            
            if (legend.Data != null && legend.Data.Count > 0)
            {
                legendJson["data"] = new JArray(legend.Data);
            }
            
            return legendJson;
        }
        
        /// <summary>
        /// 创建坐标轴JSON
        /// </summary>
        private JObject CreateAxisJson(AxisConfig axis)
        {
            var axisJson = new JObject
            {
                ["type"] = axis.Type,
                ["show"] = axis.Show
            };
            
            if (!string.IsNullOrEmpty(axis.Name))
            {
                axisJson["name"] = axis.Name;
            }
            
            if (axis.Data != null && axis.Data.Count > 0)
            {
                axisJson["data"] = new JArray(axis.Data);
            }
            
            if (axis.Inverse)
            {
                axisJson["inverse"] = true;
            }
            
            if (axis.AxisLine != null)
            {
                axisJson["axisLine"] = JObject.FromObject(axis.AxisLine);
            }
            
            if (axis.AxisLabel != null)
            {
                axisJson["axisLabel"] = JObject.FromObject(axis.AxisLabel);
            }
            
            return axisJson;
        }
        
        /// <summary>
        /// 创建系列JSON
        /// </summary>
        private JObject CreateSeriesJson(SeriesConfig series)
        {
            var seriesJson = new JObject
            {
                ["type"] = series.Type
            };
            
            if (!string.IsNullOrEmpty(series.Name))
            {
                seriesJson["name"] = series.Name;
            }
            
            if (series.Data != null)
            {
                seriesJson["data"] = JArray.FromObject(series.Data);
            }
            
            if (series.Label != null)
            {
                seriesJson["label"] = JObject.FromObject(series.Label);
            }
            
            if (series.ItemStyle != null)
            {
                seriesJson["itemStyle"] = JObject.FromObject(series.ItemStyle);
            }
            
            if (series.Emphasis != null)
            {
                seriesJson["emphasis"] = JObject.FromObject(series.Emphasis);
            }
            
            if (!string.IsNullOrEmpty(series.Stack))
            {
                seriesJson["stack"] = series.Stack;
            }
            
            if (series.ExtraOptions != null)
            {
                foreach (var kvp in series.ExtraOptions)
                {
                    seriesJson[kvp.Key] = JToken.FromObject(kvp.Value);
                }
            }
            
            return seriesJson;
        }
        
        #endregion
    }
} 