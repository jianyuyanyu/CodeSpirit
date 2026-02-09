using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Helpers;
using Xunit;

namespace CodeSpirit.Amis.Tests;

/// <summary>
/// StatusMappingHelper 单元测试
/// </summary>
public class StatusMappingHelperTests
{
    [Fact]
    public void GetStatusValue_YesNo_True_ReturnsInfo()
    {
        // Arrange
        var value = true;
        
        // Act
        var result = StatusMappingHelper.GetStatusValue(value, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal("info", result);
    }
    
    [Fact]
    public void GetStatusValue_YesNo_False_ReturnsDefault()
    {
        // Arrange
        var value = false;
        
        // Act
        var result = StatusMappingHelper.GetStatusValue(value, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal("default", result);
    }
    
    [Fact]
    public void GetStatusValue_YesNo_Null_ReturnsDefault()
    {
        // Arrange
        object? value = null;
        
        // Act
        var result = StatusMappingHelper.GetStatusValue(value, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal("default", result);
    }
    
    [Theory]
    [InlineData("true", "info")]
    [InlineData("false", "default")]
    [InlineData("yes", "info")]
    [InlineData("no", "default")]
    [InlineData("是", "info")]
    [InlineData("否", "default")]
    [InlineData("1", "info")]
    [InlineData("0", "default")]
    public void GetStatusValue_YesNo_StringValue_ReturnsMappedStatus(string input, string expected)
    {
        // Arrange & Act
        var result = StatusMappingHelper.GetStatusValue(input, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void GetStatusValue_YesNo_InvalidString_ReturnsDefault()
    {
        // Arrange
        var value = "invalid";
        
        // Act
        var result = StatusMappingHelper.GetStatusValue(value, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal("default", result);
    }
    
    [Fact]
    public void GetStatusValue_YesNo_EmptyString_ReturnsDefault()
    {
        // Arrange
        var value = "";
        
        // Act
        var result = StatusMappingHelper.GetStatusValue(value, StatusMapping.YesNo);
        
        // Assert
        Assert.Equal("default", result);
    }
}
