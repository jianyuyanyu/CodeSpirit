using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Amis.Tests;

/// <summary>
/// LocalizedDescriptionAttribute 单元测试
/// </summary>
public class LocalizedDescriptionAttributeTests
{
    /// <summary>
    /// 测试类，用于测试特性
    /// </summary>
    public class TestDto
    {
        [LocalizedDescription(
            "回退描述",
            ResourceKey = "Test.Description",
            ResourceType = typeof(TestResources)
        )]
        public string PropertyWithResource { get; set; }

        [LocalizedDescription("仅回退描述")]
        public string PropertyWithFallbackOnly { get; set; }

        [LocalizedDescription(
            ResourceKey = "Test.Description",
            ResourceType = typeof(TestResources)
        )]
        public string PropertyWithoutFallback { get; set; }

        [Description("标准描述")]
        public string PropertyWithStandardDescription { get; set; }
    }

    /// <summary>
    /// 测试资源类（模拟）
    /// </summary>
    public class TestResources
    {
        // 注意：这是一个占位类，实际测试中可能需要真实的资源文件
    }

    [Fact]
    public void LocalizedDescription_WithFallbackOnly_ShouldReturnFallback()
    {
        // Arrange
        var property = typeof(TestDto).GetProperty(nameof(TestDto.PropertyWithFallbackOnly));
        var attribute = property.GetCustomAttribute<LocalizedDescriptionAttribute>();

        // Act
        var description = attribute.Description;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("仅回退描述", description);
    }

    [Fact]
    public void LocalizedDescription_ShouldInheritFromDescriptionAttribute()
    {
        // Arrange & Act
        var attribute = new LocalizedDescriptionAttribute("测试描述");

        // Assert
        Assert.IsAssignableFrom<DescriptionAttribute>(attribute);
        Assert.Equal("测试描述", attribute.Description);
    }

    [Fact]
    public void LocalizedDescription_ShouldSupportResourceKeyAndResourceType()
    {
        // Arrange
        var attribute = new LocalizedDescriptionAttribute("回退")
        {
            ResourceKey = "Test.Key",
            ResourceType = typeof(TestResources)
        };

        // Act & Assert
        Assert.Equal("Test.Key", attribute.ResourceKey);
        Assert.Equal(typeof(TestResources), attribute.ResourceType);
        Assert.NotNull(attribute.Description);
    }

    [Fact]
    public void LocalizedDescription_ShouldCacheDescription()
    {
        // Arrange
        var attribute = new LocalizedDescriptionAttribute("测试描述");

        // Act
        var description1 = attribute.Description;
        var description2 = attribute.Description;

        // Assert
        Assert.Equal(description1, description2);
    }

    [Fact]
    public void LocalizedDescription_WithResourceKey_ShouldReturnFallbackWhenResourceNotFound()
    {
        // Arrange
        var property = typeof(TestDto).GetProperty(nameof(TestDto.PropertyWithResource));
        var attribute = property.GetCustomAttribute<LocalizedDescriptionAttribute>();

        // Act
        var description = attribute.Description;

        // Assert
        Assert.NotNull(attribute);
        // 当资源文件不存在时，应该返回回退文本
        Assert.Equal("回退描述", description);
    }
}
