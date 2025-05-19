using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Providers.ECharts;

public class EChartsProviderTests
{
    private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
    private readonly EChartsProvider _provider;

    public EChartsProviderTests()
    {
        _loggerMock = new Mock<ILogger<EChartsProvider>>();
        _provider = new EChartsProvider(_loggerMock.Object);
    }

    [Fact]
    public void Name_ShouldReturnECharts()
    {
        // Act
        var name = _provider.Name;

        // Assert
        Assert.Equal("echarts", name);
    }

    [Fact]
    public void SupportedChartTypes_ShouldReturnAllSupportedTypes()
    {
        // Act
        var types = _provider.SupportedChartTypes;

        // Assert
        Assert.NotNull(types);
        Assert.NotEmpty(types);
        Assert.Contains("line", types);
        Assert.Contains("bar", types);
        Assert.Contains("pie", types);
    }

    [Theory]
    [InlineData("line", true)]
    [InlineData("bar", true)]
    [InlineData("pie", true)]
    [InlineData("unknown", false)]
    public void SupportsChartType_ShouldReturnCorrectResult(string chartType, bool expected)
    {
        // Act
        var result = _provider.SupportsChartType(chartType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateChartConfig_WithLineChart_ShouldReturnValidConfig()
    {
        // Arrange
        var chartType = "line";
        var data = new Dictionary<string, object>
        {
            ["categories"] = new[] { "A", "B", "C" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Series 1",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var options = new Dictionary<string, object>
        {
            ["title"] = "Line Chart"
        };

        // Act
        var result = _provider.GenerateChartConfig(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("xAxis"));
        Assert.True(config.ContainsKey("yAxis"));
        Assert.True(config.ContainsKey("series"));
        Assert.True(config.ContainsKey("title"));
        
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.Equal("Line Chart", title["text"]);
    }

    [Fact]
    public void GenerateChartConfig_WithBarChart_ShouldReturnValidConfig()
    {
        // Arrange
        var chartType = "bar";
        var data = new Dictionary<string, object>
        {
            ["categories"] = new[] { "A", "B", "C" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Series 1",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };

        // Act
        var result = _provider.GenerateChartConfig(chartType, data);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("xAxis"));
        Assert.True(config.ContainsKey("yAxis"));
        Assert.True(config.ContainsKey("series"));
        
        var series = Assert.IsType<Dictionary<string, object>[]>(config["series"]);
        Assert.Equal("bar", series[0]["type"]);
    }

    [Fact]
    public void GenerateChartConfig_WithPieChart_ShouldReturnValidConfig()
    {
        // Arrange
        var chartType = "pie";
        var data = new Dictionary<string, object>
        {
            ["data"] = new[]
            {
                new Dictionary<string, object> { ["name"] = "A", ["value"] = 1 },
                new Dictionary<string, object> { ["name"] = "B", ["value"] = 2 },
                new Dictionary<string, object> { ["name"] = "C", ["value"] = 3 }
            }
        };

        // Act
        var result = _provider.GenerateChartConfig(chartType, data);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("series"));
        
        var series = Assert.IsType<Dictionary<string, object>[]>(config["series"]);
        Assert.Equal("pie", series[0]["type"]);
    }

    [Fact]
    public void GenerateChartConfig_WithUnsupportedType_ShouldThrowException()
    {
        // Arrange
        var chartType = "unsupported";
        var data = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.GenerateChartConfig(chartType, data));
    }

    [Fact]
    public void GenerateChartConfig_WithCommonOptions_ShouldApplyOptions()
    {
        // Arrange
        var chartType = "line";
        var data = new Dictionary<string, object>
        {
            ["categories"] = new[] { "A", "B", "C" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Series 1",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var options = new Dictionary<string, object>
        {
            ["title"] = "Test Chart",
            ["legend"] = new Dictionary<string, object> { ["show"] = true },
            ["tooltip"] = new Dictionary<string, object> { ["trigger"] = "axis" },
            ["grid"] = new Dictionary<string, object> { ["left"] = "10%" }
        };

        // Act
        var result = _provider.GenerateChartConfig(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("title"));
        Assert.True(config.ContainsKey("legend"));
        Assert.True(config.ContainsKey("tooltip"));
        Assert.True(config.ContainsKey("grid"));
        
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.Equal("Test Chart", title["text"]);
        
        var legend = Assert.IsType<Dictionary<string, object>>(config["legend"]);
        Assert.True((bool)legend["show"]);
        
        var tooltip = Assert.IsType<Dictionary<string, object>>(config["tooltip"]);
        Assert.Equal("axis", tooltip["trigger"]);
        
        var grid = Assert.IsType<Dictionary<string, object>>(config["grid"]);
        Assert.Equal("10%", grid["left"]);
    }

    [Fact]
    public void GenerateChartConfig_WithCustomOption_ShouldIncludeCustomOption()
    {
        // Arrange
        var chartType = "line";
        var data = new Dictionary<string, object>
        {
            ["categories"] = new[] { "A", "B", "C" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Series 1",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var options = new Dictionary<string, object>
        {
            ["customOption"] = "custom value"
        };

        // Act
        var result = _provider.GenerateChartConfig(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("customOption"));
        Assert.Equal("custom value", config["customOption"]);
    }
}