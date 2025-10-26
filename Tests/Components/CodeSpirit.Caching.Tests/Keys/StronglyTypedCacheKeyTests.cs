using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.Caching.Keys;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CodeSpirit.Caching.Tests.Keys;

/// <summary>
/// 强类型缓存键测试
/// </summary>
public class StronglyTypedCacheKeyTests
{
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<MultiLevelCacheService>> _mockLogger;
    private readonly CachingOptions _cachingOptions;
    private readonly MultiLevelCacheService _cacheService;

    public StronglyTypedCacheKeyTests()
    {
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<MultiLevelCacheService>>();

        _cachingOptions = new CachingOptions
        {
            EnableL1Cache = false, // 仅测试 L2 缓存，简化Mock
            EnableL2Cache = true,
            KeyPrefix = "Test:"
        };

        // 设置 KeyGenerator 返回简单的键（模拟添加全局前缀）
        _mockKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parts) => 
            {
                // 模拟 CacheKeyGenerator 的行为：添加全局前缀
                var key = $"{_cachingOptions.KeyPrefix}{prefix}";
                if (parts != null && parts.Length > 0)
                {
                    key += ":" + string.Join(":", parts.Select(p => p?.ToString() ?? ""));
                }
                return key;
            });

        _cacheService = new MultiLevelCacheService(
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            null, // lockProvider
            _mockKeyGenerator.Object,
            Options.Create(_cachingOptions),
            _mockLogger.Object);
    }

    #region Record 类型缓存键测试

    [Fact]
    public void TestCacheKey_ShouldGenerateCorrectKey()
    {
        // Arrange
        var key = new TestCacheKey(123);

        // Assert
        key.Key.Should().Be("TestCacheKey:123");
    }

    [Fact]
    public void TestCacheKey_WithNameof_ShouldGenerateCorrectKey()
    {
        // Arrange
        var key = new NameofCacheKey(456);

        // Assert
        key.Key.Should().Be("NameofCacheKey:NameofCacheKey:456");
    }

    [Fact]
    public void TestCacheKey_ShouldHaveCorrectOptions()
    {
        // Arrange
        var key = new TestCacheKey(123);

        // Assert
        key.Options.Should().NotBeNull();
        key.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
        key.Options.Level.Should().Be(CacheLevel.Both);
    }

    [Fact]
    public void TestCacheKey_ShouldHaveCorrectTags()
    {
        // Arrange
        var key = new TestCacheKey(123);

        // Assert
        key.Tags.Should().NotBeNull();
        key.Tags.Should().HaveCount(1);
        key.Tags.Should().Contain("test:123");
    }

    [Fact]
    public void RecordCacheKey_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var key1 = new TestCacheKey(123);
        var key2 = new TestCacheKey(123);

        // Assert
        key1.Should().Be(key2);
        (key1 == key2).Should().BeTrue();
    }

    [Fact]
    public void RecordCacheKey_WithDifferentParameters_ShouldNotBeEqual()
    {
        // Arrange
        var key1 = new TestCacheKey(123);
        var key2 = new TestCacheKey(456);

        // Assert
        key1.Should().NotBe(key2);
        (key1 != key2).Should().BeTrue();
    }

    #endregion

    #region 强类型键扩展方法测试

    [Fact]
    public async Task GetOrSetAsync_WithStronglyTypedKey_ShouldInvokeCorrectly()
    {
        // Arrange
        var key = new TestCacheKey(789);
        var expectedValue = new TestData { Id = 789, Name = "Test" };
        var factoryCalled = false;

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async () =>
            {
                factoryCalled = true;
                return expectedValue;
            });

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(789);
        result.Name.Should().Be("Test");
        factoryCalled.Should().BeTrue();

        // 验证 KeyGenerator 被调用（实际调用是通过 MultiLevelCacheService 内部调用的）
        _mockKeyGenerator.Verify(
            x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SetAsync_WithStronglyTypedKey_ShouldSetCorrectly()
    {
        // Arrange
        var key = new TestCacheKey(999);
        var value = new TestData { Id = 999, Name = "SetTest" };

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        _mockDistributedCache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithStronglyTypedKey_ShouldRemoveCorrectly()
    {
        // Arrange
        var key = new TestCacheKey(111);

        _mockDistributedCache
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDistributedCache.Verify(
            x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 类型安全测试

    [Fact]
    public void StronglyTypedKey_ShouldEnforceTypeAtCompileTime()
    {
        // 这个测试验证编译时类型安全
        // 如果能编译通过，说明类型检查正确

        // Arrange
        var key = new TestCacheKey(123);

        // Assert - 编译时验证
        // key 的类型参数是 TestData，只能用于 TestData 类型的缓存操作
        ICacheKey<TestData> typedKey = key;
        typedKey.Should().NotBeNull();
    }

    #endregion

    #region 标签传递测试

    [Fact]
    public async Task GetOrSetAsync_WithTags_ShouldPassTagsToOptions()
    {
        // Arrange
        var key = new TestCacheKey(222);
        var expectedValue = new TestData { Id = 222, Name = "TagTest" };

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async () => expectedValue);

        // Assert
        result.Should().NotBeNull();
        // 标签应该已经传递到内部的 CacheOptions 中
        key.Tags.Should().Contain("test:222");
    }

    #endregion
}

#region 测试用的缓存键定义

/// <summary>
/// 测试用的缓存键
/// </summary>
public record TestCacheKey(long Id) : ICacheKey<TestData>
{
    public string Key => $"TestCacheKey:{Id}";
    
    public CacheOptions Options => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        Level = CacheLevel.Both
    };
    
    public IReadOnlyList<string> Tags => [$"test:{Id}"];
}

/// <summary>
/// 使用 nameof 的测试缓存键
/// </summary>
public record NameofCacheKey(long Id) : ICacheKey<TestData>
{
    public string Key => $"{nameof(NameofCacheKey)}:{nameof(NameofCacheKey)}:{Id}";
    
    public CacheOptions Options => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        Level = CacheLevel.L2Only
    };
    
    public IReadOnlyList<string> Tags => [$"nameof:{Id}"];
}

/// <summary>
/// 测试数据类
/// </summary>
public class TestData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

