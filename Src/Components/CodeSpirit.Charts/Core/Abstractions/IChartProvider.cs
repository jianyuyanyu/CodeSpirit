namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表提供者接口，定义了图表库的抽象能力
/// </summary>
public interface IChartProvider
{
    /// <summary>
    /// 获取提供者名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 获取提供者支持的图表类型
    /// </summary>
    IEnumerable<string> SupportedChartTypes { get; }
    
    /// <summary>
    /// 检查是否支持指定的图表类型
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <returns>是否支持</returns>
    bool SupportsChartType(string chartType);
    
    /// <summary>
    /// 生成图表配置
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>图表配置对象</returns>
    object GenerateChartConfig(string chartType, object data, object? options = null);
}