using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Core.Services;

/// <summary>
/// 图表服务的默认实现
/// </summary>
public class ChartService : IChartService
{
    private readonly ILogger<ChartService> _logger;
    private readonly IDictionary<string, IChartProvider> _providers;
    private readonly IDataProcessor _dataProcessor;
    private readonly IChartRecommender _recommender;
    private readonly string _defaultProviderName;

    public ChartService(
        ILogger<ChartService> logger,
        IEnumerable<IChartProvider> providers,
        IDataProcessor dataProcessor,
        IChartRecommender recommender,
        string defaultProviderName = "echarts")
    {
        _logger = logger;
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _dataProcessor = dataProcessor;
        _recommender = recommender;
        _defaultProviderName = defaultProviderName;

        if (!_providers.ContainsKey(_defaultProviderName))
        {
            throw new ArgumentException($"Default provider '{_defaultProviderName}' not found in available providers.");
        }
    }

    /// <inheritdoc />
    public IEnumerable<IChartProvider> GetAvailableProviders()
    {
        return _providers.Values;
    }

    /// <inheritdoc />
    public IChartProvider GetProvider(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new KeyNotFoundException($"Provider '{providerName}' not found.");
        }
        return provider;
    }

    /// <inheritdoc />
    public IChartProvider GetDefaultProvider()
    {
        return GetProvider(_defaultProviderName);
    }

    /// <inheritdoc />
    public async Task<object> CreateChartConfigAsync(string providerName, string chartType, object data, object? options = null)
    {
        try
        {
            var provider = GetProvider(providerName);
            
            // 验证图表类型是否支持
            if (!provider.SupportsChartType(chartType))
            {
                throw new ArgumentException($"Chart type '{chartType}' is not supported by provider '{providerName}'.");
            }

            // 处理数据
            var processedData = await _dataProcessor.ProcessDataAsync(data, options);
            
            // 验证数据是否适合该图表类型
            var (isValid, errorMessage) = await _dataProcessor.ValidateDataForChartTypeAsync(processedData, chartType);
            if (!isValid)
            {
                // 查看是否是空数据集错误（支持中英文错误消息）
                if (errorMessage?.Contains("empty") == true || 
                    errorMessage?.Contains("为空") == true ||
                    errorMessage?.Contains("集合为空") == true)
                {
                    _logger.LogWarning("尝试使用空数据集创建图表: {ChartType}, {ErrorMessage}", chartType, errorMessage);
                    
                    // 为空数据集生成一个带有提示消息的默认图表配置
                    return GenerateEmptyChartConfig(chartType, errorMessage);
                }
                
                throw new InvalidOperationException($"Data validation failed for chart type '{chartType}': {errorMessage}");
            }

            // 转换数据为图表类型所需的格式
            var transformedData = await _dataProcessor.TransformForChartTypeAsync(processedData, chartType, options);
            
            // 生成图表配置
            return provider.GenerateChartConfig(chartType, transformedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chart config for provider '{ProviderName}' and chart type '{ChartType}'", 
                providerName, chartType);
            throw;
        }
    }

    /// <summary>
    /// 为空数据集生成默认图表配置
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="message">提示消息</param>
    /// <returns>默认图表配置</returns>
    private object GenerateEmptyChartConfig(string chartType, string message)
    {
        _logger.LogInformation("为图表类型 '{ChartType}' 生成空数据配置", chartType);
        
        // 基于图表类型创建不同的空图表配置
        var baseConfig = new
        {
            title = new
            {
                text = "暂无数据",
                subtext = message ?? "请稍后再试或修改查询条件",
                left = "center",
                top = "center",
                textStyle = new
                {
                    fontSize = 16,
                    color = "#999"
                },
                subtextStyle = new
                {
                    fontSize = 12,
                    color = "#ccc"
                }
            },
            tooltip = new { },
            legend = new
            {
                data = new string[] { }
            },
            graphic = new[]
            {
                new
                {
                    type = "text",
                    left = "center",
                    top = "middle",
                    style = new
                    {
                        text = "📊",
                        fontSize = 48,
                        fill = "#ddd"
                    }
                }
            }
        };

        switch (chartType.ToLowerInvariant())
        {
            case "bar":
            case "column":
                return new
                {
                    baseConfig.title,
                    baseConfig.tooltip,
                    baseConfig.legend,
                    baseConfig.graphic,
                    xAxis = new 
                    { 
                        type = "category", 
                        data = new string[] { "暂无数据" },
                        axisLabel = new { show = false },
                        axisTick = new { show = false },
                        axisLine = new { show = false }
                    },
                    yAxis = new 
                    { 
                        type = "value",
                        axisLabel = new { show = false },
                        axisTick = new { show = false },
                        axisLine = new { show = false },
                        splitLine = new { show = false }
                    },
                    series = new[]
                    {
                        new 
                        { 
                            type = "bar", 
                            data = new int[] { 0 },
                            itemStyle = new
                            {
                                color = "transparent"
                            }
                        }
                    }
                };
                
            case "line":
            case "area":
                return new
                {
                    baseConfig.title,
                    baseConfig.tooltip,
                    baseConfig.legend,
                    baseConfig.graphic,
                    xAxis = new 
                    { 
                        type = "category", 
                        data = new string[] { "暂无数据" },
                        axisLabel = new { show = false },
                        axisTick = new { show = false },
                        axisLine = new { show = false }
                    },
                    yAxis = new 
                    { 
                        type = "value",
                        axisLabel = new { show = false },
                        axisTick = new { show = false },
                        axisLine = new { show = false },
                        splitLine = new { show = false }
                    },
                    series = new[]
                    {
                        new 
                        { 
                            type = "line", 
                            data = new int[] { 0 },
                            lineStyle = new
                            {
                                color = "transparent"
                            },
                            itemStyle = new
                            {
                                color = "transparent"
                            }
                        }
                    }
                };
                
            case "pie":
            case "donut":
                return new
                {
                    baseConfig.title,
                    baseConfig.tooltip,
                    baseConfig.legend,
                    baseConfig.graphic,
                    series = new[]
                    {
                        new 
                        { 
                            type = "pie", 
                            data = new[]
                            { 
                                new { name = "暂无数据", value = 1 }
                            },
                            radius = new[] { "0%", "0%" }, // 隐藏饼图
                            label = new { show = false },
                            labelLine = new { show = false }
                        }
                    }
                };
                
            case "gauge":
                return new
                {
                    baseConfig.title,
                    baseConfig.tooltip,
                    baseConfig.graphic,
                    series = new[]
                    {
                        new
                        {
                            type = "gauge",
                            data = new[]
                            {
                                new { value = 0, name = "暂无数据" }
                            },
                            detail = new { show = false },
                            pointer = new { show = false },
                            axisTick = new { show = false },
                            axisLabel = new { show = false },
                            splitLine = new { show = false }
                        }
                    }
                };
                
            default:
                return new
                {
                    baseConfig.title,
                    baseConfig.tooltip,
                    baseConfig.legend,
                    baseConfig.graphic,
                    series = new object[] { }
                };
        }
    }

    /// <inheritdoc />
    public Task<object> CreateChartConfigAsync(string chartType, object data, object? options = null)
    {
        return CreateChartConfigAsync(_defaultProviderName, chartType, data, options);
    }

    /// <inheritdoc />
    public async Task<object> GetAmisConfigAsync(object chartConfig, string providerName, object? options = null)
    {
        try
        {
            var provider = GetProvider(providerName);
            if (provider is IChartRenderer renderer)
            {
                return await renderer.GenerateAmisConfigAsync(chartConfig, options);
            }
            throw new InvalidOperationException($"Provider '{providerName}' does not support Amis configuration generation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis config for provider '{ProviderName}'", providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object> ProcessDataSourceAsync(object dataSource, object? options = null)
    {
        try
        {
            return await _dataProcessor.ProcessDataAsync(dataSource, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data source");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> RecommendChartTypesAsync(object data, string? providerName = null)
    {
        try
        {
            var provider = providerName != null ? GetProvider(providerName) : GetDefaultProvider();
            var recommendations = await _recommender.RecommendChartTypesAsync(data, provider.Name);
            return recommendations.Select(r => r.ChartType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending chart types");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportChartDataAsync(object chartConfig, string format, object? options = null)
    {
        try
        {
            return await _dataProcessor.ExportDataAsync(chartConfig, format, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chart data to format '{Format}'", format);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportChartImageAsync(object chartConfig, string providerName, object? options = null)
    {
        try
        {
            var provider = GetProvider(providerName);
            if (provider is IChartRenderer renderer)
            {
                return await renderer.GeneratePreviewImageAsync(chartConfig, options);
            }
            throw new InvalidOperationException($"Provider '{providerName}' does not support image generation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chart image for provider '{ProviderName}'", providerName);
            throw;
        }
    }
}