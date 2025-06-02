using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using System.Linq;

namespace CodeSpirit.Authorization.Tests.Extensions;

/// <summary>
/// 服务集合扩展测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCodeSpiritAuthorization_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert - 验证服务注册
        Assert.Contains(services, s => s.ServiceType == typeof(IPermissionService));
        Assert.Contains(services, s => s.ServiceType == typeof(IHasPermissionService));
        Assert.Contains(services, s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(RolePermissionAuthorizationHandler));
        Assert.Contains(services, s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(PlatformAuthorizationHandler));
    }

    [Fact]
    public void AddPlatformAuthorization_ShouldRegisterPlatformServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformAuthorization();

        // Assert - 验证平台授权处理器注册
        Assert.Contains(services, s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(PlatformAuthorizationHandler));
    }

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldRegisterServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCodeSpiritAuthorization();

        // Assert
        var permissionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPermissionService));
        Assert.NotNull(permissionServiceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, permissionServiceDescriptor.Lifetime);

        var hasPermissionServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHasPermissionService));
        Assert.NotNull(hasPermissionServiceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, hasPermissionServiceDescriptor.Lifetime);

        var platformHandlerDescriptor = services.FirstOrDefault(s => 
            s.ServiceType == typeof(IAuthorizationHandler) && 
            s.ImplementationType == typeof(PlatformAuthorizationHandler));
        Assert.NotNull(platformHandlerDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, platformHandlerDescriptor.Lifetime);
    }

    [Fact]
    public void AddPlatformAuthorization_ShouldNotRegisterRolePermissionServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPlatformAuthorization();

        // Assert
        // 验证不应该注册角色权限相关服务
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IPermissionService));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IHasPermissionService));

        // 但应该注册平台权限处理器
        Assert.Contains(services, s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(PlatformAuthorizationHandler));
    }

    [Fact]
    public void AddCodeSpiritAuthorization_ShouldAllowMultipleRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - 多次注册不应该导致异常
        services.AddCodeSpiritAuthorization();
        services.AddCodeSpiritAuthorization();

        // Assert - 应该仍然包含必要的服务注册
        Assert.Contains(services, s => s.ServiceType == typeof(IPermissionService));
    }

    [Fact]
    public void AddPlatformAuthorization_ShouldAllowMultipleRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - 多次注册不应该导致异常
        services.AddPlatformAuthorization();
        services.AddPlatformAuthorization();

        // Assert - 应该仍然包含平台权限处理器
        Assert.Contains(services, s => s.ServiceType == typeof(IAuthorizationHandler) && s.ImplementationType == typeof(PlatformAuthorizationHandler));
    }

    [Fact]
    public void PlatformAttribute_ShouldSetCorrectPolicy()
    {
        // Arrange & Act
        var systemAttribute = new PlatformAttribute(PlatformType.System);
        var tenantAttribute = new PlatformAttribute(PlatformType.Tenant);
        var bothAttribute = new PlatformAttribute(PlatformType.Both);

        // Assert
        Assert.Equal("Platform_System", systemAttribute.Policy);
        Assert.Equal("Platform_Tenant", tenantAttribute.Policy);
        Assert.Equal("Platform_Both", bothAttribute.Policy);
    }
} 