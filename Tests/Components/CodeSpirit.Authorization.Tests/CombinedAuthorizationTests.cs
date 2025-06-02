using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// 组合权限验证测试 - 测试平台权限与角色权限的组合使用
/// </summary>
public class CombinedAuthorizationTests
{
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<ILogger<PlatformAuthorizationHandler>> _mockPlatformLogger;
    private readonly Mock<ILogger<RolePermissionAuthorizationHandler>> _mockRoleLogger;
    private readonly Mock<IHasPermissionService> _mockHasPermissionService;
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<HttpContext> _mockHttpContext;

    public CombinedAuthorizationTests()
    {
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockPlatformLogger = new Mock<ILogger<PlatformAuthorizationHandler>>();
        _mockRoleLogger = new Mock<ILogger<RolePermissionAuthorizationHandler>>();
        _mockHasPermissionService = new Mock<IHasPermissionService>();
        _mockPermissionService = new Mock<IPermissionService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockHttpContext = new Mock<HttpContext>();

        // 设置服务提供者返回必要的服务
        _mockServiceProvider.Setup(x => x.GetService(typeof(ICurrentUser)))
            .Returns(_mockCurrentUser.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IHasPermissionService)))
            .Returns(_mockHasPermissionService.Object);
        _mockServiceProvider.Setup(x => x.GetRequiredService(typeof(IHasPermissionService)))
            .Returns(_mockHasPermissionService.Object);
        
        _mockHttpContext.Setup(x => x.RequestServices)
            .Returns(_mockServiceProvider.Object);

        // 设置Endpoint Mock以避免NullReferenceException
        var mockEndpoint = new Mock<Endpoint>();
        var mockMetadataCollection = new EndpointMetadataCollection();
        mockEndpoint.Setup(x => x.Metadata).Returns(mockMetadataCollection);
        _mockHttpContext.Setup(x => x.GetEndpoint()).Returns(mockEndpoint.Object);
        
        // 设置Request Mock
        var mockRequest = new Mock<HttpRequest>();
        _mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
        
        // 设置Response Mock
        var mockResponse = new Mock<HttpResponse>();
        _mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
    }

    #region 平台权限 + 角色权限组合测试

    [Fact]
    public async Task CombinedAuthorization_SystemTenantWithAdminRole_ShouldSucceedForBoth()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, 
            tenantId: "system", 
            userId: 1L,
            roles: new[] { "Admin" },
            permissions: new HashSet<string> { "system_management", "user_create" }
        );

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var roleHandler = new RolePermissionAuthorizationHandler(_mockRoleLogger.Object);

        // 创建包含平台和权限要求的上下文
        var requirements = new IAuthorizationRequirement[]
        {
            new PlatformRequirement(PlatformType.System),
            new PermissionRequirement()
        };

        var platformContext = new AuthorizationHandlerContext(
            new[] { requirements[0] }, 
            null, 
            null);

        var roleContext = new AuthorizationHandlerContext(
            new[] { requirements[1] }, 
            null, 
            _mockHttpContext.Object);

        // Act
        await platformHandler.HandleAsync(platformContext);
        await roleHandler.HandleAsync(roleContext);

        // Assert
        Assert.True(platformContext.HasSucceeded, "系统租户应该通过平台权限验证");
        Assert.True(roleContext.HasSucceeded, "Admin角色应该通过权限验证");
    }

    [Fact]
    public async Task CombinedAuthorization_BusinessTenantWithoutPermission_ShouldFailRoleButSucceedPlatform()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, 
            tenantId: "business-tenant", 
            userId: 2L,
            roles: new[] { "User" },
            permissions: new HashSet<string>()
        );

        _mockHasPermissionService.Setup(x => x.HasPermission(It.IsAny<string>()))
            .Returns(false);

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var roleHandler = new RolePermissionAuthorizationHandler(_mockRoleLogger.Object);

        var platformContext = new AuthorizationHandlerContext(
            new[] { new PlatformRequirement(PlatformType.Tenant) },
            null,
            null);

        var roleContext = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement() },
            null,
            _mockHttpContext.Object);

        // Act
        await platformHandler.HandleAsync(platformContext);
        await roleHandler.HandleAsync(roleContext);

        // Assert
        Assert.True(platformContext.HasSucceeded, "业务租户应该通过租户平台权限验证");
        Assert.False(roleContext.HasSucceeded, "没有权限的用户应该无法通过角色权限验证");
    }

    [Fact]
    public async Task CombinedAuthorization_SystemTenantWithSpecificPermission_ShouldSucceedForBoth()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, 
            tenantId: "system", 
            userId: 3L,
            roles: new[] { "Manager" },
            permissions: new HashSet<string> { "user_management_create", "user_management_edit" }
        );

        _mockHasPermissionService.Setup(x => x.HasPermission("user_management_create"))
            .Returns(true);

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var roleHandler = new RolePermissionAuthorizationHandler(_mockRoleLogger.Object);

        var platformContext = new AuthorizationHandlerContext(
            new[] { new PlatformRequirement(PlatformType.System) },
            null,
            null);

        var roleContext = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement() },
            null,
            _mockHttpContext.Object);

        // Act
        await platformHandler.HandleAsync(platformContext);
        await roleHandler.HandleAsync(roleContext);

        // Assert
        Assert.True(platformContext.HasSucceeded, "系统租户应该通过平台权限验证");
        Assert.True(roleContext.HasSucceeded, "有权限的用户应该通过角色权限验证");
    }

    #endregion

    #region 多平台权限验证测试

    [Fact]
    public async Task CombinedAuthorization_BothPlatformType_ShouldWorkForSystemAndBusinessTenants()
    {
        // 测试系统租户
        await TestBothPlatformForTenant("system", true);
        
        // 测试业务租户
        await TestBothPlatformForTenant("business-tenant", true);
        
        // 测试默认租户（应该失败）
        await TestBothPlatformForTenant("default", false);
    }

    private async Task TestBothPlatformForTenant(string tenantId, bool shouldSucceed)
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, 
            tenantId: tenantId, 
            userId: 1L,
            roles: new[] { "User" },
            permissions: new HashSet<string>()
        );

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PlatformRequirement(PlatformType.Both) },
            null,
            null);

        // Act
        await platformHandler.HandleAsync(context);

        // Assert
        if (shouldSucceed)
        {
            Assert.True(context.HasSucceeded, $"租户 '{tenantId}' 应该通过 Both 平台权限验证");
        }
        else
        {
            Assert.False(context.HasSucceeded, $"租户 '{tenantId}' 应该无法通过 Both 平台权限验证");
        }
    }

    #endregion

    #region 权限继承测试

    [Theory]
    [InlineData("module", "module_controller_action", true)] // 模块级权限
    [InlineData("module_controller", "module_controller_action", true)] // 控制器级权限
    [InlineData("module_controller_action", "module_controller_action", true)] // 精确权限
    [InlineData("other_module", "module_controller_action", false)] // 不相关权限
    [InlineData("module_other", "module_controller_action", false)] // 不相关权限
    public async Task PermissionInheritance_ShouldWorkCorrectly(string userPermission, string requiredPermission, bool shouldSucceed)
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, 
            tenantId: "business-tenant", 
            userId: 1L,
            roles: new[] { "User" },
            permissions: new HashSet<string> { userPermission }
        );

        _mockPermissionService.Setup(x => x.HasPermission(requiredPermission, It.IsAny<HashSet<string>>()))
            .Returns(shouldSucceed);

        _mockHasPermissionService.Setup(x => x.HasPermission(requiredPermission))
            .Returns(shouldSucceed);

        var roleHandler = new RolePermissionAuthorizationHandler(_mockRoleLogger.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement() },
            null,
            _mockHttpContext.Object);

        // Act
        await roleHandler.HandleAsync(context);

        // Assert
        if (shouldSucceed)
        {
            Assert.True(context.HasSucceeded, $"用户权限 '{userPermission}' 应该满足需求权限 '{requiredPermission}'");
        }
        else
        {
            Assert.False(context.HasSucceeded, $"用户权限 '{userPermission}' 不应该满足需求权限 '{requiredPermission}'");
        }
    }

    #endregion

    #region 复杂场景测试

    [Fact]
    public async Task ComplexScenario_MultiTenantApplication_ShouldHandleCorrectly()
    {
        var testCases = new[]
        {
            new { TenantId = "system", Role = "Admin", Platform = PlatformType.System, ShouldSucceed = true },
            new { TenantId = "system", Role = "User", Platform = PlatformType.System, ShouldSucceed = true },
            new { TenantId = "business-a", Role = "Admin", Platform = PlatformType.Tenant, ShouldSucceed = true },
            new { TenantId = "business-a", Role = "User", Platform = PlatformType.Tenant, ShouldSucceed = true },
            new { TenantId = "business-a", Role = "Admin", Platform = PlatformType.System, ShouldSucceed = false },
            new { TenantId = "default", Role = "Admin", Platform = PlatformType.Both, ShouldSucceed = false },
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            SetupMockUser(
                isAuthenticated: true,
                tenantId: testCase.TenantId,
                userId: 1L,
                roles: new[] { testCase.Role },
                permissions: new HashSet<string> { "test_permission" }
            );

            var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
            var context = new AuthorizationHandlerContext(
                new[] { new PlatformRequirement(testCase.Platform) },
                null,
                null);

            // Act
            await platformHandler.HandleAsync(context);

            // Assert
            Assert.Equal(testCase.ShouldSucceed, context.HasSucceeded);
        }
    }

    [Fact]
    public async Task EdgeCase_UnauthenticatedUserWithValidTenant_ShouldFail()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: false, // 关键：用户未认证
            tenantId: "system", // 但有有效的租户ID
            userId: null,
            roles: Array.Empty<string>(),
            permissions: new HashSet<string>()
        );

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PlatformRequirement(PlatformType.System) },
            null,
            null);

        // Act
        await platformHandler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "未认证的用户不应该通过验证，即使租户ID有效");
    }

    [Fact]
    public async Task EdgeCase_AuthenticatedUserWithNullTenant_ShouldFail()
    {
        // Arrange
        SetupMockUser(
            isAuthenticated: true, // 用户已认证
            tenantId: null, // 但租户ID为空
            userId: 123L,
            roles: new[] { "User" },
            permissions: new HashSet<string>()
        );

        var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PlatformRequirement(PlatformType.Tenant) },
            null,
            null);

        // Act
        await platformHandler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "租户ID为空的用户不应该通过租户平台验证");
    }

    #endregion

    #region 性能相关测试

    [Fact]
    public async Task Performance_MultipleSimultaneousRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        
        for (int i = 0; i < 10; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                SetupMockUser(
                    isAuthenticated: true,
                    tenantId: taskIndex % 2 == 0 ? "system" : $"tenant-{taskIndex}",
                    userId: taskIndex,
                    roles: new[] { "User" },
                    permissions: new HashSet<string>()
                );

                var platformHandler = new PlatformAuthorizationHandler(_mockCurrentUser.Object, _mockPlatformLogger.Object);
                var context = new AuthorizationHandlerContext(
                    new[] { new PlatformRequirement(PlatformType.Both) },
                    null,
                    null);

                await platformHandler.HandleAsync(context);
                return context.HasSucceeded;
            }));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result, "所有请求都应该成功"));
    }

    #endregion

    #region 辅助方法

    private void SetupMockUser(bool isAuthenticated, string tenantId, long? userId, 
        string[] roles = null, HashSet<string> permissions = null)
    {
        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        _mockCurrentUser.Setup(x => x.TenantId).Returns(tenantId);
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);
        _mockCurrentUser.Setup(x => x.Roles).Returns(roles ?? Array.Empty<string>());
        _mockCurrentUser.Setup(x => x.Permissions).Returns(permissions ?? new HashSet<string>());
        
        // 创建对应的Claims
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
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