using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Providers.ECharts;

/// <summary>
/// ECharts 图表渲染器
/// </summary>
public class EChartsRenderer : IChartRenderer
{
    private readonly ILogger<EChartsRenderer> _logger;
    private readonly IChartThemeManager _themeManager;

    public EChartsRenderer(ILogger<EChartsRenderer> logger, IChartThemeManager themeManager)
    {
        _logger = logger;
        _themeManager = themeManager;
    }

    /// <inheritdoc />
    public string ProviderName => "echarts";

    /// <inheritdoc />
    public Task<object> GenerateRenderConfigAsync(object chartConfig, object? options = null)
    {
        try
        {
            // 处理渲染配置
            var renderConfig = new Dictionary<string, object>();
            
            // 复制图表配置
            if (chartConfig is Dictionary<string, object> configDict)
            {
                foreach (var kvp in configDict)
                {
                    renderConfig[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                renderConfig["config"] = chartConfig;
            }
            
            // 应用渲染选项
            if (options is Dictionary<string, object> optionsDict)
            {
                // 应用渲染器特定选项
                if (optionsDict.TryGetValue("renderer", out var rendererObj) && rendererObj is string renderer)
                {
                    renderConfig["renderer"] = renderer;
                }
                
                // 应用宽度和高度
                if (optionsDict.TryGetValue("width", out var widthObj) && widthObj is int width)
                {
                    renderConfig["width"] = width;
                }
                
                if (optionsDict.TryGetValue("height", out var heightObj) && heightObj is int height)
                {
                    renderConfig["height"] = height;
                }
                
                // 应用其他选项
                foreach (var kvp in optionsDict)
                {
                    if (kvp.Key != "renderer" && kvp.Key != "width" && kvp.Key != "height")
                    {
                        renderConfig[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return Task.FromResult<object>(renderConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating render config for ECharts");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<object> GenerateAmisConfigAsync(object chartConfig, object? options = null)
    {
        try
        {
            // 创建 Amis 图表组件配置
            var amisConfig = new Dictionary<string, object>
            {
                ["type"] = "chart",
                ["config"] = chartConfig
            };
            
            // 应用 Amis 配置选项
            if (options is Dictionary<string, object> optionsDict)
            {
                // 应用高度
                if (optionsDict.TryGetValue("height", out var heightObj))
                {
                    amisConfig["height"] = heightObj;
                }
                
                // 应用宽度
                if (optionsDict.TryGetValue("width", out var widthObj))
                {
                    amisConfig["width"] = widthObj;
                }
                
                // 应用数据源
                if (optionsDict.TryGetValue("dataSource", out var dataSourceObj))
                {
                    amisConfig["source"] = dataSourceObj;
                }
                
                // 应用刷新间隔
                if (optionsDict.TryGetValue("refreshInterval", out var refreshIntervalObj))
                {
                    amisConfig["interval"] = refreshIntervalObj;
                }
                
                // 应用加载中文本
                if (optionsDict.TryGetValue("loadingText", out var loadingTextObj))
                {
                    amisConfig["loadingText"] = loadingTextObj;
                }
                
                // 应用其他 Amis 特定选项
                if (optionsDict.TryGetValue("amisOptions", out var amisOptionsObj) && 
                    amisOptionsObj is Dictionary<string, object> amisOptions)
                {
                    foreach (var kvp in amisOptions)
                    {
                        amisConfig[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return Task.FromResult<object>(amisConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Amis config for ECharts");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object> ApplyThemeAsync(object chartConfig, object theme)
    {
        try
        {
            // 处理主题应用
            if (theme is string themeName)
            {
                // 从主题管理器获取主题配置
                var themeConfig = await _themeManager.GetThemeConfigAsync(themeName, ProviderName);
                return await ApplyThemeConfigAsync(chartConfig, themeConfig);
            }
            else
            {
                // 直接应用主题配置
                return await ApplyThemeConfigAsync(chartConfig, theme);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying theme to ECharts config");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePreviewImageAsync(object chartConfig, object? options = null)
    {
        try
        {
            // 生成预览图
            // 注意：这通常需要服务器端渲染或使用无头浏览器
            // 这里只是一个占位实现
            _logger.LogWarning("GeneratePreviewImageAsync is not fully implemented for ECharts");
            
            // 返回一个空的图片数据
            return Task.FromResult(Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview image for ECharts");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<object> GetResponsiveConfigAsync(object chartConfig, (int Width, int Height) containerSize)
    {
        try
        {
            // 处理响应式配置
            var responsiveConfig = new Dictionary<string, object>();
            
            // 复制图表配置
            if (chartConfig is Dictionary<string, object> configDict)
            {
                foreach (var kvp in configDict)
                {
                    responsiveConfig[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                responsiveConfig["config"] = chartConfig;
            }
            
            // 应用容器尺寸
            responsiveConfig["width"] = containerSize.Width;
            responsiveConfig["height"] = containerSize.Height;
            
            // 调整图表配置以适应容器尺寸
            AdjustConfigForContainerSize(responsiveConfig, containerSize);
            
            return Task.FromResult<object>(responsiveConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating responsive config for ECharts");
            throw;
        }
    }

    #region Private Methods

    private Task<object> ApplyThemeConfigAsync(object chartConfig, object themeConfig)
    {
        // 应用主题配置到图表配置
        if (chartConfig is Dictionary<string, object> configDict && 
            themeConfig is Dictionary<string, object> themeDict)
        {
            // 深度合并主题配置到图表配置
            MergeThemeConfig(configDict, themeDict);
            return Task.FromResult<object>(configDict);
        }
        
        // 如果不是字典类型，返回原始配置
        return Task.FromResult(chartConfig);
    }

    private void MergeThemeConfig(Dictionary<string, object> target, Dictionary<string, object> source)
    {
        foreach (var kvp in source)
        {
            if (kvp.Value is Dictionary<string, object> sourceDict && 
                target.TryGetValue(kvp.Key, out var targetValue) && 
                targetValue is Dictionary<string, object> targetDict)
            {
                // 递归合并嵌套字典
                MergeThemeConfig(targetDict, sourceDict);
            }
            else
            {
                // 直接替换或添加值
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    private void AdjustConfigForContainerSize(Dictionary<string, object> config, (int Width, int Height) containerSize)
    {
        // 根据容器尺寸调整图表配置
        // 例如，调整字体大小、图例位置等
        
        // 调整网格
        if (config.TryGetValue("grid", out var gridObj) && gridObj is Dictionary<string, object> grid)
        {
            // 根据容器尺寸调整网格
            if (containerSize.Width < 600)
            {
                grid["left"] = "5%";
                grid["right"] = "5%";
            }
            else
            {
                grid["left"] = "10%";
                grid["right"] = "10%";
            }
        }
        else
        {
            // 创建网格配置
            config["grid"] = new Dictionary<string, object>
            {
                ["left"] = containerSize.Width < 600 ? "5%" : "10%",
                ["right"] = containerSize.Width < 600 ? "5%" : "10%",
                ["top"] = containerSize.Height < 400 ? "10%" : "15%",
                ["bottom"] = containerSize.Height < 400 ? "10%" : "15%",
                ["containLabel"] = true
            };
        }
        
        // 调整标题
        if (config.TryGetValue("title", out var titleObj) && titleObj is Dictionary<string, object> title)
        {
            // 根据容器尺寸调整标题
            if (containerSize.Width < 600)
            {
                title["textStyle"] = new Dictionary<string, object>
                {
                    ["fontSize"] = 14
                };
            }
            else
            {
                title["textStyle"] = new Dictionary<string, object>
                {
                    ["fontSize"] = 18
                };
            }
        }
        
        // 调整图例
        if (config.TryGetValue("legend", out var legendObj) && legendObj is Dictionary<string, object> legend)
        {
            // 根据容器尺寸调整图例
            if (containerSize.Width < 600)
            {
                legend["orient"] = "horizontal";
                legend["top"] = "bottom";
            }
            else
            {
                legend["orient"] = "vertical";
                legend["right"] = "10%";
                legend["top"] = "center";
            }
        }
    }

    #endregion
}