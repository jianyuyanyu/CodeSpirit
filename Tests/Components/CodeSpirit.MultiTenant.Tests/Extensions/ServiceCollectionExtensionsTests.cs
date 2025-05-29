using CodeSpirit.MultiTenant.Extensions;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.MultiTenant.Tests.Extensions;

/// <summary>
/// 服务注册扩展单元测试
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// 测试添加多租户服务
    /// </summary>
    [Fact]
    public void AddCodeSpiritMultiTenant_ShouldRegisterServices_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MultiTenant:Enabled"] = "true",
                ["MultiTenant:DefaultTenantId"] = "default",
                ["MultiTenant:StoreType"] = "Memory"
            })
            .Build();

        // 添加必要的基础服务
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");

        // Act
        services.AddCodeSpiritMultiTenant(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // 验证核心服务已注册
        serviceProvider.GetService<ITenantResolver>().Should().NotBeNull();
        serviceProvider.GetService<ITenantStore>().Should().NotBeNull();
        
        // 验证配置选项已注册
        var options = serviceProvider.GetService<IOptions<TenantOptions>>();
        options.Should().NotBeNull();
        options!.Value.Enabled.Should().BeTrue();
        options.Value.DefaultTenantId.Should().Be("default");
    }

    /// <summary>
    /// 测试内存存储类型注册
    /// </summary>
    [Fact]
    public void AddCodeSpiritMultiTenant_ShouldRegisterMemoryStore_WhenStoreTypeIsMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MultiTenant:StoreType"] = "Memory"
            })
            .Build();

        // 添加必要的基础服务
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");

        // Act
        services.AddCodeSpiritMultiTenant(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tenantStore = serviceProvider.GetService<ITenantStore>();
        
        tenantStore.Should().NotBeNull();
        tenantStore.Should().BeOfType<MemoryTenantStore>();
    }

    /// <summary>
    /// 测试默认配置
    /// </summary>
    [Fact]
    public void AddCodeSpiritMultiTenant_ShouldUseDefaultOptions_WhenNoConfigurationProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // 添加必要的基础服务
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");

        // Act
        services.AddCodeSpiritMultiTenant(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<TenantOptions>>();
        
        options.Should().NotBeNull();
        options!.Value.Enabled.Should().BeTrue();
        options.Value.DefaultTenantId.Should().Be("default");
        options.Value.StoreType.Should().Be(TenantStoreType.Database);
    }

    /// <summary>
    /// 测试服务生命周期
    /// </summary>
    [Fact]
    public void AddCodeSpiritMultiTenant_ShouldRegisterServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // 添加必要的基础服务
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options => options.Configuration = "localhost");

        // Act
        services.AddCodeSpiritMultiTenant(configuration);

        // Assert
        var tenantResolverDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITenantResolver));
        tenantResolverDescriptor.Should().NotBeNull();
        tenantResolverDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var tenantStoreDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITenantStore));
        tenantStoreDescriptor.Should().NotBeNull();
        tenantStoreDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
} 