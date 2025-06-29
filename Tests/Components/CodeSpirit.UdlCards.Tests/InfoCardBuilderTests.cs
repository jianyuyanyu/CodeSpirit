using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// InfoCardBuilder 单元测试
/// </summary>
public class InfoCardBuilderTests
{
    private readonly InfoCardBuilder _builder;

    public InfoCardBuilderTests()
    {
        _builder = new InfoCardBuilder(NullLogger<InfoCardBuilder>.Instance);
    }

    [Fact]
    public void CardType_ShouldReturnInfo()
    {
        // Act & Assert
        _builder.CardType.Should().Be("info");
    }

    [Fact]
    public void Build_WithMinimalConfig_ShouldReturnBasicCard()
    {
        // Arrange
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息"
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("info");
        result["id"].Should().Be("test-info");
        result["className"].Should().Be("amis-cards-info");
    }

    [Fact]
    public void Build_WithContent_ShouldIncludeContent()
    {
        // Arrange
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息",
            Content = new InfoContentConfig
            {
                Type = "text",
                Text = "这是一段测试信息内容"
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("body");
        var body = result["body"] as Dictionary<string, object>;
        body["type"].Should().Be("tpl");
        body["tpl"].Should().Be("这是一段测试信息内容");
    }

    [Fact]
    public void Build_WithPropertyItems_ShouldIncludePropertyItems()
    {
        // Arrange
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息",
            Content = new InfoContentConfig
            {
                Type = "properties",
                PropertyItems = new List<InfoPropertyItem>
                {
                    new() { Name = "username", Label = "用户名", Value = "张三", ValueType = "text" },
                    new() { Name = "email", Label = "邮箱", Value = "zhangsan@example.com", ValueType = "text" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("body");
        var body = result["body"] as Dictionary<string, object>;
        body["type"].Should().Be("property");
        body.Should().ContainKey("items");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息",
            Content = new InfoContentConfig
            {
                Type = "text",
                Text = "测试内容"
            }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldReturnFalse()
    {
        // Arrange
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息",
            Content = new InfoContentConfig
            {
                Type = ""  // 空的 Type
            }
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
        var config = new InfoCardConfig
        {
            Id = "test-info",
            Title = "测试信息",
            Content = new InfoContentConfig
            {
                Type = "text",
                Text = "测试内容"
            }
        };

        // Act
        var result = builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("info");
    }
} 