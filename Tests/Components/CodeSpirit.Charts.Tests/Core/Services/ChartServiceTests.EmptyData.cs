using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

/// <summary>
/// 图表服务空数据处理测试
/// </summary>
public class ChartServiceEmptyDataTests
{
    private readonly Mock<ILogger<ChartService>> _loggerMock;
    private readonly Mock<IChartProvider> _defaultProviderMock;
    private readonly Mock<IDataProcessor> _dataProcessorMock;
    private readonly Mock<IChartRecommender> _recommenderMock;
    private readonly ChartService _service;

    public ChartServiceEmptyDataTests()
    {
        _loggerMock = new Mock<ILogger<ChartService>>();
        _defaultProviderMock = new Mock<IChartProvider>();
        _dataProcessorMock = new Mock<IDataProcessor>();
        _recommenderMock = new Mock<IChartRecommender>();

        // 设置默认提供者
        _defaultProviderMock.Setup(p => p.Name).Returns("echarts");
        _defaultProviderMock.Setup(p => p.SupportsChartType(It.IsAny<string>())).Returns(true);

        var providers = new[] { _defaultProviderMock.Object };

        _service = new ChartService(
            _loggerMock.Object,
            providers,
            _dataProcessorMock.Object,
            _recommenderMock.Object,
            "echarts");
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("line")]
    [InlineData("pie")]
    [InlineData("gauge")]
    public async Task CreateChartConfigAsync_WithEmptyDataCollection_ShouldReturnEmptyChartConfig(string chartType)
    {
        // Arrange
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "数据集合为空。请确保您的查询返回至少一条记录。"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        Assert.NotNull(result);
        
        // 验证返回的是空图表配置
        dynamic config = result;
        Assert.NotNull(config.title);
        Assert.Equal("暂无数据", config.title.text);
        
        // 验证日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("尝试使用空数据集创建图表")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithEmptyDataAndChineseErrorMessage_ShouldReturnEmptyChartConfig()
    {
        // Arrange
        var chartType = "bar";
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "数据集合为空。请确保您的查询返回至少一条记录。"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        Assert.NotNull(result);
        
        // 验证返回的是空图表配置而不是抛出异常
        dynamic config = result;
        Assert.NotNull(config.title);
        Assert.Equal("暂无数据", config.title.text);
        Assert.Contains("数据集合为空", config.title.subtext);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithEmptyDataAndEnglishErrorMessage_ShouldReturnEmptyChartConfig()
    {
        // Arrange
        var chartType = "line";
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "Data collection is empty"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        Assert.NotNull(result);
        
        // 验证返回的是空图表配置而不是抛出异常
        dynamic config = result;
        Assert.NotNull(config.title);
        Assert.Equal("暂无数据", config.title.text);
        Assert.Contains("Data collection is empty", config.title.subtext);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithNonEmptyValidationError_ShouldThrowException()
    {
        // Arrange
        var chartType = "bar";
        var data = new[] { new { Value = "invalid" } };
        var processedData = data;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(data, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "Invalid data format"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateChartConfigAsync(chartType, data));
        
        Assert.Contains("Data validation failed", exception.Message);
        Assert.Contains("Invalid data format", exception.Message);
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("column")]
    public async Task GenerateEmptyChartConfig_ForBarChart_ShouldHaveCorrectStructure(string chartType)
    {
        // Arrange
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "数据集合为空"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        dynamic config = result;
        Assert.NotNull(config.xAxis);
        Assert.NotNull(config.yAxis);
        Assert.NotNull(config.series);
        Assert.NotNull(config.graphic);
        
        // 验证轴配置被隐藏
        Assert.False(config.xAxis.axisLabel.show);
        Assert.False(config.yAxis.axisLabel.show);
    }

    [Theory]
    [InlineData("pie")]
    [InlineData("donut")]
    public async Task GenerateEmptyChartConfig_ForPieChart_ShouldHaveCorrectStructure(string chartType)
    {
        // Arrange
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "数据集合为空"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        dynamic config = result;
        Assert.NotNull(config.series);
        Assert.NotNull(config.graphic);
        
        // 验证饼图被隐藏
        var series = config.series[0];
        Assert.False(series.label.show);
        Assert.False(series.labelLine.show);
    }

    [Fact]
    public async Task GenerateEmptyChartConfig_ForGaugeChart_ShouldHaveCorrectStructure()
    {
        // Arrange
        var chartType = "gauge";
        var emptyData = Array.Empty<object>();
        var processedData = emptyData;

        _dataProcessorMock.Setup(p => p.ProcessDataAsync(emptyData, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, "数据集合为空"));

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, emptyData);

        // Assert
        dynamic config = result;
        Assert.NotNull(config.series);
        Assert.NotNull(config.graphic);
        
        // 验证仪表盘组件被隐藏
        var series = config.series[0];
        Assert.False(series.detail.show);
        Assert.False(series.pointer.show);
    }
} 