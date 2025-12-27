using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Caching.Tests.Services;

/// <summary>
/// Redis缓存管理服务单元测试
/// </summary>
public class RedisCacheManagementServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<IServer> _serverMock;
    private readonly Mock<ILogger<RedisCacheManagementService>> _loggerMock;
    private readonly CachingOptions _options;
    private readonly RedisCacheManagementService _service;

    public RedisCacheManagementServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _serverMock = new Mock<IServer>();
        _loggerMock = new Mock<ILogger<RedisCacheManagementService>>();

        _options = new CachingOptions
        {
            KeyPrefix = "CodeSpirit:Cache:",
            EnableL1Cache = true,
            EnableL2Cache = true
        };

        var optionsMock = new Mock<IOptions<CachingOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        // 设置 Redis Mock
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);
        _redisMock.Setup(x => x.GetEndPoints()).Returns(new[] { new System.Net.DnsEndPoint("localhost", 6379) });
        _redisMock.Setup(x => x.GetServer(It.IsAny<System.Net.EndPoint>())).Returns(_serverMock.Object);

        _service = new RedisCacheManagementService(
            _redisMock.Object,
            optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact(Skip = "需要真实的 Redis 连接或更复杂的 Mock 设置，建议使用集成测试")]
    public async Task GetKeysAsync_WithPattern_ShouldReturnMatchingKeys()
    {
        // 注意：此测试需要真实的 Redis 连接或复杂的 Mock 设置
        // 由于 StackExchange.Redis 的 IServer.KeysAsync 方法签名复杂，Mock 较困难
        // 建议使用集成测试来测试此功能
        _output.WriteLine("⚠️ 此测试需要集成测试环境");
    }

    [Fact(Skip = "需要真实的 Redis 连接或更复杂的 Mock 设置，建议使用集成测试")]
    public async Task GetKeysAsync_WithPagination_ShouldReturnPagedResults()
    {
        // 注意：此测试需要真实的 Redis 连接或复杂的 Mock 设置
        _output.WriteLine("⚠️ 此测试需要集成测试环境");
    }

    [Fact]
    public async Task GetValueAsync_WithStringType_ShouldReturnValue()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:test:key";
        var value = "test-value";
        var serializedValue = Encoding.UTF8.GetBytes($"\"{value}\"");

        _databaseMock.Setup(x => x.KeyExistsAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _databaseMock.Setup(x => x.KeyTypeAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisType.String);
        _databaseMock.Setup(x => x.StringGetAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)value);
        _databaseMock.Setup(x => x.KeyTimeToLiveAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((TimeSpan?)TimeSpan.FromMinutes(30));

        // Act
        var result = await _service.GetValueAsync(key);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.Type.Should().Be("string");
        result.Value.Should().Contain(value);
        result.Ttl.Should().BeGreaterThan(0);

        _output.WriteLine($"✅ 获取字符串类型缓存值成功：{result.Value}");
    }

    [Fact]
    public async Task GetValueAsync_WithHashType_ShouldReturnHashValue()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:hash:key";
        var hashFields = new[]
        {
            new HashEntry("field1", "value1"),
            new HashEntry("field2", "value2")
        };

        _databaseMock.Setup(x => x.KeyExistsAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _databaseMock.Setup(x => x.KeyTypeAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisType.Hash);
        _databaseMock.Setup(x => x.HashGetAllAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(hashFields);
        _databaseMock.Setup(x => x.KeyTimeToLiveAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync((TimeSpan?)TimeSpan.FromMinutes(30));

        // Act
        var result = await _service.GetValueAsync(key);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("hash");
        result.Value.Should().Contain("field1");
        result.Value.Should().Contain("value1");

        _output.WriteLine($"✅ 获取哈希类型缓存值成功：{result.Value}");
    }

    [Fact]
    public async Task GetValueAsync_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:nonexistent:key";

        _databaseMock.Setup(x => x.KeyExistsAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetValueAsync(key);

        // Assert
        result.Should().BeNull();

        _output.WriteLine("✅ 不存在的键返回 null");
    }

    [Fact]
    public async Task DeleteKeyAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:test:key";

        _databaseMock.Setup(x => x.KeyDeleteAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteKeyAsync(key);

        // Assert
        result.Should().BeTrue();
        _databaseMock.Verify(x => x.KeyDeleteAsync((RedisKey)key, It.IsAny<CommandFlags>()), Times.Once);

        _output.WriteLine("✅ 删除键成功");
    }

    [Fact]
    public async Task DeleteKeyAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:nonexistent:key";

        _databaseMock.Setup(x => x.KeyDeleteAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteKeyAsync(key);

        // Assert
        result.Should().BeFalse();

        _output.WriteLine("✅ 删除不存在的键返回 false");
    }

    [Fact(Skip = "需要真实的 Redis 连接或更复杂的 Mock 设置，建议使用集成测试")]
    public async Task DeleteByPatternAsync_ShouldDeleteMatchingKeys()
    {
        // 注意：此测试需要真实的 Redis 连接或复杂的 Mock 设置
        // 由于 StackExchange.Redis 的 IServer.KeysAsync 方法签名复杂，Mock 较困难
        // 建议使用集成测试来测试此功能
        _output.WriteLine("⚠️ 此测试需要集成测试环境");
    }

    [Fact(Skip = "需要真实的 Redis 连接或更复杂的 Mock 设置，建议使用集成测试")]
    public async Task DeleteByPatternAsync_WithTenantId_ShouldFilterByTenant()
    {
        // 注意：此测试需要真实的 Redis 连接或复杂的 Mock 设置
        _output.WriteLine("⚠️ 此测试需要集成测试环境");
    }

    [Fact]
    public async Task ClearAllAsync_ShouldFlushDatabase()
    {
        // Arrange
        _serverMock.Setup(x => x.FlushDatabaseAsync(
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ClearAllAsync();

        // Assert
        result.Should().BeTrue();
        _serverMock.Verify(x => x.FlushDatabaseAsync(
            It.IsAny<int>(),
            It.IsAny<CommandFlags>()), Times.Once);

        _output.WriteLine("✅ 清空所有缓存成功");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:test:key";

        _databaseMock.Setup(x => x.KeyExistsAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();

        _output.WriteLine("✅ 键存在检查返回 true");
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:nonexistent:key";

        _databaseMock.Setup(x => x.KeyExistsAsync((RedisKey)key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();

        _output.WriteLine("✅ 键不存在检查返回 false");
    }
}

/// <summary>
/// 扩展方法：将 IEnumerable 转换为 IAsyncEnumerable
/// </summary>
public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}

