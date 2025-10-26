using CodeSpirit.Charts.AmisIntegration;
using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.AmisIntegration;

public class AmisChartConfigGeneratorTests
{
    private readonly Mock<ILogger<AmisChartConfigGenerator>> _loggerMock;
    private readonly Mock<IChartService> _chartServiceMock;
    private readonly AmisChartConfigGenerator _generator;

    public AmisChartConfigGeneratorTests()
    {
        _loggerMock = new Mock<ILogger<AmisChartConfigGenerator>>();
        _chartServiceMock = new Mock<IChartService>();
        _generator = new AmisChartConfigGenerator(_loggerMock.Object, _chartServiceMock.Object);
    }

    [Fact]
    public async Task GenerateAmisChartConfigAsync_ShouldReturnValidConfig()
    {
        // Arrange
        var chartType = "line";
        var data = new { values = new[] { 1, 2, 3 } };
        var options = new Dictionary<string, object>
        {
            ["provider"] = "echarts",
            ["title"] = "Test Chart"
        };

        var chartConfig = new Dictionary<string, object>
        {
            ["type"] = "line",
            ["data"] = data
        };

        var amisConfig = new Dictionary<string, object>
        {
            ["type"] = "chart",
            ["config"] = chartConfig
        };

        _chartServiceMock.Setup(s => s.CreateChartConfigAsync("echarts", chartType, data, options))
            .ReturnsAsync(chartConfig);
        _chartServiceMock.Setup(s => s.GetAmisConfigAsync(chartConfig, "echarts", options))
            .ReturnsAsync(amisConfig);

        // Act
        var result = await _generator.GenerateAmisChartConfigAsync(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal("chart", config["type"]);
        Assert.Equal(chartConfig, config["config"]);
    }

    [Fact]
    public async Task GenerateAmisComponentConfigAsync_ShouldReturnValidConfig()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["type"] = "line",
            ["data"] = new[] { 1, 2, 3 }
        };

        var options = new Dictionary<string, object>
        {
            ["provider"] = "echarts"
        };

        var amisConfig = new Dictionary<string, object>
        {
            ["type"] = "chart",
            ["config"] = chartConfig
        };

        _chartServiceMock.Setup(s => s.GetAmisConfigAsync(chartConfig, "echarts", options))
            .ReturnsAsync(amisConfig);

        // Act
        var result = await _generator.GenerateAmisComponentConfigAsync(chartConfig, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal("chart", config["type"]);
        Assert.Equal(chartConfig, config["config"]);
    }

    [Fact]
    public void GenerateAmisDataSourceConfig_WithUrlString_ShouldReturnApiConfig()
    {
        // Arrange
        var url = "https://api.example.com/data";

        // Act
        var result = _generator.GenerateAmisDataSourceConfig(url);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        Assert.True(config.ContainsKey("api"));
        Assert.Equal(url, config["api"]);
    }

    [Fact]
    public void GenerateAmisDataSourceConfig_WithApiConfig_ShouldReturnApiConfig()
    {
        // Arrange
        var apiConfig = new Dictionary<string, object>
        {
            ["url"] = "https://api.example.com/data",
            ["method"] = "POST",
            ["data"] = new { id = 1 }
        };

        // Act
        var result = _generator.GenerateAmisDataSourceConfig(apiConfig);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        Assert.True(config.ContainsKey("api"));
        Assert.Equal(apiConfig, config["api"]);
    }

    [Fact]
    public void GenerateAmisDataSourceConfig_WithStaticData_ShouldReturnDataConfig()
    {
        // Arrange
        var data = new[] { 1, 2, 3 };

        // Act
        var result = _generator.GenerateAmisDataSourceConfig(data);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        Assert.True(config.ContainsKey("data"));
        Assert.Equal(data, config["data"]);
    }

    [Fact]
    public async Task GenerateAmisPageConfigAsync_ShouldReturnValidConfig()
    {
        // Arrange
        var chartType = "line";
        var data = new { values = new[] { 1, 2, 3 } };
        var options = new Dictionary<string, object>
        {
            ["title"] = "Test Page",
            ["pageOptions"] = new Dictionary<string, object>
            {
                ["className"] = "custom-page",
                ["toolbar"] = new[] { "refresh", "export" }
            }
        };

        var chartConfig = new Dictionary<string, object>
        {
            ["type"] = "chart",
            ["config"] = new Dictionary<string, object>
            {
                ["type"] = "line",
                ["data"] = data
            }
        };

        _chartServiceMock.Setup(s => s.CreateChartConfigAsync(It.IsAny<string>(), chartType, data, options))
            .ReturnsAsync(chartConfig);
        _chartServiceMock.Setup(s => s.GetAmisConfigAsync(It.IsAny<object>(), It.IsAny<string>(), options))
            .ReturnsAsync(chartConfig);

        // Act
        var result = await _generator.GenerateAmisPageConfigAsync(chartType, data, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.Equal("page", config["type"]);
        Assert.Equal("Test Page", config["title"]);
        Assert.Equal("custom-page", config["className"]);
        
        var toolbar = Assert.IsType<string[]>(config["toolbar"]);
        Assert.Contains("refresh", toolbar);
        Assert.Contains("export", toolbar);
        
        var body = Assert.IsType<object[]>(config["body"]);
        Assert.Single(body);
        Assert.Equal(chartConfig, body[0]);
    }

    [Fact]
    public async Task GenerateAmisDashboardConfigAsync_ShouldReturnValidConfig()
    {
        // Arrange
        var charts = new[]
        {
            new Dictionary<string, object>
            {
                ["type"] = "chart",
                ["config"] = new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = new[] { 1, 2, 3 }
                }
            },
            new Dictionary<string, object>
            {
                ["type"] = "chart",
                ["config"] = new Dictionary<string, object>
                {
                    ["type"] = "bar",
                    ["data"] = new[] { 4, 5, 6 }
                }
            }
        };

        var options = new Dictionary<string, object>
        {
            ["title"] = "Test Dashboard",
            ["dashboardOptions"] = new Dictionary<string, object>
            {
                ["className"] = "custom-dashboard",
                ["columns"] = 2
            }
        };

        // Act
        var result = await _generator.GenerateAmisDashboardConfigAsync(charts, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.Equal("page", config["type"]);
        Assert.Equal("Test Dashboard", config["title"]);
        Assert.Equal("custom-dashboard", config["className"]);
        Assert.Equal(2, config["columns"]);
        
        var body = Assert.IsType<Dictionary<string, object>>(config["body"]);
        Assert.Equal("grid", body["type"]);
        
        var columns = Assert.IsType<object[]>(body["columns"]);
        Assert.Equal(2, columns.Length);
        Assert.Equal(charts[0], columns[0]);
        Assert.Equal(charts[1], columns[1]);
    }
}