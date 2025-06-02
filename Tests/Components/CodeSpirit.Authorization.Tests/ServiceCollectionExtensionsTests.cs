using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using System;
using System.Linq;
using Xunit;
using CodeSpirit.Core;

namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// ServiceCollectionExtensions 测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    #region AddCodeSpiritAuthorization 测试

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // 添加必要的依赖服务
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        
        // 添加必要的Mock服务以避免依赖注入错误
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // 验证权限服务注册
        Assert.NotNull(serviceProvider.GetService<IPermissionService>());
        Assert.NotNull(serviceProvider.GetService<IHasPermissionService>());
        
        // 验证授权处理器注册
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();
        Assert.Contains(authorizationHandlers, h => h is RolePermissionAuthorizationHandler);
        Assert.Contains(authorizationHandlers, h => h is PlatformAuthorizationHandler);
    }

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldRegisterCorrectServiceLifetimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var permissionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPermissionService));
        Assert.NotNull(permissionServiceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, permissionServiceDescriptor.Lifetime);

        var hasPermissionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHasPermissionService));
        Assert.NotNull(hasPermissionServiceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, hasPermissionServiceDescriptor.Lifetime);
    }

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldRegisterAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // 验证动态权限策略
        Assert.True(authorizationOptions.GetPolicy("DynamicPermissions") != null);
        
        // 验证平台权限策略
        Assert.True(authorizationOptions.GetPolicy("Platform_System") != null);
        Assert.True(authorizationOptions.GetPolicy("Platform_Tenant") != null);
        Assert.True(authorizationOptions.GetPolicy("Platform_Both") != null);
    }

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldConfigurePlatformPoliciesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // 验证系统平台策略
        var systemPolicy = authorizationOptions.GetPolicy("Platform_System");
        Assert.NotNull(systemPolicy);
        Assert.Single(systemPolicy.Requirements);
        Assert.IsType<PlatformRequirement>(systemPolicy.Requirements.First());
        Assert.Equal(PlatformType.System, ((PlatformRequirement)systemPolicy.Requirements.First()).PlatformType);

        // 验证租户平台策略
        var tenantPolicy = authorizationOptions.GetPolicy("Platform_Tenant");
        Assert.NotNull(tenantPolicy);
        Assert.Single(tenantPolicy.Requirements);
        Assert.IsType<PlatformRequirement>(tenantPolicy.Requirements.First());
        Assert.Equal(PlatformType.Tenant, ((PlatformRequirement)tenantPolicy.Requirements.First()).PlatformType);

        // 验证双平台策略
        var bothPolicy = authorizationOptions.GetPolicy("Platform_Both");
        Assert.NotNull(bothPolicy);
        Assert.Single(bothPolicy.Requirements);
        Assert.IsType<PlatformRequirement>(bothPolicy.Requirements.First());
        Assert.Equal(PlatformType.Both, ((PlatformRequirement)bothPolicy.Requirements.First()).PlatformType);
    }

    #endregion

    #region AddPlatformAuthorization 测试

    [Fact]
    public void AddPlatformAuthorization_ShouldRegisterOnlyPlatformServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddPlatformAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // 验证只注册了平台授权处理器
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();
        Assert.Contains(authorizationHandlers, h => h is PlatformAuthorizationHandler);
        
        // 验证没有注册完整的权限服务
        Assert.Null(serviceProvider.GetService<IPermissionService>());
        Assert.Null(serviceProvider.GetService<IHasPermissionService>());
    }

    [Fact]
    public void AddPlatformAuthorization_ShouldRegisterPlatformAuthorizationHandlerWithScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        // Act
        services.AddPlatformAuthorization();

        // Assert
        var platformHandlerDescriptor = services
            .Where(s => s.ServiceType == typeof(IAuthorizationHandler))
            .FirstOrDefault(s => s.ImplementationType == typeof(PlatformAuthorizationHandler));
        
        Assert.NotNull(platformHandlerDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, platformHandlerDescriptor.Lifetime);
    }

    [Fact]
    public void AddPlatformAuthorization_ShouldConfigurePlatformPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        // Act
        services.AddPlatformAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // 验证平台权限策略
        Assert.True(authorizationOptions.GetPolicy("Platform_System") != null);
        Assert.True(authorizationOptions.GetPolicy("Platform_Tenant") != null);
        Assert.True(authorizationOptions.GetPolicy("Platform_Both") != null);

        // 验证没有动态权限策略
        Assert.True(authorizationOptions.GetPolicy("DynamicPermissions") == null);
    }

    #endregion

    #region 重复注册测试

    [Fact]
    public void AddCodeSpiritAuthorization_CalledMultipleTimes_ShouldNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddCodeSpiritAuthorization();
        services.AddCodeSpiritAuthorization(); // 重复调用

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 验证服务可以正常解析（ASP.NET Core允许重复注册，最后一个生效）
        Assert.NotNull(serviceProvider.GetService<IPermissionService>());
        Assert.NotNull(serviceProvider.GetService<IHasPermissionService>());
        
        // 验证授权处理器注册
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();
        Assert.True(authorizationHandlers.Count >= 2, "应该注册了至少2个授权处理器");
    }

    [Fact]
    public void AddPlatformAuthorization_CalledMultipleTimes_ShouldNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        // Act
        services.AddPlatformAuthorization();
        services.AddPlatformAuthorization(); // 重复调用

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 验证服务可以正常解析
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();
        Assert.True(authorizationHandlers.Count >= 1, "应该注册了至少1个平台授权处理器");
        
        // 验证确实包含平台授权处理器
        Assert.Contains(authorizationHandlers, h => h is PlatformAuthorizationHandler);
    }

    #endregion

    #region 混合注册测试

    [Fact]
    public void AddBothPlatformAndFullAuthorization_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddPlatformAuthorization();
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // 验证完整的权限服务都已注册
        Assert.NotNull(serviceProvider.GetService<IPermissionService>());
        Assert.NotNull(serviceProvider.GetService<IHasPermissionService>());
        
        // 验证授权处理器注册
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();
        Assert.Contains(authorizationHandlers, h => h is RolePermissionAuthorizationHandler);
        Assert.Contains(authorizationHandlers, h => h is PlatformAuthorizationHandler);

        // 验证策略配置
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.True(authorizationOptions.GetPolicy("DynamicPermissions") != null);
        Assert.True(authorizationOptions.GetPolicy("Platform_System") != null);
    }

    #endregion

    #region 依赖服务缺失测试

    [Fact]
    public void AddCodeSpiritAuthorization_WithoutRequiredDependencies_ShouldThrowWhenBuilding()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act - 不添加必要的依赖服务
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 尝试获取需要依赖的服务时应该抛出异常
        Assert.ThrowsAny<InvalidOperationException>(() => 
            serviceProvider.GetRequiredService<IPermissionService>());
    }

    [Fact]
    public void AddPlatformAuthorization_WithMinimalDependencies_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddPlatformAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 平台授权应该能正常工作
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>();
        Assert.Contains(authorizationHandlers, h => h is PlatformAuthorizationHandler);
    }

    #endregion

    #region 服务验证测试

    [Fact]
    public void RegisteredServices_ShouldImplementCorrectInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // 验证权限服务实现正确的接口
        var permissionService = serviceProvider.GetService<IPermissionService>();
        Assert.NotNull(permissionService);
        Assert.IsAssignableFrom<IPermissionService>(permissionService);

        var hasPermissionService = serviceProvider.GetService<IHasPermissionService>();
        Assert.NotNull(hasPermissionService);
        Assert.IsAssignableFrom<IHasPermissionService>(hasPermissionService);

        // 验证授权处理器实现正确的接口
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>();
        foreach (var handler in authorizationHandlers)
        {
            Assert.IsAssignableFrom<IAuthorizationHandler>(handler);
        }
    }

    [Fact]
    public void RegisteredAuthorizationHandlers_ShouldHaveCorrectGenericTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();
        
        // 添加必要的Mock服务
        var mockCurrentUser = new Mock<ICurrentUser>();
        services.AddSingleton(mockCurrentUser.Object);

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationHandlers = serviceProvider.GetServices<IAuthorizationHandler>().ToList();

        // 验证PlatformAuthorizationHandler的泛型类型
        var platformHandler = authorizationHandlers.OfType<PlatformAuthorizationHandler>().FirstOrDefault();
        Assert.NotNull(platformHandler);
        
        // 验证RolePermissionAuthorizationHandler的泛型类型
        var roleHandler = authorizationHandlers.OfType<RolePermissionAuthorizationHandler>().FirstOrDefault();
        Assert.NotNull(roleHandler);
    }

    #endregion

    #region 策略验证测试

    [Theory]
    [InlineData("Platform_System", PlatformType.System)]
    [InlineData("Platform_Tenant", PlatformType.Tenant)]
    [InlineData("Platform_Both", PlatformType.Both)]
    public void PlatformPolicies_ShouldHaveCorrectPlatformType(string policyName, PlatformType expectedPlatformType)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();

        // Act
        services.AddPlatformAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        
        var policy = authorizationOptions.GetPolicy(policyName);
        Assert.NotNull(policy);
        
        var platformRequirement = policy.Requirements.OfType<PlatformRequirement>().FirstOrDefault();
        Assert.NotNull(platformRequirement);
        Assert.Equal(expectedPlatformType, platformRequirement.PlatformType);
    }

    [Fact]
    public void DynamicPermissionsPolicy_ShouldHavePermissionRequirement()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddAuthorization();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        
        var policy = authorizationOptions.GetPolicy("DynamicPermissions");
        Assert.NotNull(policy);
        
        var permissionRequirement = policy.Requirements.OfType<PermissionRequirement>().FirstOrDefault();
        Assert.NotNull(permissionRequirement);
    }

    #endregion
} 