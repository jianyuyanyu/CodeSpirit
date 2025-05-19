namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表服务接口，作为应用程序与图表功能交互的主要入口点
/// </summary>
public interface IChartService
{
    /// <summary>
    /// 获取所有可用的图表提供者
    /// </summary>
    /// <returns>图表提供者列表</returns>
    IEnumerable<IChartProvider> GetAvailableProviders();
    
    /// <summary>
    /// 获取指定名称的图表提供者
    /// </summary>
    /// <param name="providerName">提供者名称</param>
    /// <returns>图表提供者</returns>
    IChartProvider GetProvider(string providerName);
    
    /// <summary>
    /// 获取默认的图表提供者
    /// </summary>
    /// <returns>默认图表提供者</returns>
    IChartProvider GetDefaultProvider();
    
    /// <summary>
    /// 创建图表配置
    /// </summary>
    /// <param name="providerName">提供者名称</param>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>图表配置</returns>
    Task<object> CreateChartConfigAsync(string providerName, string chartType, object data, object? options = null);
    
    /// <summary>
    /// 使用默认提供者创建图表配置
    /// </summary>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>图表配置</returns>
    Task<object> CreateChartConfigAsync(string chartType, object data, object? options = null);
    
    /// <summary>
    /// 获取图表的 Amis 配置
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="options">Amis 配置选项</param>
    /// <returns>Amis 配置对象</returns>
    Task<object> GetAmisConfigAsync(object chartConfig, string providerName, object? options = null);
    
    /// <summary>
    /// 处理数据源
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="options">处理选项</param>
    /// <returns>处理后的数据</returns>
    Task<object> ProcessDataSourceAsync(object dataSource, object? options = null);
    
    /// <summary>
    /// 推荐适合数据的图表类型
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="providerName">提供者名称</param>
    /// <returns>推荐的图表类型列表</returns>
    Task<IEnumerable<string>> RecommendChartTypesAsync(object data, string? providerName = null);
    
    /// <summary>
    /// 导出图表数据
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="format">导出格式</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的数据</returns>
    Task<byte[]> ExportChartDataAsync(object chartConfig, string format, object? options = null);
    
    /// <summary>
    /// 导出图表为图片
    /// </summary>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="options">导出选项</param>
    /// <returns>图片数据</returns>
    Task<byte[]> ExportChartImageAsync(object chartConfig, string providerName, object? options = null);
}