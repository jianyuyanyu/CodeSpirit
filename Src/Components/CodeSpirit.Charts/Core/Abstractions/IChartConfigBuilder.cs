namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表配置构建器接口，定义了构建图表配置的抽象能力
/// </summary>
public interface IChartConfigBuilder
{
    /// <summary>
    /// 设置图表类型
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithChartType(string chartType);
    
    /// <summary>
    /// 设置图表标题
    /// </summary>
    /// <param name="title">标题</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithTitle(string title);
    
    /// <summary>
    /// 设置图表子标题
    /// </summary>
    /// <param name="subtitle">子标题</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithSubtitle(string subtitle);
    
    /// <summary>
    /// 设置图表数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithData(object data);
    
    /// <summary>
    /// 设置图表数据源
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithDataSource(IChartDataSource dataSource);
    
    /// <summary>
    /// 设置X轴配置
    /// </summary>
    /// <param name="xAxisConfig">X轴配置</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithXAxis(object xAxisConfig);
    
    /// <summary>
    /// 设置Y轴配置
    /// </summary>
    /// <param name="yAxisConfig">Y轴配置</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithYAxis(object yAxisConfig);
    
    /// <summary>
    /// 设置图例配置
    /// </summary>
    /// <param name="legendConfig">图例配置</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithLegend(object legendConfig);
    
    /// <summary>
    /// 设置工具提示配置
    /// </summary>
    /// <param name="tooltipConfig">工具提示配置</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithTooltip(object tooltipConfig);
    
    /// <summary>
    /// 设置系列配置
    /// </summary>
    /// <param name="seriesConfig">系列配置</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithSeries(object seriesConfig);
    
    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="theme">主题</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithTheme(string theme);
    
    /// <summary>
    /// 设置自定义选项
    /// </summary>
    /// <param name="options">自定义选项</param>
    /// <returns>构建器实例</returns>
    IChartConfigBuilder WithOptions(object options);
    
    /// <summary>
    /// 构建图表配置
    /// </summary>
    /// <returns>图表配置</returns>
    Task<object> BuildAsync();
}