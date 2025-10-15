using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Services;
using CodeSpirit.Caching.Tests.Models;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Caching.Tests.Integration;

/// <summary>
/// 缓存键生成集成测试
/// 验证从TestExamCacheOptions到最终缓存键的完整流程
/// </summary>
public class CacheKeyGenerationIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;

    public CacheKeyGenerationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // 设置服务容器
        var services = new ServiceCollection();
        
        // 添加缓存配置
        services.Configure<CachingOptions>(options =>
        {
            options.KeyPrefix = "CodeSpirit:Cache:";
            options.EnableL1Cache = true;
            options.EnableL2Cache = false; // 禁用L2缓存以简化测试
            options.DefaultL1Expiration = TimeSpan.FromMinutes(5);
        });

        // 添加内存缓存
        services.AddMemoryCache();
        
        // 添加分布式缓存（使用内存实现用于测试）
        services.AddDistributedMemoryCache();

        // 添加分布式锁提供者（Mock实现用于测试）
        var lockProviderMock = new Mock<IDistributedLockProvider>();
        services.AddSingleton(lockProviderMock.Object);

        // 添加缓存服务
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
        services.AddScoped<ICacheService, MultiLevelCacheService>();

        // 添加日志
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _keyGenerator = _serviceProvider.GetRequiredService<ICacheKeyGenerator>();
    }

    [Fact]
    public void CacheKeyGenerator_ShouldGenerateExpectedKeys()
    {
        // Arrange
        var examId = 1978042626567733248L;
        var userId = 123456789L;
        var recordId = 987654321L;

        // Act & Assert - BasicInfo
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        var basicInfoFullKey = _keyGenerator.GenerateKey("data", basicInfoKey.Key);
        var expectedBasicInfoKey = "CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_1978042626567733248";
        
        basicInfoFullKey.Should().Be(expectedBasicInfoKey);
        _output.WriteLine($"✅ BasicInfo - 原始键: {basicInfoKey.Key}");
        _output.WriteLine($"✅ BasicInfo - 完整键: {basicInfoFullKey}");

        // Act & Assert - Questions
        var questionsKey = new TestExamCacheOptions.Questions(examId);
        var questionsFullKey = _keyGenerator.GenerateKey("data", questionsKey.Key);
        var expectedQuestionsKey = "CodeSpirit:Cache:data:TestExamCacheOptions_Questions_1978042626567733248";
        
        questionsFullKey.Should().Be(expectedQuestionsKey);
        _output.WriteLine($"✅ Questions - 原始键: {questionsKey.Key}");
        _output.WriteLine($"✅ Questions - 完整键: {questionsFullKey}");

        // Act & Assert - UserRecord
        var userRecordKey = new TestExamCacheOptions.UserRecord(examId, userId);
        var userRecordFullKey = _keyGenerator.GenerateKey("data", userRecordKey.Key);
        var expectedUserRecordKey = "CodeSpirit:Cache:data:TestExamCacheOptions_UserRecord_1978042626567733248_123456789";
        
        userRecordFullKey.Should().Be(expectedUserRecordKey);
        _output.WriteLine($"✅ UserRecord - 原始键: {userRecordKey.Key}");
        _output.WriteLine($"✅ UserRecord - 完整键: {userRecordFullKey}");

        // Act & Assert - UserAnswers
        var userAnswersKey = new TestExamCacheOptions.UserAnswers(recordId, userId);
        var userAnswersFullKey = _keyGenerator.GenerateKey("data", userAnswersKey.Key);
        var expectedUserAnswersKey = "CodeSpirit:Cache:data:TestExamCacheOptions_UserAnswers_987654321_123456789";
        
        userAnswersFullKey.Should().Be(expectedUserAnswersKey);
        _output.WriteLine($"✅ UserAnswers - 原始键: {userAnswersKey.Key}");
        _output.WriteLine($"✅ UserAnswers - 完整键: {userAnswersFullKey}");
    }

    [Fact]
    public async Task CacheService_WithTestExamCacheOptions_ShouldGenerateCorrectKeys()
    {
        // Arrange
        var examId = 1978042626567733248L;
        var testValue = "test-exam-data";

        // Act
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        
        // 使用原始键进行缓存操作（模拟实际使用场景）
        var result = await _cacheService.GetOrSetAsync(
            basicInfoKey.Key,
            () => Task.FromResult(testValue));

        // Assert
        result.Should().Be(testValue);
        
        // 验证缓存中确实存储了数据
        var cachedResult = await _cacheService.GetAsync<string>(basicInfoKey.Key);
        cachedResult.Should().Be(testValue);

        _output.WriteLine($"✅ 缓存操作成功");
        _output.WriteLine($"✅ 缓存键: {basicInfoKey.Key}");
        _output.WriteLine($"✅ 缓存值: {result}");
    }

    [Fact]
    public async Task CacheService_ShouldNotDuplicateKeyGeneration()
    {
        // Arrange
        var examId = 1978042626567733248L;
        var testValue = "test-data";

        // 创建一个自定义的键生成器来跟踪调用
        var callCount = 0;
        var actualGeneratedKeys = new List<string>();

        // 创建新的服务容器，使用自定义键生成器
        var services = new ServiceCollection();
        services.Configure<CachingOptions>(options =>
        {
            options.KeyPrefix = "CodeSpirit:Cache:";
            options.EnableL1Cache = true;
            options.EnableL2Cache = false;
        });
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddLogging();
        
        // 添加分布式锁提供者
        var lockProviderMock = new Mock<IDistributedLockProvider>();
        services.AddSingleton(lockProviderMock.Object);

        // 添加自定义键生成器
        services.AddSingleton<ICacheKeyGenerator>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CachingOptions>>();
            return new TestCacheKeyGenerator(options, (prefix, parts) =>
            {
                callCount++;
                var key = parts.Length > 0 ? parts[0]?.ToString() ?? "" : "";
                var result = $"CodeSpirit:Cache:{prefix}:{key}";
                actualGeneratedKeys.Add(result);
                _output.WriteLine($"键生成器调用 #{callCount}: {prefix} + {key} = {result}");
                return result;
            });
        });

        services.AddScoped<ICacheService, MultiLevelCacheService>();

        using var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        // Act
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        await cacheService.GetOrSetAsync(basicInfoKey.Key, () => Task.FromResult(testValue));

        // Assert
        // 键生成器被调用两次：一次用于数据键，一次用于锁键（缓存击穿保护）
        callCount.Should().Be(2, "键生成器应该被调用两次：数据键和锁键");
        
        actualGeneratedKeys.Should().HaveCount(2);
        actualGeneratedKeys[0].Should().Be($"CodeSpirit:Cache:data:{basicInfoKey.Key}");
        actualGeneratedKeys[1].Should().Be($"CodeSpirit:Cache:lock:{basicInfoKey.Key}");
        
        _output.WriteLine($"✅ 键生成器调用次数: {callCount} (期望: 2)");
        _output.WriteLine($"✅ 生成的数据键: {actualGeneratedKeys[0]}");
        _output.WriteLine($"✅ 生成的锁键: {actualGeneratedKeys[1]}");
        _output.WriteLine($"✅ 避免了键重复处理问题");
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(123456789L)]
    [InlineData(1978042626567733248L)]
    public async Task CacheService_WithDifferentExamIds_ShouldGenerateUniqueKeys(long examId)
    {
        // Arrange
        var testValue = $"test-data-{examId}";

        // Act
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        var result = await _cacheService.GetOrSetAsync(basicInfoKey.Key, () => Task.FromResult(testValue));

        // Assert
        result.Should().Be(testValue);

        // 验证键的唯一性
        var expectedKeyPattern = $"TestExamCacheOptions_BasicInfo_{examId}";
        basicInfoKey.Key.Should().Be(expectedKeyPattern);

        _output.WriteLine($"考试ID: {examId}");
        _output.WriteLine($"生成的键: {basicInfoKey.Key}");
        _output.WriteLine($"缓存值: {result}");
    }

    [Fact]
    public async Task CacheService_ConcurrentOperations_ShouldNotCauseKeyCollisions()
    {
        // Arrange
        var examIds = new[] { 1L, 2L, 3L, 4L, 5L };
        var tasks = new List<Task<string>>();

        // Act - 并发执行缓存操作
        foreach (var examId in examIds)
        {
            var task = Task.Run(async () =>
            {
                var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
                var testValue = $"concurrent-test-{examId}";
                
                var result = await _cacheService.GetOrSetAsync(basicInfoKey.Key, () => Task.FromResult(testValue));
                
                _output.WriteLine($"并发操作 - 考试ID: {examId}, 键: {basicInfoKey.Key}, 值: {result}");
                return result;
            });
            
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        for (int i = 0; i < examIds.Length; i++)
        {
            var expectedValue = $"concurrent-test-{examIds[i]}";
            results[i].Should().Be(expectedValue);
        }

        _output.WriteLine($"✅ 并发操作完成，所有键都正确生成且无冲突");
    }

    [Fact]
    public async Task CacheService_AllTestExamCacheOptionsTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var examId = 1978042626567733248L;
        var userId = 123456789L;
        var recordId = 987654321L;

        // Act & Assert - 测试所有类型的 TestExamCacheOptions
        
        // BasicInfo
        var basicInfo = new TestExamCacheOptions.BasicInfo(examId);
        var basicInfoResult = await _cacheService.GetOrSetAsync(basicInfo.Key, () => Task.FromResult("basic-info"));
        basicInfoResult.Should().Be("basic-info");

        // Questions
        var questions = new TestExamCacheOptions.Questions(examId);
        var questionsResult = await _cacheService.GetOrSetAsync(questions.Key, () => Task.FromResult("questions-data"));
        questionsResult.Should().Be("questions-data");

        // UserRecord
        var userRecord = new TestExamCacheOptions.UserRecord(examId, userId);
        var userRecordResult = await _cacheService.GetOrSetAsync(userRecord.Key, () => Task.FromResult("user-record"));
        userRecordResult.Should().Be("user-record");

        // UserAnswers
        var userAnswers = new TestExamCacheOptions.UserAnswers(recordId, userId);
        var userAnswersResult = await _cacheService.GetOrSetAsync(userAnswers.Key, () => Task.FromResult("user-answers"));
        userAnswersResult.Should().Be("user-answers");

        // ClientProfile
        var clientProfile = new TestExamCacheOptions.ClientProfile(userId);
        var clientProfileResult = await _cacheService.GetOrSetAsync(clientProfile.Key, () => Task.FromResult("client-profile"));
        clientProfileResult.Should().Be("client-profile");

        _output.WriteLine("✅ 所有 TestExamCacheOptions 类型都工作正常:");
        _output.WriteLine($"  BasicInfo: {basicInfo.Key} -> {basicInfoResult}");
        _output.WriteLine($"  Questions: {questions.Key} -> {questionsResult}");
        _output.WriteLine($"  UserRecord: {userRecord.Key} -> {userRecordResult}");
        _output.WriteLine($"  UserAnswers: {userAnswers.Key} -> {userAnswersResult}");
        _output.WriteLine($"  ClientProfile: {clientProfile.Key} -> {clientProfileResult}");
    }

    [Fact]
    public void VerifyCacheKeyGenerationFix_ComprehensiveTest()
    {
        _output.WriteLine("=== 缓存键生成修复验证 ===");
        _output.WriteLine("");
        
        _output.WriteLine("🔍 问题描述:");
        _output.WriteLine("  实际生成的键: CodeSpirit:Cache:data:CodeSpirit_Cache_data_TestExamCacheOptions_BasicInfo_1978042626567733248");
        _output.WriteLine("  期望的键:     CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_1978042626567733248");
        _output.WriteLine("  问题原因:     MultiLevelCacheService 中键被重复处理");
        _output.WriteLine("");
        
        _output.WriteLine("✅ 修复方案:");
        _output.WriteLine("  1. 添加了 GetAsyncInternal 方法，直接使用已生成的完整键");
        _output.WriteLine("  2. 添加了 SetAsyncInternal 方法，直接使用已生成的完整键");
        _output.WriteLine("  3. 修改了 GetOrSetAsync 方法，避免键重复处理");
        _output.WriteLine("");
        
        // 实际验证键生成
        var examId = 1978042626567733248L;
        var basicInfoKey = new TestExamCacheOptions.BasicInfo(examId);
        var expectedKey = $"TestExamCacheOptions_BasicInfo_{examId}";
        var fullKey = _keyGenerator.GenerateKey("data", basicInfoKey.Key);
        var expectedFullKey = $"CodeSpirit:Cache:data:TestExamCacheOptions_BasicInfo_{examId}";
        
        basicInfoKey.Key.Should().Be(expectedKey);
        fullKey.Should().Be(expectedFullKey);
        fullKey.Should().NotContain("CodeSpirit_Cache_data_", "修复后不应包含重复的前缀");
        
        _output.WriteLine("🎯 验证结果:");
        _output.WriteLine($"  TestExamCacheOptions 键: {basicInfoKey.Key}");
        _output.WriteLine($"  完整缓存键: {fullKey}");
        _output.WriteLine($"  ✅ 键格式正确，无重复处理");
        _output.WriteLine("");
        _output.WriteLine("✅ 缓存键生成修复验证通过！");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// 测试用的缓存键生成器，用于跟踪调用
    /// </summary>
    private class TestCacheKeyGenerator : ICacheKeyGenerator
    {
        private readonly CachingOptions _options;
        private readonly Func<string, object[], string> _generateKeyCallback;

        public TestCacheKeyGenerator(IOptions<CachingOptions> options, Func<string, object[], string> generateKeyCallback)
        {
            _options = options.Value;
            _generateKeyCallback = generateKeyCallback;
        }

        public string GenerateKey(string prefix, params object[] parts)
        {
            return _generateKeyCallback(prefix, parts);
        }

        public string GenerateTenantKey(string tenantId, string prefix, params object[] parts)
        {
            var allParts = new List<object> { $"tenant:{tenantId}" };
            if (parts != null)
            {
                allParts.AddRange(parts);
            }
            return GenerateKey(prefix, allParts.ToArray());
        }

        public string GenerateUserKey(long userId, string prefix, params object[] parts)
        {
            var allParts = new List<object> { $"user:{userId}" };
            if (parts != null)
            {
                allParts.AddRange(parts);
            }
            return GenerateKey(prefix, allParts.ToArray());
        }

        public bool ValidateKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.Length <= 250;
        }

        public string ExtractPrefix(string key)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith(_options.KeyPrefix))
                return string.Empty;

            var withoutGlobalPrefix = key.Substring(_options.KeyPrefix.Length);
            var firstColonIndex = withoutGlobalPrefix.IndexOf(':');
            
            return firstColonIndex > 0 
                ? withoutGlobalPrefix.Substring(0, firstColonIndex)
                : withoutGlobalPrefix;
        }
    }
}
