using System.Data;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

/// <summary>
/// 数据处理器边缘情况测试
/// </summary>
public class DataProcessorEdgeCasesTests
{
    private readonly Mock<ILogger<DataProcessor>> _loggerMock;
    private readonly DataProcessor _processor;

    public DataProcessorEdgeCasesTests()
    {
        _loggerMock = new Mock<ILogger<DataProcessor>>();
        _processor = new DataProcessor(_loggerMock.Object);
    }

    [Fact]
    public async Task ProcessDataAsync_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyCollection = Array.Empty<object>();

        // Act
        var result = await _processor.ProcessDataAsync(emptyCollection);

        // Assert
        Assert.NotNull(result);
        var resultList = Assert.IsType<List<object>>(result);
        Assert.Empty(resultList);
    }

    [Fact]
    public async Task ProcessDataAsync_WithEmptyDataTable_ShouldReturnEmptyData()
    {
        // Arrange
        var emptyDataTable = new DataTable();
        emptyDataTable.Columns.Add("Column1", typeof(string));
        emptyDataTable.Columns.Add("Column2", typeof(int));

        // Act
        var result = await _processor.ProcessDataAsync(emptyDataTable);

        // Assert
        Assert.NotNull(result);
        var resultDict = Assert.IsType<Dictionary<string, object>>(result);
        var columns = Assert.IsType<List<string>>(resultDict["Columns"]);
        var rows = Assert.IsType<List<object[]>>(resultDict["Rows"]);
        
        Assert.Equal(2, columns.Count);
        Assert.Empty(rows);
    }

    [Fact]
    public async Task ProcessDataAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? data = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.ProcessDataAsync(data!));
    }

    [Fact]
    public async Task TransformForChartTypeAsync_WithEmptyData_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyData = Array.Empty<object>();
        var chartType = "line";

        // Act
        var result = await _processor.TransformForChartTypeAsync(emptyData, chartType);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task TransformForChartTypeAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? data = null;
        var chartType = "line";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.TransformForChartTypeAsync(data!, chartType));
    }

    [Fact]
    public async Task TransformForChartTypeAsync_WithNullChartType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        string? chartType = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.TransformForChartTypeAsync(data, chartType!));
    }

    [Fact]
    public async Task ValidateDataForChartTypeAsync_WithEmptyChartType_ShouldThrowArgumentException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var chartType = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.ValidateDataForChartTypeAsync(data, chartType));
    }

    [Fact]
    public async Task ValidateDataForChartTypeAsync_WithWhitespaceChartType_ShouldThrowArgumentException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var chartType = "   ";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.ValidateDataForChartTypeAsync(data, chartType));
    }

    [Fact]
    public async Task AggregateDataAsync_WithEmptyCollection_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyCollection = Array.Empty<object>();
        var aggregationType = "sum";

        // Act
        var result = await _processor.AggregateDataAsync(emptyCollection, aggregationType);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AggregateDataAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? data = null;
        var aggregationType = "sum";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.AggregateDataAsync(data!, aggregationType));
    }

    [Fact]
    public async Task AggregateDataAsync_WithNullAggregationType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        string? aggregationType = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.AggregateDataAsync(data, aggregationType!));
    }

    [Fact]
    public async Task ExportDataAsync_WithEmptyData_ShouldReturnNonEmptyBytes()
    {
        // Arrange
        var emptyData = Array.Empty<object>();
        var format = "csv";

        // Act
        var result = await _processor.ExportDataAsync(emptyData, format);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExportDataAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        object? data = null;
        var format = "csv";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.ExportDataAsync(data!, format));
    }

    [Fact]
    public async Task ExportDataAsync_WithNullFormat_ShouldThrowArgumentNullException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        string? format = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _processor.ExportDataAsync(data, format!));
    }
}