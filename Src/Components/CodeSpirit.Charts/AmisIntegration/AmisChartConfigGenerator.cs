using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.AmisIntegration;

/// <summary>
/// Amis 图表配置生成器
/// </summary>
public class AmisChartConfigGenerator
{
    private readonly ILogger<AmisChartConfigGenerator> _logger;
    private readonly IChartService _chartService;

    public AmisChartConfigGenerator(ILogger<AmisChartConfigGenerator> logger, IChartService chartService)
    {
        _logger = logger;
        _chartService = chartService;
    }

    /// <summary>
    /// 生成 Amis 图表配置
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>Amis 图表配置</returns>
    public async Task<object> GenerateAmisChartConfigAsync(string chartType, object data, object? options = null)
    {
        try
        {
            // 获取提供者名称
            string providerName = GetProviderName(options);
            
            // 创建图表配置
            var chartConfig = await _chartService.CreateChartConfigAsync(providerName, chartType, data, options);
            
            // 生成 Amis 配置
            return await _chartService.GetAmisConfigAsync(chartConfig, providerName, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis chart config for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    /// <summary>
    /// 生成 Amis 图表组件配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="options">Amis 配置选项</param>
    /// <returns>Amis 图表组件配置</returns>
    public async Task<object> GenerateAmisComponentConfigAsync(object chartConfig, object? options = null)
    {
        try
        {
            // 获取提供者名称
            string providerName = GetProviderName(options);
            
            // 生成 Amis 配置
            return await _chartService.GetAmisConfigAsync(chartConfig, providerName, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis component config");
            throw;
        }
    }

    /// <summary>
    /// 生成 Amis 图表数据源配置
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="options">配置选项</param>
    /// <returns>Amis 数据源配置</returns>
    public object GenerateAmisDataSourceConfig(object dataSource, object? options = null)
    {
        try
        {
            // 处理不同类型的数据源
            if (dataSource is string url && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                // API 数据源
                return new Dictionary<string, object>
                {
                    ["api"] = url
                };
            }
            else if (dataSource is Dictionary<string, object> apiConfig && apiConfig.ContainsKey("url"))
            {
                // 高级 API 配置
                return new Dictionary<string, object>
                {
                    ["api"] = apiConfig
                };
            }
            else
            {
                // 静态数据
                return new Dictionary<string, object>
                {
                    ["data"] = dataSource
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis data source config");
            throw;
        }
    }

    /// <summary>
    /// 生成完整的 Amis 图表页面配置
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>Amis 页面配置</returns>
    public async Task<object> GenerateAmisPageConfigAsync(string chartType, object data, object? options = null)
    {
        try
        {
            // 生成图表配置
            var chartConfig = await GenerateAmisChartConfigAsync(chartType, data, options);
            
            // 创建页面配置
            var pageConfig = new Dictionary<string, object>
            {
                ["type"] = "page",
                ["title"] = GetPageTitle(options),
                ["body"] = new[]
                {
                    chartConfig
                }
            };
            
            // 应用页面选项
            if (options is Dictionary<string, object> optionsDict && 
                optionsDict.TryGetValue("pageOptions", out var pageOptionsObj) && 
                pageOptionsObj is Dictionary<string, object> pageOptions)
            {
                foreach (var kvp in pageOptions)
                {
                    pageConfig[kvp.Key] = kvp.Value;
                }
            }
            
            return pageConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis page config for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    /// <summary>
    /// 生成 Amis 图表仪表板配置
    /// </summary>
    /// <param name="charts">图表配置列表</param>
    /// <param name="options">配置选项</param>
    /// <returns>Amis 仪表板配置</returns>
    public async Task<object> GenerateAmisDashboardConfigAsync(IEnumerable<object> charts, object? options = null)
    {
        try
        {
            // 处理图表列表
            var chartComponents = new List<object>();
            
            foreach (var chart in charts)
            {
                if (chart is Dictionary<string, object> chartDict)
                {
                    // 如果已经是 Amis 配置，直接添加
                    chartComponents.Add(chartDict);
                }
                else
                {
                    // 否则，生成 Amis 配置
                    var amisConfig = await GenerateAmisComponentConfigAsync(chart, options);
                    chartComponents.Add(amisConfig);
                }
            }
            
            // 创建仪表板配置
            var dashboardConfig = new Dictionary<string, object>
            {
                ["type"] = "page",
                ["title"] = GetDashboardTitle(options),
                ["body"] = new Dictionary<string, object>
                {
                    ["type"] = "grid",
                    ["columns"] = chartComponents.ToArray()
                }
            };
            
            // 应用仪表板选项
            if (options is Dictionary<string, object> optionsDict && 
                optionsDict.TryGetValue("dashboardOptions", out var dashboardOptionsObj) && 
                dashboardOptionsObj is Dictionary<string, object> dashboardOptions)
            {
                foreach (var kvp in dashboardOptions)
                {
                    dashboardConfig[kvp.Key] = kvp.Value;
                }
            }
            
            return dashboardConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis dashboard config");
            throw;
        }
    }

    #region Private Methods

    private string GetProviderName(object? options)
    {
        if (options is Dictionary<string, object> optionsDict && 
            optionsDict.TryGetValue("provider", out var providerObj) && 
            providerObj is string providerName)
        {
            return providerName;
        }
        
        return "echarts"; // 默认使用 ECharts
    }

    private string GetPageTitle(object? options)
    {
        if (options is Dictionary<string, object> optionsDict && 
            optionsDict.TryGetValue("title", out var titleObj) && 
            titleObj is string title)
        {
            return title;
        }
        
        return "图表页面";
    }

    private string GetDashboardTitle(object? options)
    {
        if (options is Dictionary<string, object> optionsDict && 
            optionsDict.TryGetValue("title", out var titleObj) && 
            titleObj is string title)
        {
            return title;
        }
        
        return "图表仪表板";
    }

    #endregion
}