using CodeSpirit.Charts.Core.Services;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Core.Services;

/// <summary>
/// 卡片图表测试
/// </summary>
public class ChartServiceTests_CardChart
{
    private readonly Mock<ILogger<ChartService>> _mockLogger;
    private readonly Mock<ILogger<DataProcessor>> _mockDataProcessorLogger;
    private readonly Mock<ILogger<EChartsProvider>> _mockEChartsLogger;
    private readonly ChartService _chartService;

    public ChartServiceTests_CardChart()
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
    public async Task CreateChartConfigAsync_WithCardType_ShouldReturnValidConfig()
    {
        // Arrange
        var data = new[]
        {
            new { Period = "今日", Count = 150 },
            new { Period = "本周", Count = 1200 },
            new { Period = "本月", Count = 5000 }
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "card", data);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含 graphic 配置（卡片图表使用 graphic 组件）
        Assert.True(config.ContainsKey("graphic"));
        
        // 验证包含基本的坐标轴配置（隐藏的）
        Assert.True(config.ContainsKey("xAxis"));
        Assert.True(config.ContainsKey("yAxis"));
        
        // 验证坐标轴是隐藏的
        var xAxis = Assert.IsType<Dictionary<string, object>>(config["xAxis"]);
        var yAxis = Assert.IsType<Dictionary<string, object>>(config["yAxis"]);
        
        Assert.Equal(false, xAxis["show"]);
        Assert.Equal(false, yAxis["show"]);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithCardTypeAndEmptyData_ShouldReturnDefaultConfig()
    {
        // Arrange
        var emptyData = Array.Empty<object>();

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "card", emptyData);

        // Assert
        Assert.NotNull(result);
        
        // 空数据时返回的是匿名类型的默认配置，不是字典
        // 验证结果包含基本属性
        var resultType = result.GetType();
        Assert.True(resultType.Name.Contains("AnonymousType") || result is Dictionary<string, object>);
        
        // 如果是字典类型，验证包含必要的配置
        if (result is Dictionary<string, object> config)
        {
            Assert.True(config.ContainsKey("title") || config.ContainsKey("graphic"));
        }
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithCardTypeAndSingleValue_ShouldReturnValidConfig()
    {
        // Arrange
        var data = new[]
        {
            new { Name = "总用户数", Value = 12345 }
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "card", data);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含 graphic 配置
        Assert.True(config.ContainsKey("graphic"));
        
        // 验证 graphic 是数组
        var graphic = Assert.IsAssignableFrom<Array>(config["graphic"]);
        Assert.True(graphic.Length > 0);
    }

    [Fact]
    public async Task CreateChartConfigAsync_WithCardTypeAndOptions_ShouldIncludeTitle()
    {
        // Arrange
        var data = new[]
        {
            new { Period = "今日", Count = 150 }
        };
        
        var options = new Dictionary<string, object>
        {
            ["title"] = "系统统计"
        };

        // Act
        var result = await _chartService.CreateChartConfigAsync("echarts", "card", data, options);

        // Assert
        Assert.NotNull(result);
        
        // 验证结果是字典类型
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        // 验证包含标题配置
        Assert.True(config.ContainsKey("title"));
        
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.Equal("系统统计", title["text"]);
    }

    [Fact]
    public void EChartsProvider_SupportsCardType_ShouldReturnTrue()
    {
        // Arrange
        var provider = new EChartsProvider(_mockEChartsLogger.Object);

        // Act
        var supports = provider.SupportsChartType("card");

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void EChartsProvider_SupportedChartTypes_ShouldIncludeCard()
    {
        // Arrange
        var provider = new EChartsProvider(_mockEChartsLogger.Object);

        // Act
        var supportedTypes = provider.SupportedChartTypes;

        // Assert
        Assert.Contains("card", supportedTypes, StringComparer.OrdinalIgnoreCase);
    }
} 