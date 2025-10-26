using System.Data;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

public class DataProcessorTests
{
    private readonly Mock<ILogger<DataProcessor>> _loggerMock;
    private readonly DataProcessor _processor;

    public DataProcessorTests()
    {
        _loggerMock = new Mock<ILogger<DataProcessor>>();
        _processor = new DataProcessor(_loggerMock.Object);
    }

    [Fact]
    public async Task ProcessDataAsync_WithDataTable_ShouldReturnProcessedData()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Category", typeof(string));
        dataTable.Columns.Add("Value", typeof(int));
        dataTable.Rows.Add("A", 1);
        dataTable.Rows.Add("B", 2);
        dataTable.Rows.Add("C", 3);

        // Act
        var result = await _processor.ProcessDataAsync(dataTable);

        // Assert
        Assert.NotNull(result);
        var resultDict = Assert.IsType<Dictionary<string, object>>(result);
        var columns = Assert.IsType<List<string>>(resultDict["Columns"]);
        var rows = Assert.IsType<List<object[]>>(resultDict["Rows"]);
        
        Assert.Equal(2, columns.Count);
        Assert.Equal(3, rows.Count);
        Assert.Equal("Category", columns[0]);
        Assert.Equal("Value", columns[1]);
    }

    [Fact]
    public async Task ProcessDataAsync_WithCollection_ShouldReturnProcessedData()
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var result = await _processor.ProcessDataAsync(data);

        // Assert
        Assert.NotNull(result);
        var resultList = Assert.IsType<List<object>>(result);
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task ProcessDataAsync_WithJsonString_ShouldReturnProcessedData()
    {
        // Arrange
        var jsonData = @"[
            { ""Category"": ""A"", ""Value"": 1 },
            { ""Category"": ""B"", ""Value"": 2 },
            { ""Category"": ""C"", ""Value"": 3 }
        ]";

        // Act
        var result = await _processor.ProcessDataAsync(jsonData);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ProcessDataAsync_WithUnsupportedType_ShouldThrowException()
    {
        // Arrange
        var data = 123; // 不支持的数据类型

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _processor.ProcessDataAsync(data));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("bar")]
    [InlineData("pie")]
    public async Task TransformForChartTypeAsync_WithSupportedTypes_ShouldReturnTransformedData(string chartType)
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var result = await _processor.TransformForChartTypeAsync(data, chartType);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task TransformForChartTypeAsync_WithUnsupportedType_ShouldThrowException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var chartType = "unsupported";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.TransformForChartTypeAsync(data, chartType));
    }

    [Theory]
    [InlineData("sum")]
    [InlineData("average")]
    [InlineData("count")]
    [InlineData("distinct")]
    [InlineData("max")]
    [InlineData("min")]
    public async Task AggregateDataAsync_WithValidAggregationType_ShouldReturnAggregatedData(string aggregationType)
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "A", Value = 2 },
            new { Category = "B", Value = 3 }
        };

        // Act
        var result = await _processor.AggregateDataAsync(data, aggregationType);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AggregateDataAsync_WithInvalidAggregationType_ShouldThrowException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var aggregationType = "invalid";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.AggregateDataAsync(data, aggregationType));
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("excel")]
    [InlineData("json")]
    public async Task ExportDataAsync_WithSupportedFormat_ShouldReturnExportedData(string format)
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var result = await _processor.ExportDataAsync(data, format);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<byte[]>(result);
    }

    [Fact]
    public async Task ExportDataAsync_WithUnsupportedFormat_ShouldThrowException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var format = "unsupported";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.ExportDataAsync(data, format));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("bar")]
    [InlineData("pie")]
    public async Task ValidateDataForChartTypeAsync_WithValidData_ShouldReturnSuccess(string chartType)
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var (isValid, errorMessage) = await _processor.ValidateDataForChartTypeAsync(data, chartType);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public async Task ValidateDataForChartTypeAsync_WithInvalidChartType_ShouldThrowException()
    {
        // Arrange
        var data = new[] { new { Value = 1 } };
        var chartType = "invalid";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _processor.ValidateDataForChartTypeAsync(data, chartType));
        
        Assert.Contains("Unsupported chart type", exception.Message);
    }

    [Fact]
    public async Task ValidateDataForChartTypeAsync_WithNullData_ShouldReturnError()
    {
        // Arrange
        object? data = null;
        var chartType = "line";

        // Act
        var (isValid, errorMessage) = await _processor.ValidateDataForChartTypeAsync(data!, chartType);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }
}