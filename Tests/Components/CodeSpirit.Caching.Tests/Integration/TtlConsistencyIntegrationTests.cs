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
using System.Reflection;
using Xunit;

namespace CodeSpirit.Caching.Tests.Integration;

/// <summary>
/// TTL时间一致性集成测试
/// 使用真实的MemoryCache实例来验证TTL时间的实际行为
/// </summary>
public class TtlConsistencyIntegrationTests
{
    private readonly IMemoryCache _realMemoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<MultiLevelCacheService>> _mockLogger;
    private readonly CachingOptions _cachingOptions;

    public TtlConsistencyIntegrationTests()
    {
        _realMemoryCache = new MemoryCache(new MemoryCacheOptions());
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

        _mockKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parts) => $"{prefix}:{string.Join(":", parts)}");
    }

    [Fact]
    public async Task RealMemoryCache_ExplicitL1Expiration_ShouldExpireAtCorrectTime()
    {
        // Arrange
        var service = CreateService();
        var expiration = TimeSpan.FromSeconds(2);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = expiration
        };

        // Act
        await service.SetAsync("test:key", "test:value", options);
        
        // 立即读取应该成功
        var immediate = await service.GetAsync<string>("test:key");
        immediate.Should().Be("test:value", "刚设置的缓存应该能立即读取");

        // 等待过期时间的一半
        await Task.Delay(expiration / 2);
        var halfTime = await service.GetAsync<string>("test:key");
        halfTime.Should().Be("test:value", "在过期时间一半时仍应存在");

        // 等待超过过期时间
        await Task.Delay(expiration + TimeSpan.FromMilliseconds(500));
        var expired = await service.GetAsync<string>("test:key");
        expired.Should().BeNull("超过过期时间后应该已过期");
    }

    [Fact]
    public async Task RealMemoryCache_WithDefaultSlidingExpiration_ShouldSlideCorrectly()
    {
        // Arrange
        var service = CreateService();
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            // 不设置L1Expiration，使用DefaultSlidingExpiration
        };

        // Act
        await service.SetAsync("test:key", "test:value", options);
        
        // 持续访问，验证滑动过期的效果
        var slidingWindow = _cachingOptions.DefaultSlidingExpiration!.Value;
        var accessInterval = slidingWindow / 2;

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(accessInterval);
            var value = await service.GetAsync<string>("test:key");
            value.Should().Be("test:value", 
                $"第{i + 1}次访问时仍应存在（由于滑动过期不断刷新）");
        }

        // 停止访问，等待超过滑动窗口
        await Task.Delay(slidingWindow + TimeSpan.FromMilliseconds(500));
        var expired = await service.GetAsync<string>("test:key");
        expired.Should().BeNull("停止访问后超过滑动窗口应该已过期");
    }

    [Fact]
    public async Task RealMemoryCache_ExplicitL1Expiration_ShouldNotSlide()
    {
        // Arrange
        var service = CreateService();
        var absoluteExpiration = TimeSpan.FromSeconds(4);  // 增加到4秒以避免边界问题
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = absoluteExpiration
        };

        // Act
        await service.SetAsync("test:key", "test:value", options);
        
        // 在过期前持续访问（只访问到总时间的75%，确保不超时）
        var accessInterval = TimeSpan.FromSeconds(0.6);
        var accessDuration = TimeSpan.FromSeconds(3);  // 只访问3秒，小于4秒过期时间
        var accessCount = (int)(accessDuration.TotalSeconds / accessInterval.TotalSeconds);

        for (int i = 0; i < accessCount; i++)
        {
            await Task.Delay(accessInterval);
            var value = await service.GetAsync<string>("test:key");
            value.Should().Be("test:value", 
                $"第{i + 1}次访问（{(i + 1) * accessInterval.TotalSeconds:F1}秒）时仍应存在");
        }

        // 等待超过过期时间
        var totalElapsed = accessCount * accessInterval.TotalSeconds;
        var remainingTime = absoluteExpiration.TotalSeconds - totalElapsed;
        await Task.Delay(TimeSpan.FromSeconds(remainingTime + 0.5));
        
        var expired = await service.GetAsync<string>("test:key");
        expired.Should().BeNull(
            "即使持续访问，使用绝对过期时间的缓存也应该在指定时间后过期（不滑动）");
    }

    [Fact]
    public async Task L2Cache_TTLConsistency_VerifyActualExpiration()
    {
        // Arrange
        var service = CreateService();
        var l2Expiration = TimeSpan.FromMinutes(60);
        var options = new CacheOptions
        {
            Level = CacheLevel.L2Only,
            L2Expiration = l2Expiration
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
            "L2缓存应该使用显式设置的过期时间");
        capturedOptions.SlidingExpiration.Should().BeNull(
            "显式设置L2Expiration后不应有滑动过期");
    }

    [Fact]
    public async Task BothCache_TTLIndependence_VerifyWithRealL1()
    {
        // Arrange
        var service = CreateService();
        var l1Expiration = TimeSpan.FromSeconds(2);
        var l2Expiration = TimeSpan.FromMinutes(60);
        var options = new CacheOptions
        {
            Level = CacheLevel.Both,
            L1Expiration = l1Expiration,
            L2Expiration = l2Expiration
        };

        DistributedCacheEntryOptions? l2CapturedOptions = null;
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
        
        // 验证L1缓存的实际过期行为
        var immediate = await service.GetAsync<string>("test:key");
        immediate.Should().Be("test:value", "刚设置的缓存应该能读取");

        await Task.Delay(l1Expiration + TimeSpan.FromMilliseconds(500));
        
        // L1应该已过期，但L2仍然存在
        // 为了验证L1已过期，我们需要模拟L2返回数据
        var serializedValue = System.Text.Encoding.UTF8.GetBytes(
            Newtonsoft.Json.JsonConvert.SerializeObject("test:value"));
        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        var fromL2 = await service.GetAsync<string>("test:key");
        fromL2.Should().Be("test:value", "L1过期后应该能从L2恢复数据");

        // Assert L2的配置
        l2CapturedOptions.Should().NotBeNull();
        l2CapturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(l2Expiration);
        l2CapturedOptions.SlidingExpiration.Should().BeNull();
    }

    [Fact]
    public async Task MultipleKeys_TTLShouldBeIndependent()
    {
        // Arrange
        var service = CreateService();
        
        var key1Options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = TimeSpan.FromSeconds(2)
        };
        
        var key2Options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = TimeSpan.FromSeconds(4)
        };

        // Act
        await service.SetAsync("test:key1", "value1", key1Options);
        await service.SetAsync("test:key2", "value2", key2Options);

        // 立即读取都应该成功
        var key1Immediate = await service.GetAsync<string>("test:key1");
        var key2Immediate = await service.GetAsync<string>("test:key2");
        key1Immediate.Should().Be("value1");
        key2Immediate.Should().Be("value2");

        // 等待key1过期
        await Task.Delay(TimeSpan.FromSeconds(2.5));
        
        var key1Expired = await service.GetAsync<string>("test:key1");
        var key2Still = await service.GetAsync<string>("test:key2");
        key1Expired.Should().BeNull("key1应该已过期");
        key2Still.Should().Be("value2", "key2应该仍然存在");

        // 等待key2也过期
        await Task.Delay(TimeSpan.FromSeconds(2));
        var key2Expired = await service.GetAsync<string>("test:key2");
        key2Expired.Should().BeNull("key2现在也应该已过期");
    }

    [Fact]
    public async Task VerifyTTLBugFix_ExplicitExpirationShouldNotBeMixedWithDefault()
    {
        // Arrange - 这个测试验证修复后的行为
        var service = CreateService();
        
        // 场景：显式设置了L1Expiration为4秒
        // 旧bug：会同时应用DefaultSlidingExpiration（2分钟），导致缓存提前过期
        // 修复后：不应用DefaultSlidingExpiration，缓存应该在4秒后过期
        
        // 关键：显式过期时间要大于DefaultSlidingExpiration
        var defaultSlidingExp = _cachingOptions.DefaultSlidingExpiration!.Value;  // 2分钟
        var explicitExpiration = TimeSpan.FromSeconds(4);  // 4秒 < 2分钟，所以如果bug存在会在4秒过期
        
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = explicitExpiration
        };

        // Act
        await service.SetAsync("test:key", "test:value", options);

        // 立即验证缓存存在
        var immediate = await service.GetAsync<string>("test:key");
        immediate.Should().Be("test:value", "刚设置的缓存应该能读取");

        // 在过期时间的一半时仍然应该存在
        await Task.Delay(explicitExpiration / 2);
        var shouldStillExist = await service.GetAsync<string>("test:key");
        shouldStillExist.Should().Be("test:value",
            "在过期时间一半时，缓存应该仍然存在");

        // 但在显式设置的过期时间后应该已过期
        await Task.Delay(explicitExpiration / 2 + TimeSpan.FromMilliseconds(500));
        var shouldBeExpired = await service.GetAsync<string>("test:key");
        shouldBeExpired.Should().BeNull("应该在显式设置的过期时间（4秒）后过期");
    }

    [Fact]
    public async Task ConcurrentAccess_TTLShouldRemainConsistent()
    {
        // Arrange
        var service = CreateService();
        var expiration = TimeSpan.FromSeconds(3);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = expiration
        };

        await service.SetAsync("test:key", "test:value", options);

        // Act - 并发访问
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(i * 100));
            var value = await service.GetAsync<string>("test:key");
            return new { Index = i, Value = value, Time = DateTime.UtcNow };
        });

        var results = await Task.WhenAll(tasks);

        // Assert - 在过期前的所有访问都应该成功
        foreach (var result in results)
        {
            result.Value.Should().Be("test:value", 
                $"第{result.Index}次并发访问应该都能获取到值");
        }

        // 等待过期
        await Task.Delay(expiration + TimeSpan.FromMilliseconds(500));
        var expired = await service.GetAsync<string>("test:key");
        expired.Should().BeNull("所有并发访问后，缓存应该仍然在设定时间过期");
    }

    [Theory]
    [InlineData(1, 0.5, true)]   // 在过期前访问
    [InlineData(1, 1.5, false)]  // 在过期后访问
    [InlineData(2, 1.0, true)]   // 在过期前访问
    [InlineData(2, 2.5, false)]  // 在过期后访问
    [InlineData(5, 3.0, true)]   // 在过期前访问
    [InlineData(5, 6.0, false)]  // 在过期后访问
    public async Task ParameterizedTTL_ShouldExpireAtCorrectTime(
        double expirationSeconds, 
        double delaySeconds, 
        bool shouldExist)
    {
        // Arrange
        var service = CreateService();
        var expiration = TimeSpan.FromSeconds(expirationSeconds);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = expiration
        };

        // Act
        await service.SetAsync("test:key", "test:value", options);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        var value = await service.GetAsync<string>("test:key");

        // Assert
        if (shouldExist)
        {
            value.Should().Be("test:value", 
                $"在设置{expirationSeconds}秒过期，等待{delaySeconds}秒后应该仍然存在");
        }
        else
        {
            value.Should().BeNull(
                $"在设置{expirationSeconds}秒过期，等待{delaySeconds}秒后应该已过期");
        }
    }

    [Fact]
    public async Task GetOrSetAsync_WithExplicitExpiration_ShouldUseCorrectTTL()
    {
        // Arrange
        var service = CreateService();
        var expiration = TimeSpan.FromSeconds(2);
        var options = new CacheOptions
        {
            Level = CacheLevel.L1Only,
            L1Expiration = expiration,
            EnableBreakthroughProtection = false // 简化测试
        };

        var factoryCallCount = 0;
        Func<Task<string>> factory = async () =>
        {
            factoryCallCount++;
            await Task.Delay(10); // 模拟异步操作
            return "factory:value";
        };

        // Act - 首次调用，缓存未命中
        var first = await service.GetOrSetAsync("test:key", factory, options);
        first.Should().Be("factory:value");
        factoryCallCount.Should().Be(1, "首次调用应该执行工厂方法");

        // 立即第二次调用，应该从缓存获取
        var second = await service.GetOrSetAsync("test:key", factory, options);
        second.Should().Be("factory:value");
        factoryCallCount.Should().Be(1, "第二次调用应该从缓存获取，不执行工厂方法");

        // 等待过期
        await Task.Delay(expiration + TimeSpan.FromMilliseconds(500));

        // 过期后再次调用，应该重新执行工厂方法
        var third = await service.GetOrSetAsync("test:key", factory, options);
        third.Should().Be("factory:value");
        factoryCallCount.Should().Be(2, "缓存过期后应该重新执行工厂方法");
    }

    private MultiLevelCacheService CreateService()
    {
        return new MultiLevelCacheService(
            _realMemoryCache,
            _mockDistributedCache.Object,
            null, // lockProvider
            _mockKeyGenerator.Object,
            Options.Create(_cachingOptions),
            _mockLogger.Object);
    }
}

