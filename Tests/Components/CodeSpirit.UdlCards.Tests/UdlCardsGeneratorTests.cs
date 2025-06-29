using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using CodeSpirit.UdlCards.Extensions;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// UdlCardsGenerator 单元测试
/// </summary>
public class UdlCardsGeneratorTests
{
    private readonly UdlCardsGenerator _generator;
    private readonly UdlCardsOptions _options;

    public UdlCardsGeneratorTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUdlCards();
        
        var serviceProvider = services.BuildServiceProvider();
        _generator = serviceProvider.GetRequiredService<UdlCardsGenerator>();
        _options = serviceProvider.GetRequiredService<IOptions<UdlCardsOptions>>().Value;
    }

    #region GenerateCard Tests

    [Fact]
    public void GenerateCard_WithValidStatCard_ShouldReturnCorrectConfig()
    {
        // Arrange
        var statCard = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig 
            { 
                Value = 1250, 
                Label = "用户数", 
                Unit = "人",
                Formatter = "number"
            },
            Icon = new StatIconConfig
            {
                Name = "fa-users",
                Color = "#1890ff"
            }
        };

        // Act
        var result = _generator.GenerateCard(statCard);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("stat");
        result["id"].Should().Be("test-stat");
        result.Should().ContainKey("data");
        result.Should().ContainKey("icon");
        
        var data = result["data"] as Dictionary<string, object>;
        data.Should().NotBeNull();
        data["value"].Should().Be(1250);
        data["label"].Should().Be("用户数");
        data["unit"].Should().Be("人");
    }

    [Fact]
    public void GenerateCard_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _generator.GenerateCard(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("cardConfig");
    }

    [Fact]
    public void GenerateCard_WithUnsupportedCardType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var invalidCard = new TestInvalidCardConfig
        {
            Id = "invalid-card",
            Title = "无效卡片"
        };

        // Act & Assert
        Action act = () => _generator.GenerateCard(invalidCard);
        act.Should().Throw<NotSupportedException>()
           .WithMessage("不支持的卡片类型: invalid");
    }

    [Fact]
    public void GenerateCard_WithGlobalSettings_ShouldApplySettings()
    {
        // Arrange
        var statCard = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Theme = "primary",
            Permissions = new List<string> { "user.read" },
            Roles = new List<string> { "admin" },
            VisibleOn = "user.hasPermission('stat.view')",
            Style = new Dictionary<string, object> { ["color"] = "red" },
            ClassName = "custom-stat-card",
            Data = new StatDataConfig { Value = 100, Label = "测试" }
        };

        // Act
        var result = _generator.GenerateCard(statCard);

        // Assert
        result["theme"].Should().Be("primary");
        result["permissions"].Should().BeEquivalentTo(new[] { "user.read" });
        result["roles"].Should().BeEquivalentTo(new[] { "admin" });
        result["visibleOn"].Should().Be("user.hasPermission('stat.view')");
        result["style"].Should().BeEquivalentTo(new Dictionary<string, object> { ["color"] = "red" });
        result["className"].Should().Be("custom-stat-card");
    }

    #endregion

    #region GeneratePage Tests

    [Fact]
    public void GeneratePage_WithMultipleCards_ShouldReturnPageConfig()
    {
        // Arrange
        var cards = new List<UdlCardConfig>
        {
            new StatCardConfig
            {
                Id = "stat1",
                Title = "统计1",
                Data = new StatDataConfig { Value = 100, Label = "测试1" }
            },
            new StatCardConfig
            {
                Id = "stat2", 
                Title = "统计2",
                Data = new StatDataConfig { Value = 200, Label = "测试2" }
            }
        };

        // Act
        var result = _generator.GeneratePage(cards);

        // Assert
        result.Should().NotBeNull();
        result.Cards.Should().HaveCount(2);
        result.Cards.Should().AllSatisfy(card => card.Should().ContainKey("type"));
    }

    [Fact]
    public void GeneratePage_WithNullCards_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _generator.GeneratePage(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("cards");
    }

    [Fact]
    public void GeneratePage_WithInvalidCard_ShouldSkipInNonStrictMode()
    {
        // Arrange
        var cards = new List<UdlCardConfig>
        {
            new StatCardConfig
            {
                Id = "valid-stat",
                Title = "有效统计",
                Data = new StatDataConfig { Value = 100, Label = "测试" }
            },
            new TestInvalidCardConfig
            {
                Id = "invalid-card",
                Title = "无效卡片"
            }
        };

        // Act
        var result = _generator.GeneratePage(cards);

        // Assert
        result.Should().NotBeNull();
        result.Cards.Should().HaveCount(1, "应该跳过无效卡片");
        result.Cards[0]["id"].Should().Be("valid-stat");
    }

    [Fact]
    public void GeneratePage_WithCustomPageConfig_ShouldUseProvidedConfig()
    {
        // Arrange
        var cards = new List<UdlCardConfig>
        {
            new StatCardConfig
            {
                Id = "stat1",
                Title = "统计1",
                Data = new StatDataConfig { Value = 100, Label = "测试" }
            }
        };

        var pageConfig = new UdlPageConfig
        {
            Title = "自定义页面",
            Description = "这是一个自定义页面"
        };

        // Act
        var result = _generator.GeneratePage(cards, pageConfig);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("自定义页面");
        result.Description.Should().Be("这是一个自定义页面");
        result.Cards.Should().HaveCount(1);
    }

    #endregion

    #region GenerateDashboard Tests

    [Fact]
    public void GenerateDashboard_WithValidConfig_ShouldReturnDashboard()
    {
        // Arrange
        var dashboardConfig = new UdlDashboardConfig
        {
            Title = "测试仪表板",
            Description = "这是一个测试仪表板",
            Sections = new List<UdlDashboardSection>
            {
                new UdlDashboardSection
                {
                    Title = "区域1",
                    Cards = new List<Dictionary<string, object>>
                    {
                        new() { ["type"] = "stat", ["id"] = "stat1" }
                    }
                }
            }
        };

        // Act
        var result = _generator.GenerateDashboard(dashboardConfig);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("测试仪表板");
        result.Description.Should().Be("这是一个测试仪表板");
        result.Sections.Should().HaveCount(1);
    }

    [Fact]
    public void GenerateDashboard_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _generator.GenerateDashboard(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("dashboardConfig");
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// 用于测试的无效卡片配置类
    /// </summary>
    private class TestInvalidCardConfig : UdlCardConfig
    {
        public override string Type => "invalid";
    }

    #endregion
} 