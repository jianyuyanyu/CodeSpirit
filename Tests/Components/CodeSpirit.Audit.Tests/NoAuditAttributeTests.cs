using System.Reflection;
using CodeSpirit.Audit.Attributes;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CodeSpirit.Audit.Tests;

/// <summary>
/// NoAuditAttribute 特性测试
/// </summary>
public class NoAuditAttributeTests
{
    /// <summary>
    /// 测试 NoAuditAttribute 特性的基本功能
    /// </summary>
    [Fact]
    public void NoAuditAttribute_ShouldBeApplicableToClassAndMethod()
    {
        // Arrange & Act
        var attribute = new NoAuditAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(string.Empty, attribute.Reason);
    }

    /// <summary>
    /// 测试 NoAuditAttribute 特性带原因的构造函数
    /// </summary>
    [Fact]
    public void NoAuditAttribute_WithReason_ShouldSetReason()
    {
        // Arrange
        var reason = "测试原因";

        // Act
        var attribute = new NoAuditAttribute(reason);

        // Assert
        Assert.Equal(reason, attribute.Reason);
    }

    /// <summary>
    /// 测试 NoAuditAttribute 特性可以应用到控制器类上
    /// </summary>
    [Fact]
    public void NoAuditAttribute_CanBeAppliedToController()
    {
        // Arrange
        var controllerType = typeof(TestNoAuditController);

        // Act
        var attribute = controllerType.GetCustomAttribute<NoAuditAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("测试控制器不需要审计", attribute.Reason);
    }

    /// <summary>
    /// 测试 NoAuditAttribute 特性可以应用到方法上
    /// </summary>
    [Fact]
    public void NoAuditAttribute_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.NoAuditAction));

        // Act
        var attribute = methodInfo?.GetCustomAttribute<NoAuditAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("此方法不需要审计", attribute.Reason);
    }

    /// <summary>
    /// 测试没有 NoAuditAttribute 特性的方法
    /// </summary>
    [Fact]
    public void NoAuditAttribute_NotAppliedToMethod_ShouldReturnNull()
    {
        // Arrange
        var methodInfo = typeof(TestController).GetMethod(nameof(TestController.NormalAction));

        // Act
        var attribute = methodInfo?.GetCustomAttribute<NoAuditAttribute>();

        // Assert
        Assert.Null(attribute);
    }
}

/// <summary>
/// 测试用的控制器（整个控制器禁用审计）
/// </summary>
[NoAudit("测试控制器不需要审计")]
public class TestNoAuditController : ControllerBase
{
    /// <summary>
    /// 测试方法
    /// </summary>
    /// <returns></returns>
    public IActionResult TestAction()
    {
        return Ok();
    }
}

/// <summary>
/// 测试用的控制器（部分方法禁用审计）
/// </summary>
public class TestController : ControllerBase
{
    /// <summary>
    /// 禁用审计的方法
    /// </summary>
    /// <returns></returns>
    [NoAudit("此方法不需要审计")]
    public IActionResult NoAuditAction()
    {
        return Ok();
    }

    /// <summary>
    /// 正常的方法
    /// </summary>
    /// <returns></returns>
    public IActionResult NormalAction()
    {
        return Ok();
    }
}
