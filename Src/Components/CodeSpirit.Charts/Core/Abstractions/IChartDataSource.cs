namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 图表数据源接口，定义了数据源的抽象能力
/// </summary>
public interface IChartDataSource
{
    /// <summary>
    /// 获取数据源类型
    /// </summary>
    string SourceType { get; }
    
    /// <summary>
    /// 获取数据源名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 获取数据源描述
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// 获取数据源配置
    /// </summary>
    object Configuration { get; }
    
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <param name="parameters">查询参数</param>
    /// <returns>数据</returns>
    Task<object> GetDataAsync(object? parameters = null);
    
    /// <summary>
    /// 验证数据源配置
    /// </summary>
    /// <returns>验证结果</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateAsync();
    
    /// <summary>
    /// 获取数据源的元数据
    /// </summary>
    /// <returns>元数据</returns>
    Task<object> GetMetadataAsync();
    
    /// <summary>
    /// 获取数据源的架构
    /// </summary>
    /// <returns>架构</returns>
    Task<object> GetSchemaAsync();
}