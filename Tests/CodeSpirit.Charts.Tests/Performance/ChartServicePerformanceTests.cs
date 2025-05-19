using System.Diagnostics;
using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Charts.Tests.Performance;

/// <summary>
/// 图表服务性能测试
/// </summary>
public class ChartServicePerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ChartService> _logger;
    private readonly ILogger<DataProcessor> _dataProcessorLogger;
    private readonly IChartService _chartService;
    private readonly IDataProcessor _dataProcessor;
    private readonly IChartProvider _provider;

    public ChartServicePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 创建实际的日志记录器
        _logger = Mock.Of<ILogger<ChartService>>();
        _dataProcessorLogger = Mock.Of<ILogger<DataProcessor>>();

        // 创建实际的数据处理器
        _dataProcessor = new DataProcessor(_dataProcessorLogger);
        
        // 创建模拟的推荐器
        var recommenderMock = new Mock<IChartRecommender>();
        recommenderMock.Setup(r => r.RecommendChartTypesAsync(It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(new[]
            {
                new ChartRecommendation
                {
                    ChartType = "line",
                    Score = 100,
                    Reason = "Performance test"
                },
                new ChartRecommendation
                {
                    ChartType = "bar",
                    Score = 80,
                    Reason = "Performance test"
                },
                new ChartRecommendation
                {
                    ChartType = "pie",
                    Score = 60,
                    Reason = "Performance test"
                }
            });
        
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
        _chartService = new ChartService(_logger, new[] { _provider }, _dataProcessor, recommenderMock.Object);
    }

    [Fact]
    public async Task ProcessLargeDataset_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var dataSize = 10000;
        var data = GenerateLargeDataset(dataSize);
        var maxProcessingTimeMs = 1000; // 1秒

        // Act
        var stopwatch = Stopwatch.StartNew();
        var processedData = await _dataProcessor.ProcessDataAsync(data);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Processing {dataSize} records took {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < maxProcessingTimeMs, 
            $"Processing took too long: {stopwatch.ElapsedMilliseconds}ms > {maxProcessingTimeMs}ms");
        Assert.NotNull(processedData);
    }

    [Fact]
    public async Task TransformLargeDataset_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var dataSize = 10000;
        var data = GenerateLargeDataset(dataSize);
        var chartType = "line";
        var maxTransformTimeMs = 1000; // 1秒

        // 先处理数据
        var processedData = await _dataProcessor.ProcessDataAsync(data);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var transformedData = await _dataProcessor.TransformForChartTypeAsync(processedData, chartType);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Transforming {dataSize} records took {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < maxTransformTimeMs, 
            $"Transformation took too long: {stopwatch.ElapsedMilliseconds}ms > {maxTransformTimeMs}ms");
        Assert.NotNull(transformedData);
    }

    [Fact]
    public async Task CreateChartConfig_WithLargeDataset_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var dataSize = 5000;
        var data = GenerateLargeDataset(dataSize);
        var chartType = "line";
        var maxConfigTimeMs = 2000; // 2秒

        // Act
        var stopwatch = Stopwatch.StartNew();
        var chartConfig = await _chartService.CreateChartConfigAsync(chartType, data);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Creating chart config for {dataSize} records took {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < maxConfigTimeMs, 
            $"Chart config creation took too long: {stopwatch.ElapsedMilliseconds}ms > {maxConfigTimeMs}ms");
        Assert.NotNull(chartConfig);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task ScalabilityTest_ProcessingTime_ShouldScaleLinearly(int dataSize)
    {
        // Arrange
        var data = GenerateLargeDataset(dataSize);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var processedData = await _dataProcessor.ProcessDataAsync(data);
        stopwatch.Stop();

        // Assert
        var processingTimeMs = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Processing {dataSize} records took {processingTimeMs}ms");
        
        // 记录处理时间与数据大小的比率
        var timePerRecord = (double)processingTimeMs / dataSize;
        _output.WriteLine($"Time per record: {timePerRecord:F6}ms");
        
        // 确保处理时间不会超过合理的限制
        Assert.True(processingTimeMs < dataSize * 0.5, 
            $"Processing time scales poorly with data size: {processingTimeMs}ms for {dataSize} records");
    }

    private static object[] GenerateLargeDataset(int size)
    {
        var random = new Random(42); // 使用固定种子以获得可重复的结果
        var categories = new[] { "A", "B", "C", "D", "E" };
        
        return Enumerable.Range(0, size)
            .Select(i => new 
            {
                Id = i,
                Category = categories[random.Next(categories.Length)],
                Value = random.Next(1, 1000),
                Date = DateTime.Now.AddDays(-random.Next(0, 365))
            })
            .ToArray();
    }
}