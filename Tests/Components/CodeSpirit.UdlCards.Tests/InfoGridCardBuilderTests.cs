using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// InfoGridCardBuilder 单元测试
/// </summary>
public class InfoGridCardBuilderTests
{
    private readonly InfoGridCardBuilder _builder;

    public InfoGridCardBuilderTests()
    {
        _builder = new InfoGridCardBuilder(NullLogger<InfoGridCardBuilder>.Instance);
    }

    [Fact]
    public void CardType_ShouldReturnInfoGrid()
    {
        // Act & Assert
        _builder.CardType.Should().Be("info-grid");
    }

    [Fact]
    public void Build_WithMinimalConfig_ShouldReturnBasicCard()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格"
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("info-grid");
        result["id"].Should().Be("test-grid");
        result["className"].Should().Be("amis-cards-info-grid");
    }

    [Fact]
    public void Build_WithGridConfig_ShouldIncludeGridProperties()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Grid = new InfoGridConfig
            {
                Columns = 3,
                Gap = "16px",
                Responsive = true
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("grid");
        var grid = result["grid"] as Dictionary<string, object>;
        grid.Should().NotBeNull();
        grid!["columns"].Should().Be(3);
        grid["gap"].Should().Be("16px");
        grid["responsive"].Should().Be(true);
    }

    [Fact]
    public void Build_WithItems_ShouldIncludeItemsProperties()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Items = new List<InfoGridItem>
            {
                new()
                {
                    Title = "CPU使用率",
                    Value = "85%",
                    Icon = new InfoGridIconConfig { Name = "fa-microchip" },
                    Theme = "success",
                    Highlight = false
                },
                new()
                {
                    Title = "内存使用",
                    Value = "60%",
                    Icon = new InfoGridIconConfig { Name = "fa-memory" },
                    Theme = "warning",
                    Highlight = true
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("items");
        var items = result["items"] as List<Dictionary<string, object>>;
        items.Should().NotBeNull();
        items!.Count.Should().Be(2);
        
        items[0]["title"].Should().Be("CPU使用率");
        items[0]["value"].Should().Be("85%");
        items[0]["icon"].Should().Be("fa-microchip");
        items[0]["theme"].Should().Be("success");
        items[0]["highlight"].Should().Be(false);
        
        items[1]["title"].Should().Be("内存使用");
        items[1]["value"].Should().Be("60%");
        items[1]["icon"].Should().Be("fa-memory");
        items[1]["theme"].Should().Be("warning");
        items[1]["highlight"].Should().Be(true);
    }

    [Fact]
    public void Build_WithNullIcon_ShouldHandleGracefully()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Items = new List<InfoGridItem>
            {
                new()
                {
                    Title = "测试项",
                    Value = "100",
                    Icon = null,
                    Theme = null
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("items");
        var items = result["items"] as List<Dictionary<string, object>>;
        items![0]["icon"].Should().Be("");
        items[0]["theme"].Should().Be("");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Items = new List<InfoGridItem>
            {
                new() { Title = "测试", Value = "100" }
            }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyItems_ShouldReturnFalse()
    {
        // Arrange
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Items = new List<InfoGridItem>()
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
        var config = new InfoGridCardConfig
        {
            Id = "test-grid",
            Title = "测试网格",
            Items = new List<InfoGridItem>
            {
                new() { Title = "测试", Value = "100" }
            }
        };

        // Act
        var result = builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("info-grid");
    }

    [Fact]
    public void IUdlCardBuilderBase_Build_WithWrongType_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new StatCardConfig
        {
            Id = "wrong-type",
            Title = "错误类型"
        };

        // Act & Assert
        Action act = () => builder.Build(config);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*配置类型不匹配*");
    }

    [Fact]
    public void IUdlCardBuilderBase_Validate_WithWrongType_ShouldReturnFalse()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new StatCardConfig
        {
            Id = "wrong-type",
            Title = "错误类型"
        };

        // Act
        var result = builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }
} 