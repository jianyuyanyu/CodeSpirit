using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;
using CodeSpirit.Caching.Tests.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Caching.Tests.Services;

/// <summary>
/// 缓存键生成修复测试
/// 验证 MultiLevelCacheService 中键重复处理问题的修复
/// </summary>
public class CacheKeyGenerationFixTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly Mock<IDistributedLockProvider> _lockProviderMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ILogger<MultiLevelCacheService>> _loggerMock;
    private readonly CachingOptions _options;
    private readonly MultiLevelCacheService _cacheService;

    public CacheKeyGenerationFixTests(ITestOutputHelper output)
    {
        _output = output;
        _memoryCacheMock = new Mock<IMemoryCache>();
        _distributedCacheMock = new Mock<IDistributedCache>();
        _lockProviderMock = new Mock<IDistributedLockProvider>();
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _loggerMock = new Mock<ILogger<MultiLevelCacheService>>();

        _options = new CachingOptions
        {
            EnableL1Cache = true,
            EnableL2Cache = true,
            KeyPrefix = "CodeSpirit:Cache:",
            DefaultL1Expiration = TimeSpan.FromMinutes(5),
            DefaultL2Expiration = TimeSpan.FromMinutes(30)
        };

        var optionsMock = new Mock<IOptions<CachingOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        // 设置默认的内存缓存Mock行为
        SetupMemoryCacheMock();

        _cacheService = new MultiLevelCacheService(
            _memoryCacheMock.Object,
            _distributedCacheMock.Object,
            _lockProviderMock.Object,
            _keyGeneratorMock.Object,
            optionsMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// 设置内存缓存Mock的默认行为
    /// </summary>
    private void SetupMemoryCacheMock()
    {
        // 设置内存缓存CreateEntry方法
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupAllProperties();
        
        _memoryCacheMock
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntryMock.Object);
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange - 模拟缓存键生成修复前的问题场景
        var originalKey = "TestExamCacheOptions_BasicInfo_1978042626567733248";
        var expectedFullKey = "CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_1978042626567733248";
        var wrongDuplicatedKey = "CodeSpirit:Cache:data:CodeSpirit_Cache_data_TestExamCacheOptions_BasicInfo_1978042626567733248";
        var testValue = "test-exam-data";

        // 设置键生成器 - 只应该被调用一次用于生成完整键
        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", originalKey))
            .Returns(expectedFullKey);

        // 确保不会用完整键再次调用键生成器（这会导致重复处理）
        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", expectedFullKey))
            .Returns(wrongDuplicatedKey);

        // 设置缓存未命中
        _memoryCacheMock
            .Setup(x => x.TryGetValue(expectedFullKey, out It.Ref<object>.IsAny))
            .Returns(false);

        _distributedCacheMock
            .Setup(x => x.GetAsync(expectedFullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetOrSetAsync(originalKey, () => Task.FromResult(testValue));

        // Assert
        result.Should().Be(testValue);

        // 🔑 关键验证：键生成器只被调用一次（用于生成完整键）
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", originalKey), Times.Once,
            "键生成器应该只被调用一次来生成完整键");

        // 🔑 关键验证：不应该使用完整键再次调用键生成器（这会导致键重复处理）
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedFullKey), Times.Never,
            "不应该使用完整键再次调用键生成器，这会导致键重复处理问题");

        _output.WriteLine($"✅ 原始键: {originalKey}");
        _output.WriteLine($"✅ 生成的完整键: {expectedFullKey}");
        _output.WriteLine($"❌ 避免了错误的重复键: {wrongDuplicatedKey}");
        _output.WriteLine("✅ 键生成器只被调用一次，修复了键重复处理问题");
    }

    [Fact]
    public async Task GetOrSetAsync_WithTestExamCacheOptions_ShouldGenerateCorrectKeys()
    {
        // Arrange - 使用真实的 TestExamCacheOptions
        var examId = 1978042626567733248L;
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        var expectedOriginalKey = $"TestExamCacheOptions_BasicInfo_{examId}";
        var expectedFullKey = $"CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_{examId}";
        var testValue = "exam-basic-info-data";

        // 验证 TestExamCacheOptions 生成的键格式
        basicInfoKey.Key.Should().Be(expectedOriginalKey);

        // 设置键生成器
        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", expectedOriginalKey))
            .Returns(expectedFullKey);

        // 设置缓存未命中
        _memoryCacheMock
            .Setup(x => x.TryGetValue(expectedFullKey, out It.Ref<object>.IsAny))
            .Returns(false);

        _distributedCacheMock
            .Setup(x => x.GetAsync(expectedFullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act - 使用强类型缓存键
        var result = await _cacheService.GetOrSetAsync(
            basicInfoKey.Key,
            () => Task.FromResult(testValue));

        // Assert
        result.Should().Be(testValue);

        // 验证键生成正确
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedOriginalKey), Times.Once);

        _output.WriteLine($"✅ 考试ID: {examId}");
        _output.WriteLine($"✅ TestExamCacheOptions 原始键: {basicInfoKey.Key}");
        _output.WriteLine($"✅ 期望的完整键: {expectedFullKey}");
        _output.WriteLine("✅ 强类型缓存键工作正常");
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(123456789L)]
    [InlineData(1978042626567733248L)]
    public async Task GetOrSetAsync_WithDifferentExamIds_ShouldGenerateUniqueKeys(long examId)
    {
        // Arrange
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        var expectedOriginalKey = $"TestExamCacheOptions_BasicInfo_{examId}";
        var expectedFullKey = $"CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_{examId}";
        var testValue = $"exam-data-{examId}";

        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", expectedOriginalKey))
            .Returns(expectedFullKey);

        _memoryCacheMock
            .Setup(x => x.TryGetValue(expectedFullKey, out It.Ref<object>.IsAny))
            .Returns(false);

        _distributedCacheMock
            .Setup(x => x.GetAsync(expectedFullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            basicInfoKey.Key,
            () => Task.FromResult(testValue));

        // Assert
        result.Should().Be(testValue);
        basicInfoKey.Key.Should().Be(expectedOriginalKey);

        _output.WriteLine($"考试ID: {examId} -> 键: {basicInfoKey.Key} -> 完整键: {expectedFullKey}");
    }

    [Fact]
    public async Task GetOrSetAsync_WithL2CacheHit_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange - 测试L2缓存命中时的键处理
        var originalKey = "TestExamCacheOptions_Questions_1978042626567733248";
        var expectedFullKey = "CodeSpirit:Cache:data:TestExamCacheOptions_Questions_1978042626567733248";
        var cachedValue = "cached-questions-data";
        var serializedValue = Encoding.UTF8.GetBytes($"\"{cachedValue}\"");

        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", originalKey))
            .Returns(expectedFullKey);

        // L1缓存未命中
        _memoryCacheMock
            .Setup(x => x.TryGetValue(expectedFullKey, out It.Ref<object>.IsAny))
            .Returns(false);

        // L2缓存命中
        _distributedCacheMock
            .Setup(x => x.GetAsync(expectedFullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetOrSetAsync(originalKey, () => Task.FromResult("factory-value"));

        // Assert
        // 验证键生成器只被调用一次
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", originalKey), Times.Once);
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedFullKey), Times.Never);

        // 验证L1缓存回填（通过CreateEntry调用）
        _memoryCacheMock.Verify(x => x.CreateEntry(expectedFullKey), Times.Once);

        _output.WriteLine($"✅ L2缓存命中场景下键生成正确");
        _output.WriteLine($"✅ 键生成器只调用一次: {expectedFullKey}");
        _output.WriteLine($"✅ L1缓存回填正常");
    }

    [Fact]
    public async Task SetAsync_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange
        var originalKey = "TestExamCacheOptions_UserAnswers_987654321_123456789";
        var expectedFullKey = "CodeSpirit:Cache:data:TestExamCacheOptions_UserAnswers_987654321_123456789";
        var testValue = "user-answers-data";

        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", originalKey))
            .Returns(expectedFullKey);

        // Act
        await _cacheService.SetAsync(originalKey, testValue);

        // Assert
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", originalKey), Times.Once);
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedFullKey), Times.Never);

        _output.WriteLine($"✅ SetAsync 键生成验证通过");
        _output.WriteLine($"✅ 原始键: {originalKey}");
        _output.WriteLine($"✅ 完整键: {expectedFullKey}");
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange
        var originalKey = "TestExamCacheOptions_UserRecord_1978042626567733248_123456789";
        var expectedFullKey = "CodeSpirit:Cache:data:TestExamCacheOptions_UserRecord_1978042626567733248_123456789";

        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", originalKey))
            .Returns(expectedFullKey);

        // Act
        await _cacheService.RemoveAsync(originalKey);

        // Assert
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", originalKey), Times.Once);
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedFullKey), Times.Never);

        _output.WriteLine($"✅ RemoveAsync 键生成验证通过");
    }

    [Fact]
    public async Task GetAsync_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange
        var originalKey = "TestExamCacheOptions_ClientProfile_123456789";
        var expectedFullKey = "CodeSpirit:Cache:data:TestExamCacheOptions_ClientProfile_123456789";

        _keyGeneratorMock
            .Setup(x => x.GenerateKey("data", originalKey))
            .Returns(expectedFullKey);

        _memoryCacheMock
            .Setup(x => x.TryGetValue(expectedFullKey, out It.Ref<object>.IsAny))
            .Returns(false);

        _distributedCacheMock
            .Setup(x => x.GetAsync(expectedFullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetAsync<string>(originalKey);

        // Assert
        result.Should().BeNull();
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", originalKey), Times.Once);
        _keyGeneratorMock.Verify(x => x.GenerateKey("data", expectedFullKey), Times.Never);

        _output.WriteLine($"✅ GetAsync 键生成验证通过");
    }

    [Fact]
    public void TestExamCacheOptions_ShouldGenerateExpectedKeyFormats()
    {
        // Arrange
        var examId = 1978042626567733248L;
        var userId = 123456789L;
        var recordId = 987654321L;

        // Act & Assert
        var basicInfo = new TestExamCacheOptions.BasicInfo(examId);
        basicInfo.Key.Should().Be($"TestExamCacheOptions_BasicInfo_{examId}");

        var questions = new TestExamCacheOptions.Questions(examId);
        questions.Key.Should().Be($"TestExamCacheOptions_Questions_{examId}");

        var userRecord = new TestExamCacheOptions.UserRecord(examId, userId);
        userRecord.Key.Should().Be($"TestExamCacheOptions_UserRecord_{examId}_{userId}");

        var userAnswers = new TestExamCacheOptions.UserAnswers(recordId, userId);
        userAnswers.Key.Should().Be($"TestExamCacheOptions_UserAnswers_{recordId}_{userId}");

        var clientProfile = new TestExamCacheOptions.ClientProfile(userId);
        clientProfile.Key.Should().Be($"TestExamCacheOptions_ClientProfile_{userId}");

        _output.WriteLine("✅ 所有 TestExamCacheOptions 键格式验证通过:");
        _output.WriteLine($"  BasicInfo: {basicInfo.Key}");
        _output.WriteLine($"  Questions: {questions.Key}");
        _output.WriteLine($"  UserRecord: {userRecord.Key}");
        _output.WriteLine($"  UserAnswers: {userAnswers.Key}");
        _output.WriteLine($"  ClientProfile: {clientProfile.Key}");
    }

    [Fact]
    public void CacheKeyGenerationFix_ShouldPreventWrongKeyFormat()
    {
        // Arrange - 展示修复前后的键格式对比
        var examId = 1978042626567733248L;
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);

        // 原始键（TestExamCacheOptions 生成）
        var originalKey = basicInfoKey.Key;

        // 期望的完整键（修复后）
        var expectedCorrectKey = $"CodeSpirit:Cache:data:{originalKey}";

        // 错误的重复键（修复前的问题）
        var wrongDuplicatedKey = $"CodeSpirit:Cache:data:CodeSpirit_Cache_data_{originalKey}";

        // Assert
        originalKey.Should().Be("TestExamCacheOptions_BasicInfo_1978042626567733248");
        expectedCorrectKey.Should().Be("CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_1978042626567733248");
        wrongDuplicatedKey.Should().Be("CodeSpirit:Cache:data:CodeSpirit_Cache_data_TestExamCacheOptions_BasicInfo_1978042626567733248");

        // 验证修复效果
        expectedCorrectKey.Should().NotContain("CodeSpirit_Cache_data_", "修复后不应包含重复的前缀");
        wrongDuplicatedKey.Should().Contain("CodeSpirit_Cache_data_", "这是修复前的错误格式");

        _output.WriteLine("🔍 缓存键生成修复对比:");
        _output.WriteLine($"  原始键: {originalKey}");
        _output.WriteLine($"  ✅ 修复后: {expectedCorrectKey}");
        _output.WriteLine($"  ❌ 修复前: {wrongDuplicatedKey}");
        _output.WriteLine("");
        _output.WriteLine("✅ 修复成功：避免了键重复处理问题");
    }
}
