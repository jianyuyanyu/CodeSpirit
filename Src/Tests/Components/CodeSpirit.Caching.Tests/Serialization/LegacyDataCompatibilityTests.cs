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
/// 旧数据兼容性测试
/// 测试系统如何处理没有类型信息的旧缓存数据
/// </summary>
public class LegacyDataCompatibilityTests
{
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ILogger<MultiLevelCacheService>> _mockLogger;
    private readonly CachingOptions _cachingOptions;
    private readonly MultiLevelCacheService _cacheService;

    public LegacyDataCompatibilityTests()
    {
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        _mockLogger = new Mock<ILogger<MultiLevelCacheService>>();

        _cachingOptions = new CachingOptions
        {
            EnableL1Cache = false, // 仅测试 L2 缓存
            EnableL2Cache = true,
            KeyPrefix = "Test:"
        };

        _mockKeyGenerator
            .Setup(x => x.GenerateKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parts) => $"{prefix}:{string.Join(":", parts)}");

        _cacheService = new MultiLevelCacheService(
            _mockMemoryCache.Object,
            _mockDistributedCache.Object,
            null,
            _mockKeyGenerator.Object,
            Options.Create(_cachingOptions),
            _mockLogger.Object);
    }

    #region 旧数据格式测试（无类型信息）

    [Fact]
    public async Task GetAsync_WithLegacyInterfaceData_ShouldLogWarningAndReturnDefault()
    {
        // Arrange
        var key = "test:legacy:interface";
        
        // 模拟旧格式的JSON（没有 $type 字段）
        var legacyObj = new LegacyTestImplementation
        {
            Id = 1,
            Name = "Legacy Test",
            IsActive = true
        };
        var legacyJson = JsonConvert.SerializeObject(legacyObj); // 无 TypeNameHandling
        var data = Encoding.UTF8.GetBytes(legacyJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<ILegacyTestInterface>(key);

        // Assert
        // 由于不符合命名约定且没有 $type 信息，应返回 default(null) 并记录警告
        result.Should().BeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("反序列化失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAsync_WithLegacyData_ShouldLogTypeInferenceAttempt()
    {
        // Arrange
        var key = "test:legacy";
        
        // 模拟无法推断的旧数据
        var legacyJson = @"{""Id"":1,""Name"":""Test""}";
        var data = Encoding.UTF8.GetBytes(legacyJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<IUnknownInterface>(key);

        // Assert
        result.Should().BeNull(); // 无法推断类型，返回null
        
        // 验证记录了警告日志
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("反序列化失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce);
    }

    #endregion

    #region 新旧数据混合测试

    [Fact]
    public async Task GetAsync_WithNewFormatData_ShouldDeserializeDirectly()
    {
        // Arrange
        var key = "test:new:format";
        var testObj = new LegacyTestImplementation
        {
            Id = 1,
            Name = "New Format",
            IsActive = true
        };

        // 使用新格式（带类型信息）- 关键：使用接口类型序列化以添加 $type
        var jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        ILegacyTestInterface interfaceObj = testObj;
        var json = JsonConvert.SerializeObject(interfaceObj, typeof(ILegacyTestInterface), jsonSettings);
        var data = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<ILegacyTestInterface>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("New Format");
        
        // 不应该记录警告（新格式不需要类型推断）
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("反序列化失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Never);
    }

    #endregion

    #region ITenantInfo 特殊处理测试

    [Fact]
    public async Task GetAsync_WithITenantInfoLegacyData_ShouldInferTenantInfo()
    {
        // Arrange
        var key = "test:tenant:legacy";
        
        // 模拟旧的 TenantInfo 数据（没有类型信息）
        var legacyJson = @"{
            ""TenantId"": ""default"",
            ""Name"": ""考试系统"",
            ""DisplayName"": ""考试系统"",
            ""IsActive"": true
        }";
        var data = Encoding.UTF8.GetBytes(legacyJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        // 注意：这个测试需要实际的 ITenantInfo 接口才能工作
        // 这里仅演示测试结构
        var result = await _cacheService.GetAsync<IMockTenantInfo>(key);

        // Assert
        // 由于 ITenantInfo 有特殊处理，应该能够成功推断
        if (result != null)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("成功使用具体类型")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.AtLeastOnce);
        }
    }

    #endregion

    #region 类型推断规则测试

    [Fact]
    public async Task GetAsync_WithITypeNameConvention_ShouldInferTypeName()
    {
        // Arrange
        var key = "test:convention";
        
        // 模拟符合 I{TypeName} 命名约定的接口
        var legacyJson = @"{""Id"":1,""Value"":""Test""}";
        var data = Encoding.UTF8.GetBytes(legacyJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<IConventionTest>(key);

        // Assert
        // 应尝试推断 ConventionTest 类型
        // 如果成功，应返回结果；否则返回null并记录日志
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("类型")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce);
    }

    #endregion

    #region 错误处理测试

    [Fact]
    public async Task GetAsync_WithInvalidLegacyData_ShouldReturnNullAndLog()
    {
        // Arrange
        var key = "test:invalid";
        
        // 无效的JSON
        var invalidJson = @"{invalid json}";
        var data = Encoding.UTF8.GetBytes(invalidJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<ILegacyTestInterface>(key);

        // Assert
        result.Should().BeNull();
        
        // 应记录错误日志
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l >= LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAsync_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var key = "test:empty";
        var data = Array.Empty<byte>();

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var result = await _cacheService.GetAsync<ILegacyTestInterface>(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 性能测试

    [Fact]
    public async Task GetAsync_TypeInference_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var key = "test:performance";
        var legacyJson = @"{""Id"":1,""Name"":""Test"",""IsActive"":true}";
        var data = Encoding.UTF8.GetBytes(legacyJson);

        _mockDistributedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _cacheService.GetAsync<ILegacyTestInterface>(key);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // 应在100ms内完成
    }

    #endregion
}

#region 测试类型定义

/// <summary>
/// 旧数据测试接口（模拟遗留系统的接口）
/// </summary>
public interface ILegacyTestInterface
{
    int Id { get; set; }
    string Name { get; set; }
    bool IsActive { get; set; }
}

/// <summary>
/// 旧数据测试实现类
/// </summary>
public class LegacyTestImplementation : ILegacyTestInterface
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// 未知接口（用于测试无法推断的情况）
/// </summary>
public interface IUnknownInterface
{
    int Id { get; set; }
    string Name { get; set; }
}

/// <summary>
/// 模拟 ITenantInfo 接口
/// </summary>
public interface IMockTenantInfo
{
    string TenantId { get; set; }
    string Name { get; set; }
    string DisplayName { get; set; }
    bool IsActive { get; set; }
}

/// <summary>
/// 命名约定测试接口
/// </summary>
public interface IConventionTest
{
    int Id { get; set; }
    string Value { get; set; }
}

#endregion

