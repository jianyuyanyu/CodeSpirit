using CodeSpirit.Charts.Core.Services;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

/// <summary>
/// 仪表盘图表测试
/// </summary>
public class ChartServiceTests_GaugeChart
{
    private readonly Mock<ILogger<ChartService>> _mockLogger;
    private readonly Mock<ILogger<DataProcessor>> _mockDataProcessorLogger;
    private readonly Mock<ILogger<EChartsProvider>> _mockEChartsLogger;
    private readonly ChartService _chartService;

    public ChartServiceTests_GaugeChart()
    {
        _mockLogger = new Mock<ILogger<ChartService>>();
        _mockDataProcessorLogger = new Mock<ILogger<DataProcessor>>();
        _mockEChartsLogger = new Mock<ILogger<EChartsProvider>>();

        var dataProcessor = new DataProcessor(_mockDataProcessorLogger.Object);
        var echartsProvider = new EChartsProvider(_mockEChartsLogger.Object);
        
        _chartService = new ChartService(
            _mockLogger.Object,
            new[] { echartsProvider },
            dataProcessor,
            null!, // recommender not needed for this test
            "echarts"
        );
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithGaugeType_ShouldReturnValidConfig()
    {
        // Arrange
        var data = new[]
        {
            new { Status = "成功", Percentage = 95.5 }
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", data);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含 series 配置
        Assert.True(config.ContainsKey("series"));
        
        // 验证 series 是数组
        var series = Assert.IsAssignableFrom<Array>(config["series"]);
        Assert.True(series.Length > 0);
        
        // 验证第一个 series 是仪表盘类型
        var firstSeries = Assert.IsType<Dictionary<string, object>>(series.GetValue(0));
        Assert.Equal("gauge", firstSeries["type"]);
        
        // 验证包含数据
        Assert.True(firstSeries.ContainsKey("data"));
        var seriesData = Assert.IsAssignableFrom<Array>(firstSeries["data"]);
        Assert.True(seriesData.Length > 0);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithGaugeTypeAndZeroValue_ShouldReturnValidConfig()
    {
        // Arrange - 模拟零值情况（避免 NaN）
        var data = new[]
        {
            new { Status = "成功", Percentage = 0.0 }
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", data);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含 series 配置
        Assert.True(config.ContainsKey("series"));
        
        var series = Assert.IsAssignableFrom<Array>(config["series"]);
        var firstSeries = Assert.IsType<Dictionary<string, object>>(series.GetValue(0));
        var seriesData = Assert.IsAssignableFrom<Array>(firstSeries["data"]);
        
        // 验证数据中的值不是 NaN
        var dataItem = Assert.IsType<Dictionary<string, object>>(seriesData.GetValue(0));
        Assert.True(dataItem.ContainsKey("value"));
        
        var value = dataItem["value"];
        Assert.True(value is double || value is int || value is float);
        
        // 确保值不是 NaN
        if (value is double doubleValue)
        {
            Assert.False(double.IsNaN(doubleValue), "仪表盘数据值不应该是 NaN");
        }
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithGaugeTypeAndEmptyData_ShouldReturnDefaultConfig()
    {
        // Arrange
        var emptyData = Array.Empty<object>();

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", emptyData);

        // Assert
        Assert.NotNull(result);
        
        // 空数据时返回的是匿名类型的默认配置，不是字典
        // 验证结果包含基本属性
        var resultType = result.GetType();
        Assert.True(resultType.Name.Contains("AnonymousType") || result is Dictionary<string, object>);
        
        // 如果是字典类型，验证包含必要的配置
        if (result is Dictionary<string, object> config)
        {
            Assert.True(config.ContainsKey("title") || config.ContainsKey("series"));
        }
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithGaugeTypeAndOptions_ShouldIncludeTitle()
    {
        // Arrange
        var data = new[]
        {
            new { Status = "成功", Percentage = 85.2 }
        };
        
        var options = new Dictionary<string, object>
        {
            ["title"] = "操作成功率"
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", data, options);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含标题配置
        Assert.True(config.ContainsKey("title"));
        
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.Equal("操作成功率", title["text"]);
    }

    [Fact]
    public void EChartsProvider_SupportsGaugeType_ShouldReturnTrue()
    {
        // Arrange
        var provider = new EChartsProvider(_mockEChartsLogger.Object);

        // Act
        var supports = provider.SupportsChartType("gauge");

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void EChartsProvider_SupportedChartTypes_ShouldIncludeGauge()
    {
        // Arrange
        var provider = new EChartsProvider(_mockEChartsLogger.Object);

        // Act
        var supportedTypes = provider.SupportedChartTypes;

        // Assert
        Assert.Contains("gauge", supportedTypes, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithGaugeTypeAndValidPercentage_ShouldNotContainNaN()
    {
        // Arrange - 测试有效的百分比值
        var data = new[]
        {
            new { Status = "成功", Percentage = 95.5 },
            new { Status = "失败", Percentage = 4.5 }
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "gauge", data);

        // Assert
        Assert.NotNull(result);
        
        var config = Assert.IsType<Dictionary<string, object>>(result);
        var series = Assert.IsAssignableFrom<Array>(config["series"]);
        var firstSeries = Assert.IsType<Dictionary<string, object>>(series.GetValue(0));
        var seriesData = Assert.IsAssignableFrom<Array>(firstSeries["data"]);
        
        // 验证所有数据项的值都不是 NaN
        foreach (var item in seriesData)
        {
            var dataItem = Assert.IsType<Dictionary<string, object>>(item);
            Assert.True(dataItem.ContainsKey("value"));
            
            var value = dataItem["value"];
            if (value is double doubleValue)
            {
                Assert.False(double.IsNaN(doubleValue), $"仪表盘数据值不应该是 NaN，实际值: {value}");
                Assert.True(doubleValue >= 0, $"仪表盘数据值应该大于等于0，实际值: {doubleValue}");
            }
            else if (value is string stringValue)
            {
                Assert.NotEqual("NaN", stringValue);
            }
        }
    }
} 