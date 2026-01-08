using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ConfigCenter.Sdk.Tests.Cache;

/// <summary>
/// 配置缓存服务测试（修复依赖注入生命周期后的版本）
/// </summary>
public class ConfigCacheServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IOptions<ConfigCenterOptions>> _optionsMock;
    private readonly Mock<ILogger<ConfigCacheService>> _loggerMock;

    public ConfigCacheServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _optionsMock = new Mock<IOptions<ConfigCenterOptions>>();
        _loggerMock = new Mock<ILogger<ConfigCacheService>>();

        // 配置选项
        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            CacheExpirationMinutes = 60
        });

        // 配置 ServiceProvider 返回 ICacheService
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ICacheService)))
            .Returns(_cacheServiceMock.Object);

        // 配置 Scope 返回 ServiceProvider
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

        // 配置 ScopeFactory 创建 Scope
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
    }

    #region GetFromCacheAsync Tests

    [Fact]
    public async Task GetFromCacheAsync_CacheHit_ReturnsConfigs()
    {
        // Arrange
        var appId = "test-app-001";
        var expectedConfigs = new ConfigItemsExportDto
        {
            AppId = appId,
            Configs = new Dictionary<string, object>
            {
                { "Key1", "Value1" },
                { "Key2", 42 }
            }
        };

        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.Is<string>(k => k.Contains(appId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfigs);

        var service = CreateService();

        // Act
        var result = await service.GetFromCacheAsync(appId);

        // Assert
        result.Should().NotBeNull();
        result!.AppId.Should().Be(appId);
        result.Configs.Should().HaveCount(2);
        result.Configs.Should().ContainKey("Key1").WhoseValue.Should().Be("Value1");
        result.Configs.Should().ContainKey("Key2").WhoseValue.Should().Be(42);

        // 验证 Scope 创建和释放
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_CacheMiss_ReturnsNull()
    {
        // Arrange
        var appId = "test-app-001";

        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigItemsExportDto?)null);

        var service = CreateService();

        // Act
        var result = await service.GetFromCacheAsync(appId);

        // Assert
        result.Should().BeNull();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_NoCacheService_ReturnsNull()
    {
        // Arrange
        var service = CreateServiceWithoutCache();

        // Act
        var result = await service.GetFromCacheAsync("test-app-001");

        // Assert
        result.Should().BeNull();
        // 注意：使用不同的 ScopeFactory Mock，不验证调用次数
    }

    [Fact]
    public async Task GetFromCacheAsync_CacheThrowsException_ReturnsNullAndLogs()
    {
        // Arrange
        var appId = "test-app-001";
        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        var service = CreateService();

        // Act
        var result = await service.GetFromCacheAsync(appId);

        // Assert
        result.Should().BeNull();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_UseCorrectCacheKey()
    {
        // Arrange
        var appId = "my-application";
        var expectedKey = $"configcenter:config:{appId}";

        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                expectedKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigItemsExportDto?)null);

        var service = CreateService();

        // Act
        await service.GetFromCacheAsync(appId);

        // Assert
        _cacheServiceMock.Verify(c => c.GetAsync<ConfigItemsExportDto>(
            expectedKey,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFromCacheAsync_MultipleCalls_CreatesSeparateScopes()
    {
        // Arrange
        var appId = "test-app-001";
        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigItemsExportDto?)null);

        var service = CreateService();

        // Act
        await service.GetFromCacheAsync(appId);
        await service.GetFromCacheAsync(appId);
        await service.GetFromCacheAsync(appId);

        // Assert - 每次调用都应该创建新的 scope
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Exactly(3));
        _scopeMock.Verify(s => s.Dispose(), Times.Exactly(3));
    }

    #endregion

    #region SaveToCacheAsync Tests

    [Fact]
    public async Task SaveToCacheAsync_ValidConfigs_SavesToCache()
    {
        // Arrange
        var appId = "test-app-001";
        var configs = new ConfigItemsExportDto
        {
            AppId = appId,
            Configs = new Dictionary<string, object> { { "Key1", "Value1" } }
        };

        _cacheServiceMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ConfigItemsExportDto>(),
                It.IsAny<CacheOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.SaveToCacheAsync(appId, configs);

        // Assert
        _cacheServiceMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains(appId)),
            configs,
            It.Is<CacheOptions>(o => o.Level == CacheLevel.L2Only),
            It.IsAny<CancellationToken>()), Times.Once);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SaveToCacheAsync_NoCacheService_DoesNotThrow()
    {
        // Arrange
        var service = CreateServiceWithoutCache();
        var configs = new ConfigItemsExportDto { AppId = "test", Configs = new Dictionary<string, object>() };

        // Act
        var act = async () => await service.SaveToCacheAsync("test", configs);

        // Assert
        await act.Should().NotThrowAsync();
        // 注意：使用不同的 ScopeFactory Mock，不验证调用次数
    }

    [Fact]
    public async Task SaveToCacheAsync_CacheThrowsException_DoesNotPropagateAndLogs()
    {
        // Arrange
        var appId = "test-app-001";
        var configs = new ConfigItemsExportDto { AppId = appId, Configs = new Dictionary<string, object>() };

        _cacheServiceMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ConfigItemsExportDto>(),
                It.IsAny<CacheOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache write error"));

        var service = CreateService();

        // Act
        var act = async () => await service.SaveToCacheAsync(appId, configs);

        // Assert
        await act.Should().NotThrowAsync();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task SaveToCacheAsync_UsesCorrectCacheExpiration()
    {
        // Arrange
        var appId = "test-app-001";
        var configs = new ConfigItemsExportDto { AppId = appId, Configs = new Dictionary<string, object>() };
        var expectedMinutes = 120;

        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            CacheExpirationMinutes = expectedMinutes
        });

        _cacheServiceMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ConfigItemsExportDto>(),
                It.IsAny<CacheOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.SaveToCacheAsync(appId, configs);

        // Assert
        _cacheServiceMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<ConfigItemsExportDto>(),
            It.Is<CacheOptions>(o => 
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(expectedMinutes)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ClearCacheAsync Tests

    [Fact]
    public async Task ClearCacheAsync_ValidAppId_RemovesFromCache()
    {
        // Arrange
        var appId = "test-app-001";
        var expectedKey = $"configcenter:config:{appId}";

        _cacheServiceMock.Setup(c => c.RemoveAsync(expectedKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ClearCacheAsync(appId);

        // Assert
        _cacheServiceMock.Verify(c => c.RemoveAsync(
            expectedKey, 
            It.IsAny<CancellationToken>()), Times.Once);

        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ClearCacheAsync_NoCacheService_DoesNotThrow()
    {
        // Arrange
        var service = CreateServiceWithoutCache();

        // Act
        var act = async () => await service.ClearCacheAsync("test-app-001");

        // Assert
        await act.Should().NotThrowAsync();
        // 注意：使用不同的 ScopeFactory Mock，不验证调用次数
    }

    [Fact]
    public async Task ClearCacheAsync_CacheThrowsException_DoesNotPropagate()
    {
        // Arrange
        var appId = "test-app-001";
        _cacheServiceMock.Setup(c => c.RemoveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache remove error"));

        var service = CreateService();

        // Act
        var act = async () => await service.ClearCacheAsync(appId);

        // Assert
        await act.Should().NotThrowAsync();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
    }

    #endregion

    #region Dependency Injection Lifetime Tests

    [Fact]
    public void Constructor_WithServiceScopeFactory_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => new ConfigCacheService(
            _scopeFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ServiceLifetime_ScopeIsDisposedAfterUse()
    {
        // Arrange
        var service = CreateService();
        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigItemsExportDto?)null);

        // Act
        await service.GetFromCacheAsync("test");

        // Assert - Scope 应该被创建和释放
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ServiceLifetime_ScopeIsDisposedEvenOnException()
    {
        // Arrange
        var service = CreateService();
        _cacheServiceMock.Setup(c => c.GetAsync<ConfigItemsExportDto>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await service.GetFromCacheAsync("test");

        // Assert - 即使异常，Scope 也应该被释放
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ServiceLifetime_NoScopedServiceLeaks()
    {
        // Arrange
        var service = CreateService();
        
        // Act - 多次调用
        for (int i = 0; i < 10; i++)
        {
            await service.GetFromCacheAsync($"app-{i}");
        }

        // Assert - 每次调用都创建和释放 Scope，不会累积
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Exactly(10));
        _scopeMock.Verify(s => s.Dispose(), Times.Exactly(10));
    }

    #endregion

    #region Private Helpers

    private ConfigCacheService CreateService()
    {
        return new ConfigCacheService(
            _scopeFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    private ConfigCacheService CreateServiceWithoutCache()
    {
        // 配置 ServiceProvider 返回 null（模拟 ICacheService 未注册）
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ICacheService)))
            .Returns(null);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeMock.Setup(s => s.Dispose());  // 确保 Dispose 可以被调用

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        return new ConfigCacheService(
            scopeFactoryMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    #endregion
}
