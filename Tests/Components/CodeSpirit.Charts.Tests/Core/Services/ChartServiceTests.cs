using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

public class ChartServiceTests
{
    private readonly Mock<ILogger<ChartService>> _loggerMock;
    private readonly Mock<IChartProvider> _defaultProviderMock;
    private readonly Mock<IDataProcessor> _dataProcessorMock;
    private readonly Mock<IChartRecommender> _recommenderMock;
    private readonly ChartService _service;

    public ChartServiceTests()
    {
        _loggerMock = new Mock<ILogger<ChartService>>();
        _defaultProviderMock = new Mock<IChartProvider>();
        _defaultProviderMock.As<IChartRenderer>();
        _dataProcessorMock = new Mock<IDataProcessor>();
        _recommenderMock = new Mock<IChartRecommender>();

        // 设置默认提供者
        _defaultProviderMock.Setup(p => p.Name).Returns("echarts");
        
        var providers = new[] { _defaultProviderMock.Object };
        _service = new ChartService(_loggerMock.Object, providers, _dataProcessorMock.Object, _recommenderMock.Object);
    }

    [Fact]
    public void GetAvailableProviders_ShouldReturnAllProviders()
    {
        // Arrange
        
        // Act
        var providers = _service.GetAvailableProviders();

        // Assert
        Assert.Single(providers);
        Assert.Contains(providers, p => p.Name == "echarts");
    }

    [Fact]
    public void GetProvider_WithValidName_ShouldReturnProvider()
    {
        // Arrange
        var providerName = "echarts";

        // Act
        var provider = _service.GetProvider(providerName);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(providerName, provider.Name);
    }

    [Fact]
    public void GetProvider_WithInvalidName_ShouldThrowException()
    {
        // Arrange
        var providerName = "invalid";

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _service.GetProvider(providerName));
    }

    [Fact]
    public void GetDefaultProvider_ShouldReturnEChartsProvider()
    {
        // Arrange

        // Act
        var provider = _service.GetDefaultProvider();

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("echarts", provider.Name);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithValidInput_ShouldReturnConfig()
    {
        // Arrange
        var chartType = "line";
        var data = new { values = new[] { 1, 2, 3 } };
        var options = new { title = "Test Chart" };
        var processedData = new { processed = true };
        var transformedData = new { transformed = true };
        var expectedConfig = new { type = "line", data = transformedData };

        _defaultProviderMock.Setup(p => p.SupportsChartType(chartType)).Returns(true);
        _dataProcessorMock.Setup(p => p.ProcessDataAsync(data, options))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((true, null));
        _dataProcessorMock.Setup(p => p.TransformForChartTypeAsync(processedData, chartType, options))
            .ReturnsAsync(transformedData);
        _defaultProviderMock.Setup(p => p.GenerateChartConfig(chartType, transformedData, options))
            .Returns(expectedConfig);

        // Act
        var result = await _service.CreateChartConfigAsync(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig, result);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithUnsupportedChartType_ShouldThrowException()
    {
        // Arrange
        var chartType = "unsupported";
        var data = new { values = new[] { 1, 2, 3 } };
        
        _defaultProviderMock.Setup(p => p.SupportsChartType(chartType)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateChartConfigAsync(chartType, data));
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        var chartType = "line";
        var data = new { values = new[] { 1, 2, 3 } };
        var processedData = new { processed = true };
        var errorMessage = "Invalid data format";

        _defaultProviderMock.Setup(p => p.SupportsChartType(chartType)).Returns(true);
        _dataProcessorMock.Setup(p => p.ProcessDataAsync(data, null))
            .ReturnsAsync(processedData);
        _dataProcessorMock.Setup(p => p.ValidateDataForChartTypeAsync(processedData, chartType))
            .ReturnsAsync((false, errorMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateChartConfigAsync(chartType, data));
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task RecommendChartTypesAsync_ShouldReturnRecommendations()
    {
        // Arrange
        var data = new { values = new[] { 1, 2, 3 } };
        var expectedTypes = new[] { "line", "bar" };

        _recommenderMock.Setup(r => r.RecommendChartTypesAsync(data, "echarts"))
            .ReturnsAsync(expectedTypes.Select(t => new ChartRecommendation 
            { 
                ChartType = t,
                Score = 100,
                Reason = "Test reason"
            }));

        // Act
        var result = await _service.RecommendChartTypesAsync(data);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTypes, result);
    }

    [Fact]
    public async Task ExportChartDataAsync_ShouldReturnExportedData()
    {
        // Arrange
        var chartConfig = new { type = "line" };
        var format = "csv";
        var options = new { delimiter = "," };
        var expectedData = new byte[] { 1, 2, 3 };

        _dataProcessorMock.Setup(p => p.ExportDataAsync(chartConfig, format, options))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _service.ExportChartDataAsync(chartConfig, format, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task GetAmisConfigAsync_WithValidInput_ShouldReturnAmisConfig()
    {
        // Arrange
        var chartConfig = new { type = "line" };
        var providerName = "echarts";
        var options = new { theme = "dark" };
        var expectedConfig = new { type = "chart", config = chartConfig };

        // 设置 _defaultProviderMock 的 IChartRenderer 接口
        _defaultProviderMock.As<IChartRenderer>()
            .Setup(r => r.GenerateAmisConfigAsync(chartConfig, options))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _service.GetAmisConfigAsync(chartConfig, providerName, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig, result);
    }

    [Fact]
    public async Task GetAmisConfigAsync_WithInvalidProvider_ShouldThrowException()
    {
        // Arrange
        var chartConfig = new { type = "line" };
        var providerName = "invalid";
        var options = new { theme = "dark" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.GetAmisConfigAsync(chartConfig, providerName, options));
    }

    [Fact]
    public async Task GetAmisConfigAsync_WithNonRendererProvider_ShouldThrowException()
    {
        // Arrange
        var chartConfig = new { type = "line" };
        var providerName = "echarts";
        var options = new { theme = "dark" };

        // 创建一个不实现 IChartRenderer 的提供者
        var providerMock = new Mock<IChartProvider>();
        providerMock.Setup(p => p.Name).Returns(providerName);
        var service = new ChartService(
            _loggerMock.Object, 
            new[] { providerMock.Object }, 
            _dataProcessorMock.Object, 
            _recommenderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.GetAmisConfigAsync(chartConfig, providerName, options));
    }
}