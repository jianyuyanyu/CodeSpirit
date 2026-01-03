using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Tests.Examples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;
using Xunit;

namespace CodeSpirit.Amis.Tests;

/// <summary>
/// AmisSwitchFieldFactory 单元测试
/// </summary>
public class AmisSwitchFieldFactoryTests
{
    private readonly AmisSwitchFieldFactory _factory;
    private readonly UtilityHelper _utilityHelper;

    public AmisSwitchFieldFactoryTests()
    {
        _factory = new AmisSwitchFieldFactory();
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<CultureResolver>();
        _utilityHelper = new UtilityHelper(new CultureResolver(httpContextAccessor, logger));
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForAmisSwitchFieldAttribute()
    {
        // Arrange
        var attributeType = typeof(AmisSwitchFieldAttribute);

        // Act
        var result = _factory.CanHandle(attributeType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForOtherAttributes()
    {
        // Arrange
        var attributeType = typeof(DisplayNameAttribute);

        // Act
        var result = _factory.CanHandle(attributeType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CreateField_ShouldReturnBasicSwitchField()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.BasicSwitch));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("switch", result["type"]?.ToString());
        Assert.Equal("basicSwitch", result["name"]?.ToString());
        Assert.Equal("基本开关", result["label"]?.ToString());
    }

    [Fact]
    public void CreateField_ShouldSetDefaultValue()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.SwitchWithDefault));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(true, result["value"]?.ToObject<bool>());
    }

    [Fact]
    public void CreateField_ShouldSetCustomText()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.SwitchWithCustomText));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("开启", result["onText"]?.ToString());
        Assert.Equal("关闭", result["offText"]?.ToString());
    }

    [Fact]
    public void CreateField_ShouldSetCustomValues()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.SwitchWithCustomValue));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result["trueValue"]?.ToObject<int>());
        Assert.Equal(0, result["falseValue"]?.ToObject<int>());
    }

    [Fact]
    public void CreateField_ShouldSetSize()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.SmallSwitch));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("sm", result["size"]?.ToString());
    }

    [Fact]
    public void CreateField_ShouldSetDisabled()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.DisabledSwitch));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(true, result["disabled"]?.ToObject<bool>());
    }

    [Fact]
    public void CreateField_ShouldSetStatic()
    {
        // Arrange
        var prop = typeof(SwitchFieldExample).GetProperty(nameof(SwitchFieldExample.StaticSwitch));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(true, result["static"]?.ToObject<bool>());
    }

    [Fact]
    public void CreateField_ShouldReturnNull_WhenNoSwitchAttribute()
    {
        // Arrange
        var prop = typeof(TestClass).GetProperty(nameof(TestClass.PropertyWithoutAttribute));

        // Act
        var result = _factory.CreateField(prop, _utilityHelper);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试类，用于测试没有特性的属性
    /// </summary>
    private class TestClass
    {
        public bool PropertyWithoutAttribute { get; set; }
    }
} 