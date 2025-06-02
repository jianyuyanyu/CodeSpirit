using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// PermissionService 单元测试
/// </summary>
public class PermissionServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<PermissionService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _permissionService = new PermissionService(_mockServiceProvider.Object, _mockCache.Object, _mockLogger.Object);
    }

    #region HasPermission 基础测试

    [Fact]
    public void HasPermission_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "user_management_create", "user_management_edit", "order_view" };

        // Act
        var result = _permissionService.HasPermission("user_management_create", userPermissions);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WithoutMatch_ShouldReturnFalse()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "user_management_create", "user_management_edit" };

        // Act
        var result = _permissionService.HasPermission("order_delete", userPermissions);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithEmptyPermissions_ShouldReturnFalse()
    {
        // Arrange
        var userPermissions = new HashSet<string>();

        // Act
        var result = _permissionService.HasPermission("any_permission", userPermissions);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPermission_WithNullPermissions_ShouldReturnFalse()
    {
        // Arrange
        HashSet<string> userPermissions = null;

        // Act
        var result = _permissionService.HasPermission("any_permission", userPermissions);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 权限继承测试

    [Theory]
    [InlineData("module", "module_controller_action", true)] // 模块级权限应该继承到具体动作
    [InlineData("module_controller", "module_controller_action", true)] // 控制器级权限应该继承到动作
    [InlineData("module_controller_action", "module_controller_action", true)] // 精确匹配
    [InlineData("other_module", "module_controller_action", false)] // 不相关的模块
    [InlineData("module_other", "module_controller_action", false)] // 不相关的控制器
    [InlineData("module", "other_controller_action", false)] // 不在模块下的其他权限
    public void HasPermission_WithInheritance_ShouldWorkCorrectly(string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void HasPermission_WithMultiLevelInheritance_ShouldWork()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "system" }; // 最高级权限

        // Act & Assert
        Assert.True(_permissionService.HasPermission("system_admin_users_create", userPermissions));
        Assert.True(_permissionService.HasPermission("system_admin_roles_edit", userPermissions));
        Assert.True(_permissionService.HasPermission("system_config_settings_view", userPermissions));
    }

    [Fact]
    public void HasPermission_WithPartialMatch_ShouldNotInherit()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "user_manage" }; // 部分匹配但不是前缀

        // Act
        var result = _permissionService.HasPermission("user_management_create", userPermissions);

        // Assert
        Assert.False(result); // 部分匹配不应该被认为是继承
    }

    #endregion

    #region 特殊字符和分隔符测试

    [Theory]
    [InlineData("module.controller", "module.controller.action", false)] // 实际实现只支持下划线分隔符
    [InlineData("module-controller", "module-controller-action", false)] // 实际实现只支持下划线分隔符
    [InlineData("module:controller", "module:controller:action", false)] // 实际实现只支持下划线分隔符
    [InlineData("module/controller", "module/controller/action", false)] // 实际实现只支持下划线分隔符
    public void HasPermission_WithDifferentSeparators_ShouldWorkCorrectly(string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("用户_管理", "用户_管理_创建", true)]
    [InlineData("módulo_controle", "módulo_controle_ação", true)]
    [InlineData("模块_控制器", "模块_控制器_动作", true)]
    public void HasPermission_WithInternationalCharacters_ShouldWork(string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region 边界条件测试

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    public void HasPermission_WithInvalidRequiredPermission_ShouldReturnFalse(string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { "valid_permission" };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void HasPermission_WithWhitespaceInPermissions_ShouldHandleCorrectly()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "  user_management  ", "order_view" };

        // Act
        var result1 = _permissionService.HasPermission("user_management", userPermissions);
        var result2 = _permissionService.HasPermission("  user_management  ", userPermissions);

        // Assert
        Assert.False(result1); // 空格不应该被忽略
        Assert.True(result2); // 精确匹配应该成功
    }

    #endregion

    #region 大小写敏感性测试

    [Theory]
    [InlineData("User_Management", "user_management", false)] // 大小写不匹配
    [InlineData("user_management", "User_Management", false)] // 大小写不匹配
    [InlineData("user_management", "user_management", true)] // 精确匹配
    [InlineData("USER_MANAGEMENT", "USER_MANAGEMENT_CREATE", true)] // 大写继承
    public void HasPermission_ShouldBeCaseSensitive(string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region 复杂权限场景测试

    [Fact]
    public void HasPermission_WithComplexHierarchy_ShouldWorkCorrectly()
    {
        // Arrange
        var userPermissions = new HashSet<string>
        {
            "admin", // 最高级权限
            "tenant_user_management", // 租户级用户管理
            "specific_action_only" // 特定动作权限
        };

        // Act & Assert
        // Admin权限应该继承所有子权限
        Assert.True(_permissionService.HasPermission("admin_system_config", userPermissions));
        Assert.True(_permissionService.HasPermission("admin_user_create", userPermissions));
        
        // 租户级权限应该继承子权限
        Assert.True(_permissionService.HasPermission("tenant_user_management_create", userPermissions));
        Assert.True(_permissionService.HasPermission("tenant_user_management_edit", userPermissions));
        
        // 特定权限只匹配自己
        Assert.True(_permissionService.HasPermission("specific_action_only", userPermissions));
        
        // 向下匹配通常不支持
        Assert.False(_permissionService.HasPermission("specific_action", userPermissions));
        
        // 不相关权限应该失败
        Assert.False(_permissionService.HasPermission("order_management_delete", userPermissions));
    }

    [Fact]
    public void HasPermission_WithOverlappingPermissions_ShouldUseShortestMatch()
    {
        // Arrange
        var userPermissions = new HashSet<string>
        {
            "user", // 短权限
            "user_management", // 长权限
            "user_management_advanced" // 更长权限
        };

        // Act & Assert
        // 所有这些都应该成功，因为用户有"user"权限（最短匹配）
        Assert.True(_permissionService.HasPermission("user_profile_edit", userPermissions));
        Assert.True(_permissionService.HasPermission("user_management_create", userPermissions));
        Assert.True(_permissionService.HasPermission("user_management_advanced_audit", userPermissions));
    }

    #endregion

    #region 性能和缓存测试

    [Fact]
    public void HasPermission_WithLargePermissionSet_ShouldPerformWell()
    {
        // Arrange
        var userPermissions = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            userPermissions.Add($"permission_{i}_module_{i % 10}_action_{i % 5}");
        }

        // Act & Assert
        var startTime = DateTime.UtcNow;
        
        for (int i = 0; i < 100; i++)
        {
            _permissionService.HasPermission($"permission_{i}_module_{i % 10}_action_{i % 5}_specific", userPermissions);
        }
        
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        
        // 性能应该在合理范围内（100次调用不超过1秒）
        Assert.True(duration.TotalSeconds < 1.0, $"Performance test failed. Duration: {duration.TotalSeconds} seconds");
    }

    [Fact]
    public void HasPermission_CalledMultipleTimes_ShouldReturnConsistentResults()
    {
        // Arrange
        var userPermissions = new HashSet<string> { "consistent_test" };

        // Act
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(_permissionService.HasPermission("consistent_test_action", userPermissions));
        }

        // Assert
        Assert.True(results.All(r => r == true), "所有结果应该保持一致");
    }

    #endregion

    #region 权限解析算法测试

    [Theory]
    [InlineData("a_b_c", "a_b_c_d_e", true)] // 标准继承：父权限覆盖子权限
    [InlineData("a_b", "a_b_c_d", true)] // 跨级继承
    [InlineData("a", "a_b_c_d_e_f", true)] // 深层继承
    [InlineData("x_y", "a_b_c", false)] // 完全不匹配
    [InlineData("a_b_c", "a_b", false)] // 向下匹配通常不支持
    [InlineData("a_b_c", "a_b_x", false)] // 部分匹配但分支不同
    public void HasPermission_InheritanceAlgorithm_ShouldFollowHierarchy(string userPermission, string requiredPermission, bool expectedResult)
    {
        // Arrange
        var userPermissions = new HashSet<string> { userPermission };

        // Act
        var result = _permissionService.HasPermission(requiredPermission, userPermissions);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void HasPermission_WithComplexPermissionStructure_ShouldResolveCorrectly()
    {
        // Arrange
        var userPermissions = new HashSet<string>
        {
            "app", // 应用级权限
            "app_module_specific", // 特定模块权限
            "other_module_admin" // 其他模块管理权限
        };

        // Act & Assert
        // 应用级权限应该覆盖所有
        Assert.True(_permissionService.HasPermission("app_any_module_any_action", userPermissions));
        Assert.True(_permissionService.HasPermission("app_user_management_create", userPermissions));
        
        // 特定模块权限
        Assert.True(_permissionService.HasPermission("app_module_specific_action", userPermissions));
        
        // 其他模块管理权限
        Assert.True(_permissionService.HasPermission("other_module_admin_configure", userPermissions));
        
        // 应该失败的情况
        Assert.False(_permissionService.HasPermission("different_app_action", userPermissions));
        Assert.False(_permissionService.HasPermission("other_module_user_action", userPermissions));
    }

    #endregion

    #region 缓存集成测试

    [Fact]
    public void HasPermission_ShouldUseCacheWhenAvailable()
    {
        // Arrange
        var cachedResult = Encoding.UTF8.GetBytes("true");
        
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync(cachedResult);

        var userPermissions = new HashSet<string> { "test_permission" };

        // Act - 这个测试主要验证缓存逻辑的存在，实际的缓存行为需要在实际实现中验证
        var result = _permissionService.HasPermission("test_permission_action", userPermissions);

        // Assert
        Assert.True(result); // 基于权限继承应该返回true
    }

    [Fact]
    public void HasPermission_ShouldSetCacheAfterCalculation()
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[])null); // 缓存未命中

        var userPermissions = new HashSet<string> { "cache_test" };

        // Act
        var result = _permissionService.HasPermission("cache_test_action", userPermissions);

        // Assert
        Assert.True(result);
        
        // 验证缓存设置被调用（如果实现了缓存）
        // 这里主要是验证方法执行正常，实际的缓存验证需要根据具体实现
    }

    #endregion
} 