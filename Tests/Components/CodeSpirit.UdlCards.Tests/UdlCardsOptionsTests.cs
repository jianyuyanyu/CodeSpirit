using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// UdlCardsOptions 单元测试
/// </summary>
public class UdlCardsOptionsTests
{
    [Fact]
    public void UdlCardsOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new UdlCardsOptions();

        // Assert
        options.DefaultTheme.Should().Be("default");
        options.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(15);
        options.MaxCardsPerPage.Should().Be(10);
        options.DefaultRefreshInterval.Should().Be(0);
        options.StrictMode.Should().BeFalse();
        options.EnablePermissionControl.Should().BeTrue();
        options.DebugMode.Should().BeFalse();
        options.PageConfig.Should().BeNull();
        options.LayoutConfig.Should().BeNull();
        options.DashboardConfig.Should().BeNull();
        options.ApiBaseUrl.Should().BeNull();
    }

    [Fact]
    public void UdlCardsOptions_SectionName_ShouldBeCorrect()
    {
        // Act & Assert
        UdlCardsOptions.SectionName.Should().Be("UdlCards");
    }

    [Fact]
    public void UdlCardsOptions_PropertyAssignment_ShouldWork()
    {
        // Arrange
        var options = new UdlCardsOptions();

        // Act
        options.DefaultTheme = "dark";
        options.EnableCaching = true;
        options.CacheExpirationMinutes = 30;
        options.MaxCardsPerPage = 20;
        options.DefaultRefreshInterval = 5000;
        options.StrictMode = true;
        options.EnablePermissionControl = false;
        options.DebugMode = true;
        options.ApiBaseUrl = "https://api.example.com";

        // Assert
        options.DefaultTheme.Should().Be("dark");
        options.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(30);
        options.MaxCardsPerPage.Should().Be(20);
        options.DefaultRefreshInterval.Should().Be(5000);
        options.StrictMode.Should().BeTrue();
        options.EnablePermissionControl.Should().BeFalse();
        options.DebugMode.Should().BeTrue();
        options.ApiBaseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    public void UdlCardsOptions_ComplexConfigObjects_ShouldBeSettable()
    {
        // Arrange
        var options = new UdlCardsOptions();
        var pageConfig = new Dictionary<string, object>
        {
            ["className"] = "custom-page",
            ["style"] = new { backgroundColor = "#f0f0f0" }
        };
        var layoutConfig = new Dictionary<string, object>
        {
            ["type"] = "grid",
            ["columns"] = 3
        };
        var dashboardConfig = new Dictionary<string, object>
        {
            ["toolbar"] = new { show = true },
            ["filters"] = new { collapsible = true }
        };

        // Act
        options.PageConfig = pageConfig;
        options.LayoutConfig = layoutConfig;
        options.DashboardConfig = dashboardConfig;

        // Assert
        options.PageConfig.Should().BeSameAs(pageConfig);
        options.PageConfig["className"].Should().Be("custom-page");
        
        options.LayoutConfig.Should().BeSameAs(layoutConfig);
        options.LayoutConfig["type"].Should().Be("grid");
        options.LayoutConfig["columns"].Should().Be(3);
        
        options.DashboardConfig.Should().BeSameAs(dashboardConfig);
        options.DashboardConfig.Should().ContainKey("toolbar");
        options.DashboardConfig.Should().ContainKey("filters");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("primary")]
    [InlineData("dark")]
    [InlineData("light")]
    [InlineData("")]
    [InlineData(null!)]  // C# 允许给 string 属性设置 null
    public void UdlCardsOptions_DefaultTheme_ShouldAcceptValidValues(string theme)
    {
        // Arrange
        var options = new UdlCardsOptions();

        // Act
        options.DefaultTheme = theme;

        // Assert  
        options.DefaultTheme.Should().Be(theme);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(0)]
    [InlineData(-1)] // 允许负值，但在实际使用中可能不合理
    public void UdlCardsOptions_MaxCardsPerPage_ShouldAcceptAllIntegerValues(int maxCards)
    {
        // Arrange
        var options = new UdlCardsOptions();

        // Act
        options.MaxCardsPerPage = maxCards;

        // Assert
        options.MaxCardsPerPage.Should().Be(maxCards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(30000)]
    [InlineData(-1)] // 允许负值，但在实际使用中可能不合理
    public void UdlCardsOptions_DefaultRefreshInterval_ShouldAcceptAllIntegerValues(int interval)
    {
        // Arrange
        var options = new UdlCardsOptions();

        // Act
        options.DefaultRefreshInterval = interval;

        // Assert
        options.DefaultRefreshInterval.Should().Be(interval);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(1440)] // 24小时
    [InlineData(0)]
    [InlineData(-1)] // 允许负值，但在实际使用中可能不合理
    public void UdlCardsOptions_CacheExpirationMinutes_ShouldAcceptAllIntegerValues(int minutes)
    {
        // Arrange
        var options = new UdlCardsOptions();

        // Act
        options.CacheExpirationMinutes = minutes;

        // Assert
        options.CacheExpirationMinutes.Should().Be(minutes);
    }
} 