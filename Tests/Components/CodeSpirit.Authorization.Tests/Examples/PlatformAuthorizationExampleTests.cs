using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;

namespace CodeSpirit.Authorization.Tests.Examples;

/// <summary>
/// 平台权限验证使用示例控制器
/// </summary>
[DisplayName("平台权限示例")]
public class PlatformAuthorizationExampleController : ControllerBase
{
    /// <summary>
    /// 系统管理功能 - 只有系统租户用户可以访问
    /// </summary>
    [HttpGet("system-management")]
    [Platform(PlatformType.System)]
    [DisplayName("系统管理")]
    public IActionResult SystemManagement()
    {
        return Ok(new { message = "这是系统管理功能，只有系统租户用户可以访问" });
    }

    /// <summary>
    /// 租户业务功能 - 只有业务租户用户可以访问
    /// </summary>
    [HttpGet("tenant-business")]
    [Platform(PlatformType.Tenant)]
    [DisplayName("租户业务")]
    public IActionResult TenantBusiness()
    {
        return Ok(new { message = "这是租户业务功能，只有业务租户用户可以访问" });
    }

    /// <summary>
    /// 通用功能 - 系统租户和业务租户用户都可以访问
    /// </summary>
    [HttpGet("common-feature")]
    [Platform(PlatformType.Both)]
    [DisplayName("通用功能")]
    public IActionResult CommonFeature()
    {
        return Ok(new { message = "这是通用功能，系统租户和业务租户用户都可以访问" });
    }

    /// <summary>
    /// 组合权限验证示例 - 既需要平台权限，也需要具体的业务权限
    /// </summary>
    [HttpPost("advanced-operation")]
    [Platform(PlatformType.Tenant)]
    [Permission(Name = "tenant_advanced_operation", DisplayName = "租户高级操作")]
    [DisplayName("高级操作")]
    public IActionResult AdvancedOperation()
    {
        return Ok(new { message = "这是高级操作，需要同时满足租户平台权限和具体业务权限" });
    }

    /// <summary>
    /// 系统用户管理 - 系统租户专用功能
    /// </summary>
    [HttpGet("system-users")]
    [Platform(PlatformType.System)]
    [Permission(Name = "system_user_management", DisplayName = "系统用户管理")]
    [DisplayName("系统用户管理")]
    public IActionResult SystemUserManagement()
    {
        return Ok(new { message = "系统用户管理功能，只有系统管理员可以访问" });
    }

    /// <summary>
    /// 租户数据统计 - 业务租户专用功能
    /// </summary>
    [HttpGet("tenant-statistics")]
    [Platform(PlatformType.Tenant)]
    [Permission(Name = "tenant_statistics", DisplayName = "租户数据统计")]
    [DisplayName("租户数据统计")]
    public IActionResult TenantStatistics()
    {
        return Ok(new { message = "租户数据统计功能，只有业务租户用户可以访问" });
    }

    /// <summary>
    /// 审计日志查看 - 通用功能但需要权限
    /// </summary>
    [HttpGet("audit-logs")]
    [Platform(PlatformType.Both)]
    [Permission(Name = "audit_log_view", DisplayName = "审计日志查看")]
    [DisplayName("审计日志查看")]
    public IActionResult ViewAuditLogs()
    {
        return Ok(new { message = "审计日志查看功能，系统和业务用户都可以访问，但需要相应权限" });
    }

    /// <summary>
    /// 公开API - 无权限要求
    /// </summary>
    [HttpGet("public-info")]
    [DisplayName("公开信息")]
    public IActionResult PublicInfo()
    {
        return Ok(new { message = "这是公开信息，无需任何权限验证" });
    }
}

/// <summary>
/// 平台权限验证示例控制器的单元测试
/// </summary>
public class PlatformAuthorizationExampleControllerTests
{
    private readonly PlatformAuthorizationExampleController _controller;

    public PlatformAuthorizationExampleControllerTests()
    {
        _controller = new PlatformAuthorizationExampleController();
    }

    [Fact]
    public void SystemManagement_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.SystemManagement();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void TenantBusiness_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.TenantBusiness();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void CommonFeature_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.CommonFeature();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void AdvancedOperation_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.AdvancedOperation();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void SystemUserManagement_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.SystemUserManagement();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void TenantStatistics_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.TenantStatistics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void ViewAuditLogs_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.ViewAuditLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public void PublicInfo_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.PublicInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }
} 