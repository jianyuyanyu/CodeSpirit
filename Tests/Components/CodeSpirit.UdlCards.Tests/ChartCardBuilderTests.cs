using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// ChartCardBuilder 单元测试
/// </summary>
public class ChartCardBuilderTests
{
    private readonly ChartCardBuilder _builder;

    public ChartCardBuilderTests()
    {
        _builder = new ChartCardBuilder(NullLogger<ChartCardBuilder>.Instance);
    }

    [Fact]
    public void CardType_ShouldReturnChart()
    {
        // Act & Assert
        _builder.CardType.Should().Be("chart");
    }

    [Fact]
    public void Build_WithMinimalConfig_ShouldReturnBasicCard()
    {
        // Arrange
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表"
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("chart");
        result["id"].Should().Be("test-chart");
        result["className"].Should().Be("amis-cards-chart");
    }

    [Fact]
    public void Build_WithChartConfig_ShouldIncludeChartProperties()
    {
        // Arrange
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表",
            Chart = new ChartConfig
            {
                Type = "line",
                Height = 400,
                Theme = "dark",
                Responsive = true,
                Options = new Dictionary<string, object>
                {
                    ["grid"] = new { left = "3%", right = "4%" },
                    ["xAxis"] = new { type = "category" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("chart");
        var chart = result["chart"] as Dictionary<string, object>;
        chart["type"].Should().Be("line");
        chart["height"].Should().Be(400);
        chart["theme"].Should().Be("dark");
        chart["responsive"].Should().Be(true);
        chart.Should().ContainKey("config");
    }

    [Fact]
    public void Build_WithDataConfig_ShouldIncludeDataProperties()
    {
        // Arrange
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表",
            Chart = new ChartConfig { Type = "line" },
            Data = new ChartDataConfig
            {
                ApiUrl = "/api/charts/data",
                FieldMapping = new ChartFieldMapping
                {
                    XField = "date",
                    YField = "value",
                    SeriesField = "category"
                },
                RefreshInterval = 30000,
                StaticData = new List<Dictionary<string, object>>
                {
                    new() { ["series"] = new[] { new { name = "测试", data = new[] { 1, 2, 3 } } } }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        // 静态数据会直接设置在顶层的 data 键中
        result.Should().ContainKey("data");
        var data = result["data"] as System.Collections.ICollection;
        data.Should().NotBeNull();
        data!.Count.Should().Be(1);

        // API 配置会设置在顶层的 api 键中
        result.Should().ContainKey("api");
        var api = result["api"] as Dictionary<string, object>;
        api["method"].Should().Be("get");
        api["url"].Should().Be("/api/charts/data");

        // 刷新间隔会设置在顶层的 interval 键中
        result.Should().ContainKey("interval");
        result["interval"].Should().Be(30000);

        // 字段映射会设置在顶层的 dataMapping 键中
        result.Should().ContainKey("dataMapping");
        var mapping = result["dataMapping"] as Dictionary<string, object>;
        mapping["x"].Should().Be("date");
        mapping["y"].Should().Be("value");
        mapping["series"].Should().Be("category");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表",
            Chart = new ChartConfig { Type = "line" },
            Data = new ChartDataConfig { ApiUrl = "/api/data" }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyChartType_ShouldReturnFalse()
    {
        // Arrange
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表",
            Chart = new ChartConfig { Type = "" }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IUdlCardBuilderBase_Build_WithCorrectType_ShouldWork()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表"
        };

        // Act
        var result = builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("chart");
    }
} 