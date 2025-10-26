using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Integration;

/// <summary>
/// 图表服务集成测试
/// </summary>
public class ChartServiceIntegrationTests
{
    private readonly ILogger<ChartService> _logger;
    private readonly ILogger<DataProcessor> _dataProcessorLogger;
    private readonly ILogger<ChartRecommender> _recommenderLogger;
    private readonly ILogger<ChartThemeManager> _themeManagerLogger;
    private readonly IChartService _chartService;
    private readonly IDataProcessor _dataProcessor;
    private readonly IChartRecommender _recommender;
    private readonly IChartThemeManager _themeManager;
    private readonly IChartProvider _provider;

    public ChartServiceIntegrationTests()
    {
        // 创建实际的日志记录器
        _logger = Mock.Of<ILogger<ChartService>>();
        _dataProcessorLogger = Mock.Of<ILogger<DataProcessor>>();
        _recommenderLogger = Mock.Of<ILogger<ChartRecommender>>();
        _themeManagerLogger = Mock.Of<ILogger<ChartThemeManager>>();

        // 创建实际的数据处理器
        _dataProcessor = new DataProcessor(_dataProcessorLogger);
        
        // 创建实际的推荐器
        _recommender = new ChartRecommender(_recommenderLogger);
        
        // 创建实际的主题管理器
        _themeManager = new ChartThemeManager(_themeManagerLogger);
        
        // 创建模拟的提供者
        var providerMock = new Mock<IChartProvider>();
        providerMock.Setup(p => p.Name).Returns("echarts");
        providerMock.Setup(p => p.SupportedChartTypes).Returns(new[] { "line", "bar", "pie" });
        providerMock.Setup(p => p.SupportsChartType(It.IsAny<string>())).Returns<string>(type => 
            new[] { "line", "bar", "pie" }.Contains(type.ToLowerInvariant()));
        providerMock.Setup(p => p.GenerateChartConfig(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
            .Returns<string, object, object>((type, data, options) => new
            {
                type,
                data,
                options
            });
        
        _provider = providerMock.Object;
        
        // 创建实际的图表服务
        _chartService = new ChartService(_logger, new[] { _provider }, _dataProcessor, _recommender);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithValidData_ShouldReturnConfig()
    {
        // Arrange
        var chartType = "line";
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };
        var options = new { title = "Test Chart" };

        // Act
        var result = await _chartService.CreateChartConfigAsync(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        dynamic config = result;
        Assert.Equal(chartType, config.type);
        Assert.NotNull(config.data);
        Assert.Equal(options, config.options);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithInvalidChartType_ShouldThrowException()
    {
        // Arrange
        var chartType = "invalid";
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _chartService.CreateChartConfigAsync(chartType, data));
    }

    [Fact]
    public async Task RecommendChartTypesAsync_WithValidData_ShouldReturnRecommendations()
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var result = await _chartService.RecommendChartTypesAsync(data);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ProcessAndTransformData_WithValidData_ShouldReturnTransformedData()
    {
        // Arrange
        var chartType = "line";
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act
        var processedData = await _dataProcessor.ProcessDataAsync(data);
        var transformedData = await _dataProcessor.TransformForChartTypeAsync(processedData, chartType);

        // Assert
        Assert.NotNull(transformedData);
    }

    [Fact]
    public async Task EndToEndChartCreation_WithValidData_ShouldSucceed()
    {
        // Arrange
        var data = new[]
        {
            new { Category = "A", Value = 1 },
            new { Category = "B", Value = 2 },
            new { Category = "C", Value = 3 }
        };

        // Act - 推荐图表类型
        var recommendedTypes = await _chartService.RecommendChartTypesAsync(data);
        
        // 确保有推荐的图表类型
        Assert.NotEmpty(recommendedTypes);
        
        // 使用第一个推荐的图表类型
        var chartType = recommendedTypes.First();
        
        // 创建图表配置
        var chartConfig = await _chartService.CreateChartConfigAsync(chartType, data);
        
        // Assert
        Assert.NotNull(chartConfig);
    }
}