using System.Data;
using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Core.Services;

/// <summary>
/// 数据处理器的默认实现
/// </summary>
public class DataProcessor : IDataProcessor
{
    private readonly ILogger<DataProcessor> _logger;
    private readonly Dictionary<string, Func<object, object?, Task<object>>> _formatProcessors;

    public DataProcessor(ILogger<DataProcessor> logger)
    {
        _logger = logger;
        _formatProcessors = new Dictionary<string, Func<object, object?, Task<object>>>
        {
            { "csv", ExportToCsvAsync },
            { "excel", ExportToExcelAsync },
            { "json", ExportToJsonAsync }
        };
    }

    /// <inheritdoc />
    public async Task<object> ProcessDataAsync(object rawData, object? options = null)
    {
        try
        {
            // 验证输入参数
            if (rawData == null)
            {
                throw new ArgumentNullException(nameof(rawData), "Raw data cannot be null");
            }

            // 根据数据类型进行不同的处理
            return rawData switch
            {
                DataTable dt => await ProcessDataTableAsync(dt, options),
                IEnumerable<object> collection => await ProcessCollectionAsync(collection, options),
                string jsonString => await ProcessJsonAsync(jsonString, options),
                // 添加对匿名类型的支持
                _ when rawData.GetType().Name.Contains("AnonymousType") => await ProcessAnonymousTypeAsync(rawData, options),
                _ => throw new ArgumentException($"Unsupported data type: {rawData.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object> TransformForChartTypeAsync(object data, string chartType, object? options = null)
    {
        try
        {
            // 验证输入参数
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            }

            if (string.IsNullOrWhiteSpace(chartType))
            {
                throw new ArgumentNullException(nameof(chartType), "Chart type cannot be null or empty");
            }

            // 根据图表类型进行数据转换
            return chartType.ToLowerInvariant() switch
            {
                "line" or "area" => await TransformForLineChartAsync(data, options),
                "bar" or "column" => await TransformForBarChartAsync(data, options),
                "pie" or "donut" => await TransformForPieChartAsync(data, options),
                "scatter" => await TransformForScatterChartAsync(data, options),
                "radar" => await TransformForRadarChartAsync(data, options),
                "tree" or "treemap" => await TransformForTreeChartAsync(data, options),
                "sankey" => await TransformForSankeyChartAsync(data, options),
                "heatmap" => await TransformForHeatmapChartAsync(data, options),
                _ => throw new ArgumentException($"Unsupported chart type: {chartType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming data for chart type '{ChartType}'", chartType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateDataForChartTypeAsync(object data, string chartType)
    {
        // 检查空值
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        // 检查图表类型
        if (string.IsNullOrWhiteSpace(chartType))
        {
            throw new ArgumentException("Chart type cannot be empty", nameof(chartType));
        }

        try
        {
            // 根据图表类型验证数据结构
            var validator = GetValidatorForChartType(chartType);
            return await validator(data);
        }
        catch (ArgumentException)
        {
            // 重新抛出参数异常
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data for chart type '{ChartType}'", chartType);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<object> AggregateDataAsync(object data, string aggregationType, object? options = null)
    {
        try
        {
            // 验证输入参数
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            }

            if (string.IsNullOrWhiteSpace(aggregationType))
            {
                throw new ArgumentNullException(nameof(aggregationType), "Aggregation type cannot be null or empty");
            }

            // 如果数据是空集合，返回空结果
            if (data is IEnumerable<object> collection && !collection.Any())
            {
                return Array.Empty<object>();
            }

            // 根据聚合类型进行数据聚合
            return aggregationType.ToLowerInvariant() switch
            {
                "sum" => await AggregateSum(data, options),
                "average" => await AggregateAverage(data, options),
                "count" => await AggregateCount(data, options),
                "distinct" => await AggregateDistinct(data, options),
                "max" => await AggregateMax(data, options),
                "min" => await AggregateMin(data, options),
                _ => throw new ArgumentException($"Unsupported aggregation type: {aggregationType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating data with type '{AggregationType}'", aggregationType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportDataAsync(object data, string format, object? options = null)
    {
        try
        {
            // 验证输入参数
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Data cannot be null");
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format), "Export format cannot be null or empty");
            }

            // 如果数据是空集合，返回空数据的导出结果
            if (data is IEnumerable<object> collection && !collection.Any())
            {
                // 创建一个包含表头的空数据导出
                var emptyResult = new byte[] { 0xEF, 0xBB, 0xBF }; // UTF-8 BOM
                return emptyResult;
            }

            // 验证导出格式
            if (!_formatProcessors.TryGetValue(format.ToLowerInvariant(), out var processor))
            {
                throw new ArgumentException($"Unsupported export format: {format}", nameof(format));
            }

            // 处理数据并导出
            var processedData = await processor(data, options);
            return processedData switch
            {
                byte[] bytes => bytes,
                string str => System.Text.Encoding.UTF8.GetBytes(str),
                _ => throw new InvalidOperationException($"Unexpected export result type: {processedData.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to format '{Format}'", format);
            throw;
        }
    }

    #region Private Methods

    private async Task<object> ProcessDataTableAsync(DataTable dataTable, object? options)
    {
        // 实现 DataTable 处理逻辑
        return await Task.FromResult(new Dictionary<string, object>
        {
            ["Columns"] = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
            ["Rows"] = dataTable.Rows.Cast<DataRow>().Select(r => r.ItemArray).ToList()
        });
    }

    private async Task<object> ProcessCollectionAsync(IEnumerable<object> collection, object? options)
    {
        // 实现集合处理逻辑
        return await Task.FromResult(collection.ToList());
    }

    private async Task<object> ProcessJsonAsync(string jsonString, object? options)
    {
        // 实现 JSON 处理逻辑
        return await Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<object>(jsonString));
    }

    private async Task<object> ProcessAnonymousTypeAsync(object anonymousType, object? options)
    {
        _logger.LogInformation("处理匿名类型数据: {Type}", anonymousType.GetType().Name);

        try
        {
            // 获取属性信息
            var properties = anonymousType.GetType().GetProperties();

            // 检查是否是集合类型的属性
            var collectionProperty = properties.FirstOrDefault(p =>
                typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) &&
                p.PropertyType != typeof(string));

            // 如果找到集合属性，直接处理该集合
            if (collectionProperty != null)
            {
                var collection = collectionProperty.GetValue(anonymousType) as IEnumerable<object>;
                if (collection != null)
                {
                    _logger.LogInformation("在匿名类型中找到集合属性 {PropertyName}，直接处理集合", collectionProperty.Name);
                    return await ProcessCollectionAsync(collection, options);
                }
            }

            // 普通匿名对象转换为图表数据格式
            // 尝试猜测类别值字段
            var categoryProp = properties.FirstOrDefault(p =>
                p.Name.Contains("category", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("name", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("label", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("title", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("key", StringComparison.OrdinalIgnoreCase));

            var valueProp = properties.FirstOrDefault(p =>
                p.Name.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("count", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("sum", StringComparison.OrdinalIgnoreCase));

            // 如果识别到类别和值属性，自动转换为图表数据格式
            if (categoryProp != null && valueProp != null)
            {
                _logger.LogInformation("检测到类别字段 {CategoryField} 和值字段 {ValueField}，自动转换为图表数据",
                    categoryProp.Name, valueProp.Name);

                // 转换为图表数据集合格式
                return new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["category"] = categoryProp.GetValue(anonymousType) ?? "未知类别",
                        ["value"] = valueProp.GetValue(anonymousType) ?? 0
                    }
                };
            }

            // 如果找不到特定结构，将所有属性转换为单独的图表数据项
            var result = new List<object>();
            foreach (var prop in properties)
            {
                // 跳过复杂类型
                if (prop.PropertyType.IsClass &&
                    prop.PropertyType != typeof(string) &&
                    !prop.PropertyType.IsPrimitive)
                    continue;

                // 添加每个属性作为一个数据项
                result.Add(new Dictionary<string, object>
                {
                    ["category"] = prop.Name,
                    ["value"] = prop.GetValue(anonymousType) ?? 0
                });
            }

            if (result.Any())
            {
                _logger.LogInformation("将匿名类型的 {Count} 个属性转换为图表数据项", result.Count);
                return result;
            }

            // 作为后备方案，返回字典形式
            var dictResult = new Dictionary<string, object>();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(anonymousType);
                if (value != null)
                {
                    dictResult[prop.Name] = value;
                }
            }

            _logger.LogInformation("成功将匿名类型转换为字典，包含 {Count} 个属性", dictResult.Count);
            return await Task.FromResult(dictResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理匿名类型时出错");
            throw;
        }
    }

    private Func<object, Task<(bool, string?)>> GetValidatorForChartType(string chartType)
    {
        if (string.IsNullOrWhiteSpace(chartType))
        {
            throw new ArgumentException("Chart type cannot be empty", nameof(chartType));
        }

        var normalizedChartType = chartType.ToLowerInvariant();
        
        // 检查是否是支持的图表类型
        var supportedTypes = new[]
        {
            "line", "area", "bar", "column", "pie", "donut",
            "scatter", "radar", "tree", "treemap", "sankey", "heatmap"
        };

        if (!supportedTypes.Contains(normalizedChartType))
        {
            throw new ArgumentException($"Unsupported chart type: {chartType}", nameof(chartType));
        }

        return normalizedChartType switch
        {
            "line" or "area" => ValidateLineChartData,
            "bar" or "column" => ValidateBarChartData,
            "pie" or "donut" => ValidatePieChartData,
            "scatter" => ValidateScatterChartData,
            "radar" => ValidateRadarChartData,
            "tree" or "treemap" => ValidateTreeChartData,
            "sankey" => ValidateSankeyChartData,
            "heatmap" => ValidateHeatmapChartData,
            _ => throw new ArgumentException($"Unsupported chart type: {chartType}", nameof(chartType))
        };
    }

    private async Task<(bool, string?)> ValidateLineChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    return (false, "Data collection is empty");
                }
                return (true, null);
            }

            return (false, "Data must be a collection");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating line chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateBarChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "数据不能为空。请确保您的查询返回有效数据。");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    _logger.LogWarning("条形图数据集合为空");
                    return (false, "数据集合为空。请确保您的查询返回至少一条记录。");
                }
                return (true, null);
            }

            return (false, "数据必须是对象集合");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证条形图数据时出错");
            return (false, $"验证条形图数据时出错: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidatePieChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    return (false, "Data collection is empty");
                }
                return (true, null);
            }

            return (false, "Data must be a collection");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating pie chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateScatterChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    return (false, "Data collection is empty");
                }
                return (true, null);
            }

            return (false, "Data must be a collection");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating scatter chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateRadarChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    return (false, "Data collection is empty");
                }
                return (true, null);
            }

            return (false, "Data must be a collection");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating radar chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateTreeChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IDictionary<string, object>)
            {
                return (true, null);
            }

            return (false, "Data must be a hierarchical structure");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating tree chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateSankeyChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IDictionary<string, object> dict)
            {
                if (!dict.ContainsKey("nodes") || !dict.ContainsKey("links"))
                {
                    return (false, "Data must contain 'nodes' and 'links' collections");
                }
                return (true, null);
            }

            return (false, "Data must be a dictionary containing nodes and links");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating sankey chart data: {ex.Message}");
        }
    }

    private async Task<(bool, string?)> ValidateHeatmapChartData(object data)
    {
        // 基本数据验证
        if (data == null)
        {
            return (false, "Data cannot be null");
        }

        try
        {
            // 验证数据结构
            if (data is IEnumerable<object> collection)
            {
                var list = collection.ToList();
                if (!list.Any())
                {
                    return (false, "Data collection is empty");
                }
                return (true, null);
            }

            return (false, "Data must be a collection");
        }
        catch (Exception ex)
        {
            return (false, $"Error validating heatmap chart data: {ex.Message}");
        }
    }

    #region Data Transformation Methods

    private Task<object> TransformForLineChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForBarChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForPieChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForScatterChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForRadarChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForTreeChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForSankeyChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    private Task<object> TransformForHeatmapChartAsync(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体转换逻辑

    #endregion

    #region Data Aggregation Methods

    private Task<object> AggregateSum(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    private Task<object> AggregateAverage(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    private Task<object> AggregateCount(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    private Task<object> AggregateDistinct(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    private Task<object> AggregateMax(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    private Task<object> AggregateMin(object data, object? options) =>
        Task.FromResult<object>(data); // 实现具体聚合逻辑

    #endregion

    #region Data Export Methods

    private Task<object> ExportToCsvAsync(object data, object? options) =>
        Task.FromResult<object>(""); // 实现 CSV 导出逻辑

    private Task<object> ExportToExcelAsync(object data, object? options) =>
        Task.FromResult<object>(Array.Empty<byte>()); // 实现 Excel 导出逻辑

    private Task<object> ExportToJsonAsync(object data, object? options) =>
        Task.FromResult<object>(""); // 实现 JSON 导出逻辑

    #endregion

    #endregion
}