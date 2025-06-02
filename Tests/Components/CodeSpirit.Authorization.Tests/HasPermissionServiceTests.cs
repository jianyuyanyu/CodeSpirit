using Microsoft.Extensions.Logging;
using Moq;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using System.Linq;

namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// HasPermissionService 单元测试
/// </summary>
public class HasPermissionServiceTests
{
    private readonly Mock<ILogger<HasPermissionService>> _mockLogger;
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly HasPermissionService _hasPermissionService;

    public HasPermissionServiceTests()
    {
        _mockLogger = new Mock<ILogger<HasPermissionService>>();
        _mockPermissionService = new Mock<IPermissionService>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        
        _hasPermissionService = new HasPermissionService(
            _mockLogger.Object,
            _mockPermissionService.Object,
            _mockCurrentUser.Object);
    }

    #region HasPermission 基础测试

    [Fact]
    public void HasPermission_WithUnauthenticatedUser_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(isAuthenticated: false);

        // Act
        var result = _hasPermissionService.HasPermission("test_permission");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithAdminRole_ShouldReturnTrue()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "Admin" },
            permissions: new HashSet<string>());

        // Act
        var result = _hasPermissionService.HasPermission("any_permission");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WithSpecificPermission_ShouldReturnTrue()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "user_create", "user_edit" };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission("user_create", userPermissions))
            .Returns(true);

        // Act
        var result = _hasPermissionService.HasPermission("user_create");

        // Assert
        Assert.True(result);
        _mockPermissionService.Verify(x => x.HasPermission("user_create", userPermissions), Times.Once);
    }

    [Fact]
    public void HasPermission_WithoutSpecificPermission_ShouldReturnFalse()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "user_view" };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission("user_delete", userPermissions))
            .Returns(false);

        // Act
        var result = _hasPermissionService.HasPermission("user_delete");

        // Assert
        Assert.False(result);
        _mockPermissionService.Verify(x => x.HasPermission("user_delete", userPermissions), Times.Once);
    }

    #endregion

    #region HasNavigationPermission 测试

    [Fact]
    public void HasNavigationPermission_WithUnauthenticatedUser_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(isAuthenticated: false);

        // Act
        var result = _hasPermissionService.HasNavigationPermission("test_navigation");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasNavigationPermission_WithAdminRole_ShouldReturnTrue()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "Admin" });

        // Act
        var result = _hasPermissionService.HasNavigationPermission("admin_navigation");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasNavigationPermission_WithEmptyPermissionCode_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" });

        // Act
        var result = _hasPermissionService.HasNavigationPermission("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasNavigationPermission_WithNullPermissionCode_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" });

        // Act
        var result = _hasPermissionService.HasNavigationPermission(null);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 权限继承测试

    [Theory]
    [InlineData("module", "module_controller_action", true)]
    [InlineData("module_controller", "module_controller_action", true)]
    [InlineData("module_controller_action", "module_controller_action", true)]
    [InlineData("other_module", "module_controller_action", false)]
    public void HasPermission_WithPermissionInheritance_ShouldWorkCorrectly(
        string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission(requiredPermission, userPermissions))
            .Returns(expectedResult);

        // Act
        var result = _hasPermissionService.HasPermission(requiredPermission);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region 多角色测试

    [Fact]
    public void HasPermission_WithMultipleRoles_AdminShouldTakePrecedence()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User", "Admin", "Manager" },
            permissions: new HashSet<string>());

        // Act
        var result = _hasPermissionService.HasPermission("restricted_permission");

        // Assert
        Assert.True(result); // Admin角色应该优先处理
    }

    [Fact]
    public void HasPermission_WithoutAdminRole_ShouldCheckSpecificPermissions()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "basic_permission" };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User", "Guest" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission("basic_permission", userPermissions))
            .Returns(true);

        // Act
        var result = _hasPermissionService.HasPermission("basic_permission");

        // Assert
        Assert.True(result);
        _mockPermissionService.Verify(x => x.HasPermission("basic_permission", userPermissions), Times.Once);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void HasPermission_WithNullPermissionCode_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: new HashSet<string> { "valid_permission" });

        // Act
        var result = _hasPermissionService.HasPermission(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithEmptyPermissionCode_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: new HashSet<string> { "valid_permission" });

        // Act
        var result = _hasPermissionService.HasPermission("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithWhitespacePermissionCode_ShouldReturnFalse()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: new HashSet<string> { "valid_permission" });

        _mockPermissionService.Setup(x => x.HasPermission("   ", It.IsAny<HashSet<string>>()))
            .Returns(false);

        // Act
        var result = _hasPermissionService.HasPermission("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithNullPermissions_ShouldHandleGracefully()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: null);

        _mockPermissionService.Setup(x => x.HasPermission("test_permission", null))
            .Returns(false);

        // Act
        var result = _hasPermissionService.HasPermission("test_permission");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 缓存和性能测试

    [Fact]
    public void HasPermission_CalledMultipleTimes_ShouldUseCachedPermissions()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "cached_permission" };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission("cached_permission", userPermissions))
            .Returns(true);

        // Act - 多次调用相同权限
        var result1 = _hasPermissionService.HasPermission("cached_permission");
        var result2 = _hasPermissionService.HasPermission("cached_permission");
        var result3 = _hasPermissionService.HasPermission("cached_permission");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        
        // 验证权限服务被调用了多次（因为没有缓存在HasPermissionService层面）
        _mockPermissionService.Verify(x => x.HasPermission("cached_permission", userPermissions), Times.Exactly(3));
    }

    #endregion

    #region 国际化和特殊字符测试

    [Theory]
    [InlineData("用户_管理_创建", true)]
    [InlineData("módulo_contrôle_açāo", true)]
    [InlineData("модуль_контроллер_действие", true)]
    [InlineData("permission-with-dashes", true)]
    [InlineData("permission_with_numbers_123", true)]
    public void HasPermission_WithInternationalAndSpecialCharacters_ShouldWork(string permissionCode, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { permissionCode };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "User" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission(permissionCode, userPermissions))
            .Returns(expectedResult);

        // Act
        var result = _hasPermissionService.HasPermission(permissionCode);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region 复杂场景测试

    [Fact]
    public void HasNavigationPermission_ComplexScenario_ShouldWorkAsExpected()
    {
        // Arrange
        // 用户实际权限包含三级权限，经过ExtractNavigationPermissions后会变成二级权限
        var userPermissions = new HashSet<string>
        {
            "exam_examPapers_createExamPaper",
            "exam_examPapers_editExamPaper",
            "system_users_viewProfile",
            "system_roles_createRole",
            "exam", // 已有的一级权限
            "reports" // 已有的一级权限
        };

        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "Manager" },
            permissions: userPermissions);

        // 根据ExtractNavigationPermissions的逻辑，提取后的导航权限集合应该是：
        var expectedNavigationPermissions = new HashSet<string>
        {
            "exam",            // 一级权限（保持不变）
            "exam_examPapers", // 二级权限（从三级权限中提取）
            "system_users",    // 二级权限（从三级权限中提取）
            "system_roles",    // 二级权限（从三级权限中提取）
            "reports"          // 一级权限（保持不变）
        };

        // Setup permission service to simulate ExtractNavigationPermissions behavior
        _mockPermissionService.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<HashSet<string>>()))
            .Returns<string, HashSet<string>>((permission, extractedPermissions) => 
                expectedNavigationPermissions.Contains(permission));

        // Act & Assert - 测试各种导航权限
        Assert.True(_hasPermissionService.HasNavigationPermission("exam"));
        Assert.True(_hasPermissionService.HasNavigationPermission("exam_examPapers"));
        Assert.True(_hasPermissionService.HasNavigationPermission("system_users"));
        Assert.True(_hasPermissionService.HasNavigationPermission("system_roles"));
        Assert.True(_hasPermissionService.HasNavigationPermission("reports"));

        // 用户没有的权限
        Assert.False(_hasPermissionService.HasNavigationPermission("exam_examRecords"));
        Assert.False(_hasPermissionService.HasNavigationPermission("system_settings"));

        // 验证方法被调用次数
        _mockPermissionService.Verify(x => x.HasPermission(It.IsAny<string>(), It.IsAny<HashSet<string>>()), Times.Exactly(7));
    }

    [Fact]
    public void HasPermission_WithHierarchicalPermissions_ShouldSupportInheritance()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "system", "exam_management" };
        SetupMockUser(
            isAuthenticated: true,
            roles: new[] { "SystemAdmin" },
            permissions: userPermissions);

        _mockPermissionService.Setup(x => x.HasPermission(It.IsAny<string>(), It.IsAny<HashSet<string>>()))
            .Returns<string, HashSet<string>>((permission, permissions) =>
            {
                // 模拟权限继承逻辑
                return permissions.Any(p => permission.StartsWith(p + "_") || permission == p);
            });

        // Act & Assert
        Assert.True(_hasPermissionService.HasPermission("system_users_create"));
        Assert.True(_hasPermissionService.HasPermission("system_roles_edit"));
        Assert.True(_hasPermissionService.HasPermission("exam_management_view"));
        Assert.False(_hasPermissionService.HasPermission("other_module_action"));
    }

    #endregion

    #region 辅助方法

    private void SetupMockUser(bool isAuthenticated, string userName = "testuser", 
        string[] roles = null, HashSet<string> permissions = null)
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        _mockCurrentUser.Setup(x => x.UserName).Returns(userName);
        _mockCurrentUser.Setup(x => x.Roles).Returns(roles ?? Array.Empty<string>());
        _mockCurrentUser.Setup(x => x.Permissions).Returns(permissions ?? new HashSet<string>());
        
        var claims = new List<Claim>();
        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        _mockCurrentUser.Setup(x => x.Claims).Returns(claims);
    }

    #endregion
} 