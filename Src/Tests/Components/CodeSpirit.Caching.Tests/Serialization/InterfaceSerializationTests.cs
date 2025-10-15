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
using Newtonsoft.Json;
using System.Text;
using Xunit;

namespace CodeSpirit.Caching.Tests.Serialization;

/// <summary>
/// 接口序列化测试
/// </summary>
public class InterfaceSerializationTests
{
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<MultiLevelCacheService>> _mockLogger;
    private readonly CachingOptions _cachingOptions;
    private readonly MultiLevelCacheService _cacheService;

    public InterfaceSerializationTests()
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

        // 设置 KeyGenerator 返回简单的键（模拟 "data" 前缀的添加）
        _mockKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parts) => $"Test:{prefix}:{string.Join(":", parts)}");

        _cacheService = new MultiLevelCacheService(
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            null, // lockProvider
            _mockKeyGenerator.Object,
            Options.Create(_cachingOptions),
            _mockLogger.Object);
    }

    #region 接口类型序列化测试

    [Fact]
    public async Task SetAsync_WithInterfaceType_ShouldSerializeWithTypeInfo()
    {
        // Arrange
        var key = "test:interface";
        ITestInterface testObj = new TestImplementation
        {
            Id = 1,
            Name = "Test",
            IsActive = true
        };

        byte[]? capturedData = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        // 关键：使用泛型方法时，泛型参数类型（而非变量声明类型）决定序列化行为
        await _cacheService.SetAsync<ITestInterface>(key, testObj, new CacheOptions { Level = CacheLevel.L2Only });

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        // TypeNameHandling.Auto 基于泛型参数类型，当 T 是接口时会添加 $type
        json.Should().Contain("$type"); // 应包含类型信息
        json.Should().Contain("TestImplementation");
    }

    [Fact]
    public async Task GetAsync_WithInterfaceType_ShouldDeserializeCorrectly()
    {
        // Arrange
        var key = "test:interface";
        var testObj = new TestImplementation
        {
            Id = 1,
            Name = "Test",
            IsActive = true
        };

        // 关键：使用接口类型序列化以包含 $type 信息
        var jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        // 强制转换为接口类型以触发类型信息添加
        ITestInterface interfaceObj = testObj;
        var json = JsonConvert.SerializeObject(interfaceObj, typeof(ITestInterface), jsonSettings);
        var data = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<ITestInterface>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrSetAsync_WithInterfaceType_ShouldWorkCorrectly()
    {
        // Arrange
        var key = "test:interface";
        var factoryCalled = false;

        ITestInterface factory() 
        {
            factoryCalled = true;
            return new TestImplementation
            {
                Id = 1,
                Name = "Test",
                IsActive = true
            };
        }

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async () => await Task.FromResult(factory()),
            new CacheOptions { Level = CacheLevel.L2Only });

        // Assert
        factoryCalled.Should().BeTrue();
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    #endregion

    #region 抽象类序列化测试

    [Fact]
    public async Task SetAsync_WithAbstractClass_ShouldSerializeWithTypeInfo()
    {
        // Arrange
        var key = "test:abstract";
        BaseTestEntity testObj = new ConcreteTestEntity
        {
            Id = 1,
            Name = "Test",
            Description = "Concrete"
        };

        byte[]? capturedData = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        await _cacheService.SetAsync<BaseTestEntity>(key, testObj, new CacheOptions { Level = CacheLevel.L2Only });

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        json.Should().Contain("$type");
        json.Should().Contain("ConcreteTestEntity");
    }

    [Fact]
    public async Task GetAsync_WithAbstractClass_ShouldDeserializeCorrectly()
    {
        // Arrange
        var key = "test:abstract";
        var testObj = new ConcreteTestEntity
        {
            Id = 1,
            Name = "Test",
            Description = "Concrete"
        };

        var jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        // 使用抽象类型序列化以包含类型信息
        BaseTestEntity abstractObj = testObj;
        var json = JsonConvert.SerializeObject(abstractObj, typeof(BaseTestEntity), jsonSettings);
        var data = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<BaseTestEntity>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ConcreteTestEntity>();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        ((ConcreteTestEntity)result).Description.Should().Be("Concrete");
    }

    #endregion

    #region 多态集合测试

    [Fact]
    public async Task SetAsync_WithPolymorphicCollection_ShouldSerializeCorrectly()
    {
        // Arrange
        var key = "test:collection";
        IEnumerable<ITestInterface> collection = new List<ITestInterface>
        {
            new TestImplementation { Id = 1, Name = "Test1", IsActive = true },
            new TestImplementation { Id = 2, Name = "Test2", IsActive = false }
        };

        byte[]? capturedData = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        await _cacheService.SetAsync(key, collection, new CacheOptions { Level = CacheLevel.L2Only });

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        json.Should().Contain("$type");
    }

    [Fact]
    public async Task GetAsync_WithPolymorphicCollection_ShouldDeserializeCorrectly()
    {
        // Arrange
        var key = "test:collection";
        var collection = new List<TestImplementation>
        {
            new TestImplementation { Id = 1, Name = "Test1", IsActive = true },
            new TestImplementation { Id = 2, Name = "Test2", IsActive = false }
        };

        var jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        // 使用接口集合类型序列化
        IEnumerable<ITestInterface> interfaceCollection = collection;
        var json = JsonConvert.SerializeObject(interfaceCollection, typeof(IEnumerable<ITestInterface>), jsonSettings);
        var data = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<IEnumerable<ITestInterface>>(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        var list = result!.ToList();
        list[0].Id.Should().Be(1);
        list[1].Id.Should().Be(2);
    }

    #endregion

    #region 具体类型测试（不应包含类型信息）

    [Fact]
    public async Task SetAsync_WithConcreteType_ShouldNotIncludeTypeInfo()
    {
        // Arrange
        var key = "test:concrete";
        var testObj = new TestImplementation
        {
            Id = 1,
            Name = "Test",
            IsActive = true
        };

        byte[]? capturedData = null;
        _mockDistributedCache
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        await _cacheService.SetAsync(key, testObj, new CacheOptions { Level = CacheLevel.L2Only });

        // Assert
        capturedData.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedData!);
        // 具体类型使用 TypeNameHandling.Auto 时不应包含 $type
        json.Should().NotContain("$type");
    }

    #endregion
}

#region 测试类型定义

/// <summary>
/// 测试接口
/// </summary>
public interface ITestInterface
{
    int Id { get; set; }
    string Name { get; set; }
    bool IsActive { get; set; }
}

/// <summary>
/// 测试实现类
/// </summary>
public class TestImplementation : ITestInterface
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// 测试抽象基类
/// </summary>
public abstract class BaseTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 测试具体实现类
/// </summary>
public class ConcreteTestEntity : BaseTestEntity
{
    public string Description { get; set; } = string.Empty;
}

#endregion

