using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeSpirit.Caching.Tests.Services;

/// <summary>
/// TTL时间一致性测试
/// 验证缓存组件在各种配置场景下，L1和L2缓存的TTL时间计算是否正确且一致
/// </summary>
public class TtlConsistencyTests
{
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<MultiLevelCacheService>> _mockLogger;
    private readonly CachingOptions _cachingOptions;

    public TtlConsistencyTests()
    {
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<MultiLevelCacheService>>();

        _cachingOptions = new CachingOptions
        {
            EnableL1Cache = true,
            EnableL2Cache = true,
            DefaultL1Expiration = TimeSpan.FromMinutes(5),
            DefaultL2Expiration = TimeSpan.FromMinutes(30),
            DefaultSlidingExpiration = TimeSpan.FromMinutes(2)
        };

        // 设置键生成器返回简单的键
        _mockKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parts) => $"{prefix}:{string.Join(":", parts)}");
    }

    #region L1缓存TTL测试

    [Fact]
    public async Task L1Cache_WithExplicitL1Expiration_ShouldNotApplyDefaultSlidingExpiration()
    {
        // Arrange
        var service = CreateService();
        var expectedExpiration = TimeSpan.FromMinutes(10);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = expectedExpiration
        };

        MemoryCacheEntryOptions? capturedOptions = null;
        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                // 捕获设置的选项
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    capturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(expectedExpiration);
        
        // 关键断言：显式设置L1Expiration时，不应应用DefaultSlidingExpiration
        capturedOptions.SlidingExpiration.Should().BeNull(
            "因为显式设置了L1Expiration，不应再应用DefaultSlidingExpiration");
    }

    [Fact]
    public async Task L1Cache_WithoutExplicitExpiration_ShouldApplyDefaultSlidingExpiration()
    {
        // Arrange
        var service = CreateService();
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only
            // 未设置任何过期时间
        };

        MemoryCacheEntryOptions? capturedOptions = null;
        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    capturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(_cachingOptions.DefaultL1Expiration);
        capturedOptions.SlidingExpiration.Should().Be(_cachingOptions.DefaultSlidingExpiration,
            "因为未显式设置L1Expiration，应应用DefaultSlidingExpiration");
    }

    [Fact]
    public async Task L1Cache_WithExplicitSlidingExpiration_ShouldUseExplicitValue()
    {
        // Arrange
        var service = CreateService();
        var explicitSlidingExpiration = TimeSpan.FromMinutes(3);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = TimeSpan.FromMinutes(10),
            SlidingExpiration = explicitSlidingExpiration
        };

        MemoryCacheEntryOptions? capturedOptions = null;
        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    capturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.SlidingExpiration.Should().Be(explicitSlidingExpiration,
            "应使用显式设置的SlidingExpiration，而不是默认值");
    }

    [Fact]
    public async Task L1Cache_WithAbsoluteExpirationRelativeToNow_ShouldUseThatValue()
    {
        // Arrange
        var service = CreateService();
        var absoluteExpiration = TimeSpan.FromMinutes(15);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            AbsoluteExpirationRelativeToNow = absoluteExpiration
            // 未设置L1Expiration
        };

        MemoryCacheEntryOptions? capturedOptions = null;
        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    capturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(absoluteExpiration);
    }

    [Fact]
    public async Task L1Cache_PriorityOrder_L1Expiration_OverridesOthers()
    {
        // Arrange
        var service = CreateService();
        var l1Expiration = TimeSpan.FromMinutes(7);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = l1Expiration,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            AbsoluteExpiration = TimeSpan.FromMinutes(20)
        };

        MemoryCacheEntryOptions? capturedOptions = null;
        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    capturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l1Expiration,
            "L1Expiration应该具有最高优先级");
    }

    #endregion

    #region L2缓存TTL测试

    [Fact]
    public async Task L2Cache_WithExplicitL2Expiration_ShouldNotApplyDefaultSlidingExpiration()
    {
        // Arrange
        var service = CreateService();
        var expectedExpiration = TimeSpan.FromMinutes(60);
        var options = new CacheOptions
        {
            Level = CacheLevel.L2Only,
            L2Expiration = expectedExpiration
        };

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => capturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(expectedExpiration);
        
        // 关键断言：显式设置L2Expiration时，不应应用DefaultSlidingExpiration
        capturedOptions.SlidingExpiration.Should().BeNull(
            "因为显式设置了L2Expiration，不应再应用DefaultSlidingExpiration");
    }

    [Fact]
    public async Task L2Cache_WithoutExplicitExpiration_ShouldApplyDefaultSlidingExpiration()
    {
        // Arrange
        var service = CreateService();
        var options = new CacheOptions
        {
            Level = CacheLevel.L2Only
            // 未设置任何过期时间
        };

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => capturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(_cachingOptions.DefaultL2Expiration);
        capturedOptions.SlidingExpiration.Should().Be(_cachingOptions.DefaultSlidingExpiration,
            "因为未显式设置L2Expiration，应应用DefaultSlidingExpiration");
    }

    [Fact]
    public async Task L2Cache_WithExplicitSlidingExpiration_ShouldUseExplicitValue()
    {
        // Arrange
        var service = CreateService();
        var explicitSlidingExpiration = TimeSpan.FromMinutes(10);
        var options = new CacheOptions
        {
            Level = CacheLevel.L2Only,
            L2Expiration = TimeSpan.FromMinutes(60),
            SlidingExpiration = explicitSlidingExpiration
        };

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => capturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.SlidingExpiration.Should().Be(explicitSlidingExpiration,
            "应使用显式设置的SlidingExpiration，而不是默认值");
    }

    [Fact]
    public async Task L2Cache_PriorityOrder_L2Expiration_OverridesOthers()
    {
        // Arrange
        var service = CreateService();
        var l2Expiration = TimeSpan.FromMinutes(45);
        var options = new CacheOptions
        {
            Level = CacheLevel.L2Only,
            L2Expiration = l2Expiration,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            AbsoluteExpiration = TimeSpan.FromMinutes(60)
        };

        DistributedCacheEntryOptions? capturedOptions = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => capturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l2Expiration,
            "L2Expiration应该具有最高优先级");
    }

    #endregion

    #region 两级缓存TTL独立性测试

    [Fact]
    public async Task BothCache_L1AndL2ExpirationShouldBeIndependent()
    {
        // Arrange
        var service = CreateService();
        var l1Expiration = TimeSpan.FromMinutes(5);
        var l2Expiration = TimeSpan.FromMinutes(60);
        var options = new CacheOptions
        {
            Level = CacheLevel.Both,
            L1Expiration = l1Expiration,
            L2Expiration = l2Expiration
        };

        MemoryCacheEntryOptions? l1CapturedOptions = null;
        DistributedCacheEntryOptions? l2CapturedOptions = null;

        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    l1CapturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => l2CapturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        l1CapturedOptions.Should().NotBeNull();
        l2CapturedOptions.Should().NotBeNull();
        
        // L1缓存应使用L1Expiration
        l1CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l1Expiration);
        l1CapturedOptions.SlidingExpiration.Should().BeNull("显式设置了L1Expiration");
        
        // L2缓存应使用L2Expiration
        l2CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l2Expiration);
        l2CapturedOptions.SlidingExpiration.Should().BeNull("显式设置了L2Expiration");
        
        // 验证两者独立
        l1CapturedOptions.AbsoluteExpirationRelativeToNow.Should().NotBeNull();
        l2CapturedOptions.AbsoluteExpirationRelativeToNow.Should().NotBeNull();
        l1CapturedOptions.AbsoluteExpirationRelativeToNow!.Value.Should()
            .NotBe(l2CapturedOptions.AbsoluteExpirationRelativeToNow!.Value,
                "L1和L2的过期时间应该独立设置");
    }

    [Fact]
    public async Task BothCache_WithOnlyL1Expiration_L2ShouldUseDefault()
    {
        // Arrange
        var service = CreateService();
        var l1Expiration = TimeSpan.FromMinutes(5);
        var options = new CacheOptions
        {
            Level = CacheLevel.Both,
            L1Expiration = l1Expiration
            // 未设置L2Expiration
        };

        MemoryCacheEntryOptions? l1CapturedOptions = null;
        DistributedCacheEntryOptions? l2CapturedOptions = null;

        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    l1CapturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => l2CapturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        l1CapturedOptions.Should().NotBeNull();
        l2CapturedOptions.Should().NotBeNull();
        
        // L1使用显式设置的值
        l1CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l1Expiration);
        l1CapturedOptions.SlidingExpiration.Should().BeNull();
        
        // L2使用默认值
        l2CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(_cachingOptions.DefaultL2Expiration);
        l2CapturedOptions.SlidingExpiration.Should().Be(_cachingOptions.DefaultSlidingExpiration);
    }

    [Fact]
    public async Task BothCache_WithCommonExpiration_BothShouldUseIt()
    {
        // Arrange
        var service = CreateService();
        var commonExpiration = TimeSpan.FromMinutes(20);
        var options = new CacheOptions
        {
            Level = CacheLevel.Both,
            AbsoluteExpirationRelativeToNow = commonExpiration
            // 未设置L1Expiration和L2Expiration
        };

        MemoryCacheEntryOptions? l1CapturedOptions = null;
        DistributedCacheEntryOptions? l2CapturedOptions = null;

        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    l1CapturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => l2CapturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        l1CapturedOptions.Should().NotBeNull();
        l2CapturedOptions.Should().NotBeNull();
        
        // 两者都应使用共同的过期时间
        l1CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(commonExpiration);
        l2CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(commonExpiration);
    }

    #endregion

    #region 边界条件和异常场景测试

    [Fact]
    public async Task TTL_WithZeroExpiration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var service = CreateService();
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = TimeSpan.Zero  // 无效值
        };

        // Act & Assert
        // MemoryCacheEntryOptions 不接受零值或负值的过期时间
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SetAsync("test:key", "test:value", options));
        
        exception.Should().NotBeNull();
        exception.Message.Should().Contain("relative expiration");
    }

    [Fact]
    public async Task TTL_WithNegativeExpiration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var service = CreateService();
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = TimeSpan.FromMinutes(-1)  // 负值
        };

        // Act & Assert
        // MemoryCacheEntryOptions 不接受零值或负值的过期时间
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SetAsync("test:key", "test:value", options));
        
        exception.Should().NotBeNull();
        exception.Message.Should().Contain("relative expiration");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public async Task TTL_WithVariousExpirationTimes_ShouldBeConsistent(int minutes)
    {
        // Arrange
        var service = CreateService();
        var expiration = TimeSpan.FromMinutes(minutes);
        var options = new CacheOptions
        {
            Level = CacheLevel.Both,
            L1Expiration = expiration,
            L2Expiration = expiration
        };

        MemoryCacheEntryOptions? l1CapturedOptions = null;
        DistributedCacheEntryOptions? l2CapturedOptions = null;

        _mockMemoryCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) =>
            {
                var mockEntry = new Mock<ICacheEntry>();
                mockEntry.SetupProperty(e => e.Value);
                mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                mockEntry.SetupProperty(e => e.SlidingExpiration);
                mockEntry.SetupProperty(e => e.Priority);
                
                mockEntry.Setup(e => e.Dispose()).Callback(() =>
                {
                    l1CapturedOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = mockEntry.Object.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = mockEntry.Object.SlidingExpiration,
                        Priority = mockEntry.Object.Priority
                    };
                });

                return mockEntry.Object;
            });

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, opts, ct) => l2CapturedOptions = opts)
            .Returns(Task.CompletedTask);

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // Assert
        l1CapturedOptions.Should().NotBeNull();
        l2CapturedOptions.Should().NotBeNull();
        
        l1CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(expiration,
            $"L1缓存应使用设置的{minutes}分钟过期时间");
        l2CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(expiration,
            $"L2缓存应使用设置的{minutes}分钟过期时间");
        
        // 验证不应用DefaultSlidingExpiration
        l1CapturedOptions.SlidingExpiration.Should().BeNull();
        l2CapturedOptions.SlidingExpiration.Should().BeNull();
    }

    #endregion

    #region 辅助方法

    private MultiLevelCacheService CreateService()
    {
        return new MultiLevelCacheService(
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            null, // lockProvider
            _mockKeyGenerator.Object,
            Options.Create(_cachingOptions),
            _mockLogger.Object);
    }

    #endregion
}

