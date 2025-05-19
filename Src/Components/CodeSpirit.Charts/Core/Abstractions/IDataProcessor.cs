using System.Data;

namespace CodeSpirit.Charts.Core.Abstractions;

/// <summary>
/// 数据处理器接口，定义了数据处理和转换的能力
/// </summary>
public interface IDataProcessor
{
    /// <summary>
    /// 处理原始数据并转换为标准格式
    /// </summary>
    /// <param name="rawData">原始数据</param>
    /// <param name="options">处理选项</param>
    /// <returns>处理后的数据</returns>
    Task<object> ProcessDataAsync(object rawData, object? options = null);
    
    /// <summary>
    /// 将数据转换为指定图表类型所需的格式
    /// </summary>
    /// <param name="data">标准格式数据</param>
    /// <param name="chartType">目标图表类型</param>
    /// <param name="options">转换选项</param>
    /// <returns>转换后的数据</returns>
    Task<object> TransformForChartTypeAsync(object data, string chartType, object? options = null);
    
    /// <summary>
    /// 验证数据是否适合指定的图表类型
    /// </summary>
    /// <param name="data">要验证的数据</param>
    /// <param name="chartType">图表类型</param>
    /// <returns>验证结果</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateDataForChartTypeAsync(object data, string chartType);
    
    /// <summary>
    /// 聚合或转换数据
    /// </summary>
    /// <param name="data">源数据</param>
    /// <param name="aggregationType">聚合类型</param>
    /// <param name="options">聚合选项</param>
    /// <returns>聚合后的数据</returns>
    Task<object> AggregateDataAsync(object data, string aggregationType, object? options = null);
    
    /// <summary>
    /// 将处理后的数据导出为指定格式
    /// </summary>
    /// <param name="data">处理后的数据</param>
    /// <param name="format">导出格式</param>
    /// <param name="options">导出选项</param>
    /// <returns>导出的数据</returns>
    Task<byte[]> ExportDataAsync(object data, string format, object? options = null);
}