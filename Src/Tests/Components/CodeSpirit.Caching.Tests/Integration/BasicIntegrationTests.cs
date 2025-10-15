using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeSpirit.Caching.Tests.Integration;

/// <summary>
/// 基础集成测试
/// </summary>
public class BasicIntegrationTests
{
    [Fact]
    public void CachingOptions_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var options = new CachingOptions();

        // Assert
        options.EnableL1Cache.Should().BeTrue();
        options.EnableL2Cache.Should().BeTrue();
        options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
        options.KeyPrefix.Should().Be("CodeSpirit:Cache:");
    }

    [Fact]
    public void CacheOptions_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var options = new CacheOptions();

        // Assert
        options.Level.Should().Be(CacheLevel.Both);
        options.Priority.Should().Be(CachePriority.Normal);
        options.EnableBreakthroughProtection.Should().BeTrue();
    }

    [Fact]
    public void CacheKeyGenerator_ShouldGenerateValidKeys()
    {
        // Arrange
        var cachingOptions = new CachingOptions
        {
            KeyPrefix = "TestApp:"
        };
        var generator = new CacheKeyGenerator(Options.Create(cachingOptions));

        // Act
        var key = generator.GenerateKey("user", 123, "profile");

        // Assert
        key.Should().StartWith("TestApp:");
        key.Should().Contain("user");
        key.Should().Contain("123");
        key.Should().Contain("profile");
    }

    [Fact]
    public void CacheWarmupItem_ShouldCreateSuccessfully()
    {
        // Arrange
        var key = "test:key";
        var factory = async () => await Task.FromResult("test value");
        var options = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        // Act
        var item = CacheWarmupItem.Create(key, factory, options);

        // Assert
        item.Should().NotBeNull();
        item.Key.Should().Be(key);
        item.Factory.Should().NotBeNull();
        item.Options.Should().Be(options);
    }

    [Fact]
    public async Task CacheWarmupItem_FactoryShouldBeInvokable()
    {
        // Arrange
        var expectedValue = "test value";
        var factory = async () => await Task.FromResult(expectedValue);
        var item = CacheWarmupItem.Create("test:key", factory);

        // Act
        var result = await item.Factory();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(CacheLevel.L1Only)]
    [InlineData(CacheLevel.L2Only)]
    [InlineData(CacheLevel.Both)]
    [InlineData(CacheLevel.Auto)]
    public void CacheLevel_ShouldAcceptAllValues(CacheLevel level)
    {
        // Arrange
        var options = new CacheOptions();

        // Act
        options.Level = level;

        // Assert
        options.Level.Should().Be(level);
    }

    [Theory]
    [InlineData(CachePriority.Low)]
    [InlineData(CachePriority.Normal)]
    [InlineData(CachePriority.High)]
    [InlineData(CachePriority.NeverRemove)]
    public void CachePriority_ShouldAcceptAllValues(CachePriority priority)
    {
        // Arrange
        var options = new CacheOptions();

        // Act
        options.Priority = priority;

        // Assert
        options.Priority.Should().Be(priority);
    }

    [Fact]
    public void CachingOptions_Validate_ShouldReturnTrueForValidConfig()
    {
        // Arrange
        var options = new CachingOptions
        {
            EnableL1Cache = true,
            EnableL2Cache = true,
            DefaultExpiration = TimeSpan.FromMinutes(30),
            DefaultL1Expiration = TimeSpan.FromMinutes(5),
            DefaultL2Expiration = TimeSpan.FromMinutes(30)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CachingOptions_Validate_ShouldReturnFalseWhenBothCachesDisabled()
    {
        // Arrange
        var options = new CachingOptions
        {
            EnableL1Cache = false,
            EnableL2Cache = false
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CacheKeyGenerator_ShouldGenerateUserKey()
    {
        // Arrange
        var cachingOptions = new CachingOptions { KeyPrefix = "App:" };
        var generator = new CacheKeyGenerator(Options.Create(cachingOptions));

        // Act
        var key = generator.GenerateUserKey(123, "cart");

        // Assert
        key.Should().StartWith("App:");
        key.Should().Contain("123");
        key.Should().Contain("cart");
    }

    [Fact]
    public void CacheKeyGenerator_ShouldGenerateTenantKey()
    {
        // Arrange
        var cachingOptions = new CachingOptions { KeyPrefix = "App:" };
        var generator = new CacheKeyGenerator(Options.Create(cachingOptions));

        // Act
        var key = generator.GenerateTenantKey("tenant1", "settings");

        // Assert
        key.Should().StartWith("App:");
        key.Should().Contain("tenant1");
        key.Should().Contain("settings");
    }

    [Fact]
    public void RedisDistributedLockOptions_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var options = new RedisDistributedLockOptions();

        // Assert
        options.DefaultLockTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.KeyPrefix.Should().Be("CodeSpirit:Cache:Lock:");
    }

    [Fact]
    public async Task CacheWarmupService_ShouldHandleFactoryCall()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        var mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        var cachingOptions = new CachingOptions();
        var mockLogger = new Mock<ILogger<CacheWarmupService>>();
        
        var key = "test:key";
        var fullKey = "data:test:key";
        var statusKey = "warmup:test:key";
        
        // 设置mock返回值
        mockKeyGenerator
            .Setup(x => x.GenerateKey("data", key))
            .Returns(fullKey);
        mockKeyGenerator
            .Setup(x => x.GenerateKey("warmup", key))
            .Returns(statusKey);
        mockCacheService
            .Setup(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        var warmupService = new CacheWarmupService(
            mockCacheService.Object,
            mockKeyGenerator.Object,
            Options.Create(cachingOptions),
            mockLogger.Object);

        var value = "test value";
        var factory = async () => await Task.FromResult(value);

        // Act
        await warmupService.WarmupAsync(key, factory);

        // Assert
        mockCacheService.Verify(
            x => x.SetAsync(key, value, It.IsAny<CacheOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

