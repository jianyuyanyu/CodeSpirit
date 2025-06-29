using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// StatCardBuilder 单元测试
/// </summary>
public class StatCardBuilderTests
{
    private readonly StatCardBuilder _builder;

    public StatCardBuilderTests()
    {
        _builder = new StatCardBuilder(NullLogger<StatCardBuilder>.Instance);
    }

    [Fact]
    public void CardType_ShouldReturnStat()
    {
        // Act & Assert
        _builder.CardType.Should().Be("stat");
    }

    [Fact]
    public void Build_WithMinimalConfig_ShouldReturnBasicCard()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig 
            { 
                Value = 100, 
                Label = "测试数据" 
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("stat");
        result["id"].Should().Be("test-stat");
        result["className"].Should().Be("amis-cards-stat");
        result.Should().ContainKey("data");

        var data = result["data"] as Dictionary<string, object>;
        data.Should().NotBeNull();
        data["value"].Should().Be(100);
        data["label"].Should().Be("测试数据");
    }

    [Fact]
    public void Build_WithCompleteDataConfig_ShouldIncludeAllDataProperties()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig
            {
                Value = 1250.75m,
                Label = "用户总数",
                Unit = "人",
                Prefix = "约",
                Suffix = "名用户",
                Formatter = "number",
                DecimalPlaces = 2,
                ShowSeparator = true,
                ApiUrl = "/api/stats/users",
                FieldMapping = new Dictionary<string, string> 
                { 
                    ["count"] = "value", 
                    ["name"] = "label" 
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        var data = result["data"] as Dictionary<string, object>;
        data["value"].Should().Be(1250.75m);
        data["label"].Should().Be("用户总数");
        data["unit"].Should().Be("人");
        data["prefix"].Should().Be("约");
        data["suffix"].Should().Be("名用户");
        data["formatter"].Should().Be("number");
        data["decimalPlaces"].Should().Be(2);
        data["showSeparator"].Should().Be(true);
        data["fieldMapping"].Should().BeEquivalentTo(new Dictionary<string, string> 
        { 
            ["count"] = "value", 
            ["name"] = "label" 
        });

        result.Should().ContainKey("api");
        var api = result["api"] as Dictionary<string, object>;
        api["method"].Should().Be("get");
        api["url"].Should().Be("/api/stats/users");
    }

    [Fact]
    public void Build_WithIconConfig_ShouldIncludeIconProperties()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Icon = new StatIconConfig
            {
                Name = "fa-users",
                Position = "left",
                Size = "lg",
                Color = "#1890ff",
                BackgroundColor = "#f0f9ff",
                ShowBorder = true,
                BorderColor = "#d1ecf1",
                Style = new Dictionary<string, object> { ["borderRadius"] = "8px" }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("icon");
        var icon = result["icon"] as Dictionary<string, object>;
        icon["name"].Should().Be("fa-users");
        icon["position"].Should().Be("left");
        icon["size"].Should().Be("lg");
        icon["color"].Should().Be("#1890ff");
        icon["backgroundColor"].Should().Be("#f0f9ff");
        icon["showBorder"].Should().Be(true);
        icon["borderColor"].Should().Be("#d1ecf1");
        icon["style"].Should().BeEquivalentTo(new Dictionary<string, object> { ["borderRadius"] = "8px" });
    }

    [Fact]
    public void Build_WithTrendConfig_ShouldIncludeTrendProperties()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Trend = new StatTrendConfig
            {
                Direction = "up",
                Value = 15.5m,
                IsPercentage = true,
                Text = "较昨日增长",
                Colors = new StatTrendColorConfig
                {
                    Up = "#52c41a",
                    Down = "#ff4d4f",
                    Stable = "#faad14"
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("trend");
        var trend = result["trend"] as Dictionary<string, object>;
        trend["direction"].Should().Be("up");
        trend["value"].Should().Be(15.5m);
        trend["isPercentage"].Should().Be(true);
        trend["text"].Should().Be("较昨日增长");
        trend.Should().ContainKey("colors");
    }

    [Fact]
    public void Build_WithProgressConfig_ShouldIncludeProgressProperties()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 75, Label = "测试" },
            Progress = new StatProgressConfig
            {
                Target = 100,
                Show = true,
                Height = 8,
                ShowText = true,
                Color = "#52c41a",
                BackgroundColor = "#f6ffed"
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("progress");
        var progress = result["progress"] as Dictionary<string, object>;
        progress["target"].Should().Be(100);
        progress["show"].Should().Be(true);
        progress["height"].Should().Be(8);
        progress["showText"].Should().Be(true);
        progress["color"].Should().Be("#52c41a");
        progress["backgroundColor"].Should().Be("#f6ffed");
    }

    [Fact]
    public void Build_WithAnimationConfig_ShouldIncludeAnimationProperties()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Animation = new StatAnimationConfig
            {
                EnableValueAnimation = true,
                Duration = 3000,
                Easing = "ease-in-out",
                Delay = 500
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("animation");
        var animation = result["animation"] as Dictionary<string, object>;
        animation["enableValueAnimation"].Should().Be(true);
        animation["duration"].Should().Be(3000);
        animation["easing"].Should().Be("ease-in-out");
        animation["delay"].Should().Be(500);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Icon = new StatIconConfig { Name = "fa-users" },
            Progress = new StatProgressConfig { Target = 100 }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullDataValue_ShouldReturnFalse()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = null!, Label = "测试" }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyIconName_ShouldReturnFalse()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Icon = new StatIconConfig { Name = "" }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidProgressTarget_ShouldReturnFalse()
    {
        // Arrange
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" },
            Progress = new StatProgressConfig { Target = 0 }
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
        var config = new StatCardConfig
        {
            Id = "test-stat",
            Title = "测试统计",
            Data = new StatDataConfig { Value = 100, Label = "测试" }
        };

        // Act
        var result = builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("stat");
    }

    [Fact]
    public void IUdlCardBuilderBase_Build_WithWrongType_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表"
        };

        // Act & Assert
        Action act = () => builder.Build(config);
        act.Should().Throw<ArgumentException>()
           .WithMessage("配置类型不匹配，期望 StatCardConfig，实际 ChartCardConfig");
    }

    [Fact]
    public void IUdlCardBuilderBase_Validate_WithWrongType_ShouldReturnFalse()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new ChartCardConfig
        {
            Id = "test-chart",
            Title = "测试图表"
        };

        // Act
        var result = builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }
} 