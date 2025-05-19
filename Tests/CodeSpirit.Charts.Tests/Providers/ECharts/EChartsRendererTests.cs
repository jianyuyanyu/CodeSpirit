using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.Charts.Tests.Providers.ECharts;

public class EChartsRendererTests
{
    private readonly Mock<ILogger<EChartsRenderer>> _loggerMock;
    private readonly Mock<IChartThemeManager> _themeManagerMock;
    private readonly EChartsRenderer _renderer;

    public EChartsRendererTests()
    {
        _loggerMock = new Mock<ILogger<EChartsRenderer>>();
        _themeManagerMock = new Mock<IChartThemeManager>();
        _renderer = new EChartsRenderer(_loggerMock.Object, _themeManagerMock.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnECharts()
    {
        // Act
        var name = _renderer.ProviderName;

        // Assert
        Assert.Equal("echarts", name);
    }

    [Fact]
    public async Task GenerateRenderConfigAsync_WithDictionaryConfig_ShouldReturnValidConfig()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var options = new Dictionary<string, object>
        {
            ["renderer"] = "canvas",
            ["width"] = 800,
            ["height"] = 600
        };

        // Act
        var result = await _renderer.GenerateRenderConfigAsync(chartConfig, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("title"));
        Assert.True(config.ContainsKey("series"));
        Assert.True(config.ContainsKey("renderer"));
        Assert.True(config.ContainsKey("width"));
        Assert.True(config.ContainsKey("height"));
        
        Assert.Equal("canvas", config["renderer"]);
        Assert.Equal(800, config["width"]);
        Assert.Equal(600, config["height"]);
    }

    [Fact]
    public async Task GenerateRenderConfigAsync_WithNonDictionaryConfig_ShouldWrapInConfig()
    {
        // Arrange
        var chartConfig = "test config";

        // Act
        var result = await _renderer.GenerateRenderConfigAsync(chartConfig);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("config"));
        Assert.Equal("test config", config["config"]);
    }

    [Fact]
    public async Task GenerateAmisConfigAsync_ShouldReturnValidAmisConfig()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var options = new Dictionary<string, object>
        {
            ["height"] = 400,
            ["width"] = 600,
            ["dataSource"] = "/api/data",
            ["refreshInterval"] = 30000,
            ["loadingText"] = "Loading...",
            ["amisOptions"] = new Dictionary<string, object>
            {
                ["className"] = "custom-chart"
            }
        };

        // Act
        var result = await _renderer.GenerateAmisConfigAsync(chartConfig, options);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.Equal("chart", config["type"]);
        Assert.Equal(chartConfig, config["config"]);
        Assert.Equal(400, config["height"]);
        Assert.Equal(600, config["width"]);
        Assert.Equal("/api/data", config["source"]);
        Assert.Equal(30000, config["interval"]);
        Assert.Equal("Loading...", config["loadingText"]);
        Assert.Equal("custom-chart", config["className"]);
    }

    [Fact]
    public async Task ApplyThemeAsync_WithThemeName_ShouldApplyTheme()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" },
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "line",
                    ["data"] = new[] { 1, 2, 3 }
                }
            }
        };
        var themeName = "dark";
        var themeConfig = new Dictionary<string, object>
        {
            ["backgroundColor"] = "#333",
            ["textStyle"] = new Dictionary<string, object> { ["color"] = "#fff" }
        };

        _themeManagerMock.Setup(m => m.GetThemeConfigAsync(themeName, "echarts"))
            .ReturnsAsync(themeConfig);

        // Act
        var result = await _renderer.ApplyThemeAsync(chartConfig, themeName);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("backgroundColor"));
        Assert.Equal("#333", config["backgroundColor"]);
        
        Assert.True(config.ContainsKey("textStyle"));
        var textStyle = Assert.IsType<Dictionary<string, object>>(config["textStyle"]);
        Assert.Equal("#fff", textStyle["color"]);
    }

    [Fact]
    public async Task ApplyThemeAsync_WithThemeConfig_ShouldApplyTheme()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" }
        };
        var themeConfig = new Dictionary<string, object>
        {
            ["backgroundColor"] = "#333",
            ["title"] = new Dictionary<string, object> { ["textStyle"] = new Dictionary<string, object> { ["color"] = "#fff" } }
        };

        // Act
        var result = await _renderer.ApplyThemeAsync(chartConfig, themeConfig);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.True(config.ContainsKey("backgroundColor"));
        Assert.Equal("#333", config["backgroundColor"]);
        
        Assert.True(config.ContainsKey("title"));
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.True(title.ContainsKey("text"));
        Assert.Equal("Test Chart", title["text"]);
        Assert.True(title.ContainsKey("textStyle"));
        var textStyle = Assert.IsType<Dictionary<string, object>>(title["textStyle"]);
        Assert.Equal("#fff", textStyle["color"]);
    }

    [Fact]
    public async Task GetResponsiveConfigAsync_ShouldAdjustConfigForContainerSize()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" }
        };
        var containerSize = (Width: 400, Height: 300);

        // Act
        var result = await _renderer.GetResponsiveConfigAsync(chartConfig, containerSize);

        // Assert
        Assert.NotNull(result);
        var config = Assert.IsType<Dictionary<string, object>>(result);
        
        Assert.Equal(400, config["width"]);
        Assert.Equal(300, config["height"]);
        
        Assert.True(config.ContainsKey("grid"));
        var grid = Assert.IsType<Dictionary<string, object>>(config["grid"]);
        Assert.Equal("5%", grid["left"]);
        Assert.Equal("5%", grid["right"]);
        Assert.Equal("10%", grid["top"]);
        Assert.Equal("10%", grid["bottom"]);
        Assert.True((bool)grid["containLabel"]);
        
        Assert.True(config.ContainsKey("title"));
        var title = Assert.IsType<Dictionary<string, object>>(config["title"]);
        Assert.True(title.ContainsKey("textStyle"));
        var textStyle = Assert.IsType<Dictionary<string, object>>(title["textStyle"]);
        Assert.Equal(14, textStyle["fontSize"]);
    }

    [Fact]
    public async Task GeneratePreviewImageAsync_ShouldReturnEmptyArray()
    {
        // Arrange
        var chartConfig = new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object> { ["text"] = "Test Chart" }
        };

        // Act
        var result = await _renderer.GeneratePreviewImageAsync(chartConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}