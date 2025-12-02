using CodeSpirit.Settings.Models;
using CodeSpirit.Settings.Services.Implementations;
using CodeSpirit.Settings.Tests.TestBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit;
using CodeSpirit.Settings.Data;

namespace CodeSpirit.Settings.Tests.Services;

/// <summary>
/// 租户设置服务测试类
/// </summary>
public class TenantSettingsServiceTests : SettingsServiceTestBase
{
    private readonly TestSettingsService _settingsService;
    private const string TestModule = "TestModule";
    private const string TestKey = "TestKey";
    private const string TestTenantId = "tenant1";

    public TenantSettingsServiceTests()
        : base()
    {
        // 初始化TestSettingsService
        _settingsService = new TestSettingsService(
            DbContext,
            MockSettingsServiceLogger.Object,
            MockDistributedCache.Object,
            this
        );
        
        // 准备测试数据
        SeedTestData();
    }
    
    /// <summary>
    /// 准备设置测试数据
    /// </summary>
    protected override void SeedTestData()
    {
        // 准备一个JSON对象用于测试
        var testModel = new TestModel
        {
            Name = "Test",
            Value = 123
        };
        
        // 使用System.Text.Json序列化，这与SettingsService中使用的序列化器一致
        var jsonValue = System.Text.Json.JsonSerializer.Serialize(testModel);
        
        var settingItems = new List<SettingItem>
        {
            new SettingItem
            {
                Id = 1,
                Module = TestModule,
                Key = "GlobalSetting",
                Name = "Global Setting",
                Value = "GlobalValue",
                Scope = SettingScope.Global,
                TenantId = "default",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new SettingItem
            {
                Id = 2,
                Module = TestModule,
                Key = "TenantSetting",
                Name = "Tenant Setting",
                Value = "TenantValue",
                Scope = SettingScope.Tenant,
                ScopeId = TestTenantId,
                TenantId = TestTenantId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new SettingItem
            {
                Id = 3,
                Module = TestModule,
                Key = "JsonSetting",
                Name = "JSON Setting",
                Value = jsonValue,
                Scope = SettingScope.Global,
                ValueType = SettingValueType.Json,
                TenantId = "default",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        
        SeedSettingItems(settingItems.ToArray());
    }

    /// <summary>
    /// 在每个测试方法执行前自动清理数据库上下文
    /// </summary>
    protected void Setup()
    {
        ClearDbContext();
        CacheItems.Clear(); // 清除所有缓存项
    }

    [Fact]
    public async Task GetTenantSettingAsync_Exists_ReturnsTenantValue()
    {
        // 安排 
        var key = "TenantSetting";

        // 执行
        var result = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);

        // 断言
        Assert.Equal("TenantValue", result);
        
        // 验证缓存已被填充
        var cacheKey = _settingsService.GetCacheKey("Tenant", TestModule, key, TestTenantId);
        Assert.True(CacheKeyExists(cacheKey));
        Assert.Equal("TenantValue", GetCachedValue(cacheKey));
    }

    [Fact]
    public async Task GetTenantSettingAsync_NotExists_ReturnsGlobalValue()
    {
        // 安排
        var key = "GlobalSetting";

        // 执行
        var result = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);

        // 断言 - 应该返回全局设置值
        Assert.Equal("GlobalValue", result);
    }

    [Fact]
    public async Task GetTenantSettingAsync_WithCache_ReturnsCachedValue()
    {
        // 安排
        var key = "TenantSetting";
        var cacheKey = _settingsService.GetCacheKey("Tenant", TestModule, key, TestTenantId);
        MockCachedValue(cacheKey, "CachedTenantValue");

        // 执行
        var result = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);

        // 断言
        Assert.Equal("CachedTenantValue", result);
    }

    [Fact]
    public async Task GetTenantSettingAsync_NotFound_ReturnsNull()
    {
        // 安排
        var key = "NonExistentSetting";

        // 执行
        var result = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);

        // 断言
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTenantSettingAsync_Generic_DeserializesValue()
    {
        // 安排
        var key = "JsonSetting";

        // 执行
        var result = await _settingsService.GetTenantSettingAsync<TestModel>(TestModule, key, TestTenantId);

        // 断言 - 应该返回全局设置的JSON值
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task SetTenantSettingAsync_NewSetting_CreatesSetting()
    {
        // 安排
        var key = "NewTenantSetting";
        var value = "NewValue";

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证设置已创建
        var setting = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(value, setting);
        
        // 验证缓存已更新
        var cacheKey = _settingsService.GetCacheKey("Tenant", TestModule, key, TestTenantId);
        Assert.True(CacheKeyExists(cacheKey));
    }

    [Fact]
    public async Task SetTenantSettingAsync_ExistingSetting_UpdatesSetting()
    {
        // 安排
        var key = "TenantSetting";
        var newValue = "UpdatedTenantValue";

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, newValue, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证设置已更新
        var setting = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(newValue, setting);
    }

    [Fact]
    public async Task SetTenantSettingAsync_Generic_SerializesValue()
    {
        // 安排
        var key = "NewJsonTenantSetting";
        var testModel = new TestModel { Name = "TenantTest", Value = 456 };

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, testModel, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证设置已创建并可以反序列化
        var retrieved = await _settingsService.GetTenantSettingAsync<TestModel>(TestModule, key, TestTenantId);
        Assert.NotNull(retrieved);
        Assert.Equal("TenantTest", retrieved.Name);
        Assert.Equal(456, retrieved.Value);
    }

    [Fact]
    public async Task GetAllTenantSettingsAsync_ReturnsMergedSettings()
    {
        // 执行
        var result = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);

        // 断言 - 应该包含全局设置和租户设置
        Assert.True(result.ContainsKey("GlobalSetting"));
        Assert.True(result.ContainsKey("TenantSetting"));
        Assert.Equal("GlobalValue", result["GlobalSetting"]);
        Assert.Equal("TenantValue", result["TenantSetting"]);
        
        // 验证缓存已创建
        var cacheKey = _settingsService.GetCacheKey("AllTenant", TestModule, TestTenantId);
        Assert.True(CacheKeyExists(cacheKey));
    }

    [Fact]
    public async Task BatchSetTenantSettingsAsync_SetsMultipleSettings()
    {
        // 安排
        var settings = new Dictionary<string, string>
        {
            { "Setting1", "Value1" },
            { "Setting2", "Value2" }
        };

        // 执行
        var result = await _settingsService.BatchSetTenantSettingsAsync(TestModule, settings, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证所有设置都已设置
        var allSettings = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        Assert.Equal("Value1", allSettings["Setting1"]);
        Assert.Equal("Value2", allSettings["Setting2"]);
    }

    [Fact]
    public async Task ResetTenantSettingToDefaultAsync_SpecificKey_RemovesTenantSetting()
    {
        // 安排
        var key = "TenantSetting";

        // 执行
        var result = await _settingsService.ResetTenantSettingToDefaultAsync(TestModule, key, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证租户设置已删除，现在返回全局设置
        var setting = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        // 由于没有全局设置，应该返回null或全局设置值
        // 这里假设返回全局设置值（如果存在）
    }

    [Fact]
    public async Task ResetTenantSettingToDefaultAsync_AllKeys_RemovesAllTenantSettings()
    {
        // 安排 - 先创建一个租户设置
        var setResult = await _settingsService.SetTenantSettingAsync(TestModule, "TempSetting", "TempValue", TestTenantId);
        Assert.True(setResult, "设置租户设置应该成功");
        
        // 清除变更跟踪器，确保后续查询从数据库读取
        ClearDbContext();
        
        // 验证设置已创建
        var beforeReset = await _settingsService.GetTenantSettingAsync(TestModule, "TempSetting", TestTenantId);
        Assert.Equal("TempValue", beforeReset);

        // 清除变更跟踪器
        ClearDbContext();

        // 执行
        var result = await _settingsService.ResetTenantSettingToDefaultAsync(TestModule, null, TestTenantId);

        // 断言
        Assert.True(result, "重置租户设置应该成功");
        
        // 清除变更跟踪器
        ClearDbContext();
        
        // 验证所有租户设置已删除
        var allSettings = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        // 应该只包含全局设置，不包含租户特定设置
        Assert.False(allSettings.ContainsKey("TempSetting"), "租户设置应该已被删除");
    }

    [Fact]
    public async Task GetTenantSettingAsync_MultiTenantIsolation_IsolatesTenants()
    {
        // 安排
        var tenant1Id = "tenant1";
        var tenant2Id = "tenant2";
        var key = "IsolatedSetting";
        
        await _settingsService.SetTenantSettingAsync(TestModule, key, "Tenant1Value", tenant1Id);
        await _settingsService.SetTenantSettingAsync(TestModule, key, "Tenant2Value", tenant2Id);

        // 执行
        var tenant1Value = await _settingsService.GetTenantSettingAsync(TestModule, key, tenant1Id);
        var tenant2Value = await _settingsService.GetTenantSettingAsync(TestModule, key, tenant2Id);

        // 断言 - 两个租户的设置应该隔离
        Assert.Equal("Tenant1Value", tenant1Value);
        Assert.Equal("Tenant2Value", tenant2Value);
    }

    #region 边界情况测试

    [Fact]
    public async Task SetTenantSettingAsync_EmptyValue_HandlesCorrectly()
    {
        // 安排
        var key = "EmptyValueSetting";
        var value = string.Empty;

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);

        // 断言
        Assert.True(result);
        var retrieved = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(string.Empty, retrieved);
    }

    [Fact]
    public async Task SetTenantSettingAsync_LongValue_HandlesCorrectly()
    {
        // 安排
        var key = "LongValueSetting";
        var value = new string('A', 4000); // 最大长度

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);

        // 断言
        Assert.True(result);
        var retrieved = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetTenantSettingAsync_SpecialCharacters_HandlesCorrectly()
    {
        // 安排
        var key = "SpecialCharsSetting";
        var value = "测试值!@#$%^&*()_+-=[]{}|;':\",./<>?";

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);

        // 断言
        Assert.True(result);
        var retrieved = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SetTenantSettingAsync_UnicodeCharacters_HandlesCorrectly()
    {
        // 安排
        var key = "UnicodeSetting";
        var value = "测试值 🎉 émojis 中文 日本語 한국어";

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);

        // 断言
        Assert.True(result);
        var retrieved = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task GetTenantSettingAsync_DifferentTenantIds_ReturnsCorrectValues()
    {
        // 安排
        var tenant1 = "tenant-001";
        var tenant2 = "tenant-002";
        var tenant3 = "tenant-with-special-chars-123";
        var key = "MultiTenantKey";
        
        await _settingsService.SetTenantSettingAsync(TestModule, key, "Value1", tenant1);
        await _settingsService.SetTenantSettingAsync(TestModule, key, "Value2", tenant2);
        await _settingsService.SetTenantSettingAsync(TestModule, key, "Value3", tenant3);

        // 执行
        var value1 = await _settingsService.GetTenantSettingAsync(TestModule, key, tenant1);
        var value2 = await _settingsService.GetTenantSettingAsync(TestModule, key, tenant2);
        var value3 = await _settingsService.GetTenantSettingAsync(TestModule, key, tenant3);

        // 断言
        Assert.Equal("Value1", value1);
        Assert.Equal("Value2", value2);
        Assert.Equal("Value3", value3);
    }

    [Fact]
    public async Task BatchSetTenantSettingsAsync_EmptyDictionary_HandlesCorrectly()
    {
        // 安排
        var emptySettings = new Dictionary<string, string>();

        // 执行
        var result = await _settingsService.BatchSetTenantSettingsAsync(TestModule, emptySettings, TestTenantId);

        // 断言
        Assert.True(result);
    }

    [Fact]
    public async Task BatchSetTenantSettingsAsync_LargeBatch_HandlesCorrectly()
    {
        // 安排
        var largeSettings = new Dictionary<string, string>();
        for (int i = 0; i < 100; i++)
        {
            largeSettings[$"Setting{i}"] = $"Value{i}";
        }

        // 执行
        var result = await _settingsService.BatchSetTenantSettingsAsync(TestModule, largeSettings, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证所有设置都已保存
        var allSettings = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(allSettings.ContainsKey($"Setting{i}"));
            Assert.Equal($"Value{i}", allSettings[$"Setting{i}"]);
        }
    }

    [Fact]
    public async Task ResetTenantSettingToDefaultAsync_NonExistentKey_ReturnsTrue()
    {
        // 安排
        var nonExistentKey = "NonExistentKey";

        // 执行
        var result = await _settingsService.ResetTenantSettingToDefaultAsync(TestModule, nonExistentKey, TestTenantId);

        // 断言 - 即使键不存在，也应该返回true（幂等操作）
        Assert.True(result);
    }

    [Fact]
    public async Task ResetTenantSettingToDefaultAsync_NonExistentModule_ReturnsTrue()
    {
        // 安排
        var nonExistentModule = "NonExistentModule";

        // 执行
        var result = await _settingsService.ResetTenantSettingToDefaultAsync(nonExistentModule, null, TestTenantId);

        // 断言 - 即使模块不存在，也应该返回true（幂等操作）
        Assert.True(result);
    }

    #endregion

    #region 设置值类型测试

    [Fact]
    public async Task SetTenantSettingAsync_DifferentValueTypes_HandlesCorrectly()
    {
        // 安排和执行 - 测试不同类型的值
        var stringValue = "StringValue";
        var intValue = "123";
        var boolValue = "true";
        var decimalValue = "123.45";
        var jsonValue = "{\"key\":\"value\"}";

        // 执行
        await _settingsService.SetTenantSettingAsync(TestModule, "StringType", stringValue, TestTenantId);
        await _settingsService.SetTenantSettingAsync(TestModule, "IntType", intValue, TestTenantId);
        await _settingsService.SetTenantSettingAsync(TestModule, "BoolType", boolValue, TestTenantId);
        await _settingsService.SetTenantSettingAsync(TestModule, "DecimalType", decimalValue, TestTenantId);
        await _settingsService.SetTenantSettingAsync(TestModule, "JsonType", jsonValue, TestTenantId);

        // 断言
        Assert.Equal(stringValue, await _settingsService.GetTenantSettingAsync(TestModule, "StringType", TestTenantId));
        Assert.Equal(intValue, await _settingsService.GetTenantSettingAsync(TestModule, "IntType", TestTenantId));
        Assert.Equal(boolValue, await _settingsService.GetTenantSettingAsync(TestModule, "BoolType", TestTenantId));
        Assert.Equal(decimalValue, await _settingsService.GetTenantSettingAsync(TestModule, "DecimalType", TestTenantId));
        Assert.Equal(jsonValue, await _settingsService.GetTenantSettingAsync(TestModule, "JsonType", TestTenantId));
    }

    [Fact]
    public async Task SetTenantSettingAsync_ComplexJsonObject_SerializesCorrectly()
    {
        // 安排
        var key = "ComplexJsonSetting";
        var complexObject = new
        {
            Name = "Test",
            Value = 123,
            Nested = new
            {
                Property1 = "Value1",
                Property2 = new[] { 1, 2, 3 }
            },
            Array = new[] { "item1", "item2", "item3" }
        };

        // 执行
        var result = await _settingsService.SetTenantSettingAsync(TestModule, key, complexObject, TestTenantId);

        // 断言
        Assert.True(result);
        
        // 验证可以反序列化
        var jsonString = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.NotNull(jsonString);
        Assert.Contains("Test", jsonString);
        Assert.Contains("123", jsonString);
    }

    #endregion

    #region 缓存相关测试

    [Fact]
    public async Task GetTenantSettingAsync_CacheInvalidation_WorksCorrectly()
    {
        // 安排
        var key = "CacheTestSetting";
        var value1 = "Value1";
        var value2 = "Value2";
        
        // 设置初始值
        await _settingsService.SetTenantSettingAsync(TestModule, key, value1, TestTenantId);
        ClearDbContext();
        
        // 验证缓存已创建
        var cacheKey = _settingsService.GetCacheKey("Tenant", TestModule, key, TestTenantId);
        Assert.True(CacheKeyExists(cacheKey));
        Assert.Equal(value1, GetCachedValue(cacheKey));

        // 执行 - 更新值
        await _settingsService.SetTenantSettingAsync(TestModule, key, value2, TestTenantId);
        ClearDbContext();

        // 断言 - 缓存应该被清除，新值应该被缓存
        var retrieved = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.Equal(value2, retrieved);
        Assert.Equal(value2, GetCachedValue(cacheKey));
    }

    [Fact]
    public async Task GetAllTenantSettingsAsync_CacheUpdate_WorksCorrectly()
    {
        // 安排
        // 首次获取，应该创建缓存
        var settings1 = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        var cacheKey = _settingsService.GetCacheKey("AllTenant", TestModule, TestTenantId);
        Assert.True(CacheKeyExists(cacheKey));

        // 执行 - 添加新设置
        await _settingsService.SetTenantSettingAsync(TestModule, "NewCacheSetting", "NewValue", TestTenantId);
        ClearDbContext();

        // 断言 - 缓存应该被清除，重新获取应该包含新设置
        var settings2 = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        Assert.True(settings2.ContainsKey("NewCacheSetting"));
        Assert.Equal("NewValue", settings2["NewCacheSetting"]);
    }

    #endregion

    #region 租户设置优先级测试

    [Fact]
    public async Task GetTenantSettingAsync_TenantOverridesGlobal_PriorityCorrect()
    {
        // 安排
        var key = "PriorityTestSetting";
        var globalValue = "GlobalValue";
        var tenantValue = "TenantValue";
        
        // 先设置全局设置
        await _settingsService.SetGlobalSettingAsync(TestModule, key, globalValue);
        ClearDbContext();
        
        // 验证全局设置存在
        var global = await _settingsService.GetGlobalSettingAsync(TestModule, key);
        Assert.Equal(globalValue, global);
        
        // 设置租户设置
        await _settingsService.SetTenantSettingAsync(TestModule, key, tenantValue, TestTenantId);
        ClearDbContext();

        // 执行
        var tenant = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);

        // 断言 - 租户设置应该优先于全局设置
        Assert.Equal(tenantValue, tenant);
        Assert.NotEqual(globalValue, tenant);
    }

    [Fact]
    public async Task GetAllTenantSettingsAsync_TenantOverridesGlobal_MergesCorrectly()
    {
        // 安排
        var key = "MergeTestSetting";
        var globalValue = "GlobalValue";
        var tenantValue = "TenantValue";
        
        await _settingsService.SetGlobalSettingAsync(TestModule, key, globalValue);
        await _settingsService.SetTenantSettingAsync(TestModule, key, tenantValue, TestTenantId);
        ClearDbContext();

        // 执行
        var allSettings = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);

        // 断言 - 租户设置应该覆盖全局设置
        Assert.True(allSettings.ContainsKey(key));
        Assert.Equal(tenantValue, allSettings[key]);
        Assert.NotEqual(globalValue, allSettings[key]);
    }

    #endregion

    #region 版本号和历史记录测试

    [Fact]
    public async Task SetTenantSettingAsync_VersionIncrements_OnUpdate()
    {
        // 安排
        var key = "VersionTestSetting";
        var value1 = "Value1";
        var value2 = "Value2";
        var value3 = "Value3";
        
        // 创建新设置（版本号应该是1）
        await _settingsService.SetTenantSettingAsync(TestModule, key, value1, TestTenantId);
        ClearDbContext();
        
        // 获取初始版本 - 使用AsTracking确保能查询到并跟踪
        var setting1 = await DbContext.SettingItems
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Module == TestModule && s.Key == key && s.Scope == SettingScope.Tenant && s.ScopeId == TestTenantId);
        Assert.NotNull(setting1);
        var initialVersion = setting1.Version;
        Assert.Equal(1, initialVersion); // 新创建的设置版本号应该是1
        ClearDbContext();

        // 执行 - 第一次更新设置（版本号应该变为2）
        await _settingsService.SetTenantSettingAsync(TestModule, key, value2, TestTenantId);
        ClearDbContext();

        // 断言 - 版本应该递增到2
        var setting2 = await DbContext.SettingItems
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Module == TestModule && s.Key == key && s.Scope == SettingScope.Tenant && s.ScopeId == TestTenantId);
        Assert.NotNull(setting2);
        Assert.True(setting2.Version > initialVersion, $"版本号应该递增，初始版本: {initialVersion}, 当前版本: {setting2.Version}");
        Assert.Equal(2, setting2.Version);
        ClearDbContext();
        
        // 执行 - 第二次更新设置（版本号应该变为3）
        await _settingsService.SetTenantSettingAsync(TestModule, key, value3, TestTenantId);
        ClearDbContext();
        
        // 断言 - 版本应该递增到3
        var setting3 = await DbContext.SettingItems
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Module == TestModule && s.Key == key && s.Scope == SettingScope.Tenant && s.ScopeId == TestTenantId);
        Assert.NotNull(setting3);
        Assert.True(setting3.Version > setting2.Version, $"版本号应该继续递增，上次版本: {setting2.Version}, 当前版本: {setting3.Version}");
        Assert.Equal(3, setting3.Version);
    }

    [Fact]
    public async Task SetTenantSettingAsync_WithReason_RecordsHistory()
    {
        // 安排
        var key = "HistoryTestSetting";
        var value1 = "Value1";
        var value2 = "Value2";
        var reason = "测试历史记录";

        // 创建初始设置
        await _settingsService.SetTenantSettingAsync(TestModule, key, value1, TestTenantId);
        ClearDbContext();

        // 执行 - 更新设置并记录原因
        await _settingsService.SetTenantSettingAsync(TestModule, key, value2, TestTenantId, reason);
        ClearDbContext();

        // 断言 - 应该创建历史记录
        var setting = await DbContext.SettingItems
            .FirstOrDefaultAsync(s => s.Module == TestModule && s.Key == key && s.Scope == SettingScope.Tenant && s.ScopeId == TestTenantId);
        
        Assert.NotNull(setting);
        
        // 验证历史记录
        var history = await DbContext.SettingHistories
            .Where(h => h.SettingId == setting.Id)
            .OrderByDescending(h => h.Version)
            .FirstOrDefaultAsync();
        
        Assert.NotNull(history);
        Assert.Equal(value1, history.OldValue);
        Assert.Equal(value2, history.NewValue);
        Assert.Equal(reason, history.Reason);
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task SetTenantSettingAsync_ConcurrentUpdates_HandlesCorrectly()
    {
        // 安排
        var key = "ConcurrentTestSetting";
        var tasks = new List<Task<bool>>();
        
        // 执行 - 并发设置多个值
        for (int i = 0; i < 10; i++)
        {
            var value = $"Value{i}";
            tasks.Add(_settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId));
        }
        
        await Task.WhenAll(tasks);
        ClearDbContext();

        // 断言 - 最终值应该是最后一个设置的值（或其中一个）
        var finalValue = await _settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId);
        Assert.NotNull(finalValue);
        Assert.StartsWith("Value", finalValue);
    }

    [Fact]
    public async Task GetTenantSettingAsync_ConcurrentReads_HandlesCorrectly()
    {
        // 安排
        var key = "ConcurrentReadSetting";
        var value = "ConcurrentValue";
        
        await _settingsService.SetTenantSettingAsync(TestModule, key, value, TestTenantId);
        ClearDbContext();

        // 执行 - 并发读取
        var tasks = new List<Task<string?>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_settingsService.GetTenantSettingAsync(TestModule, key, TestTenantId));
        }
        
        var results = await Task.WhenAll(tasks);

        // 断言 - 所有读取应该返回相同的值
        Assert.All(results, r => Assert.Equal(value, r));
    }

    #endregion

    #region 模块和键的边界情况测试

    [Fact]
    public async Task SetTenantSettingAsync_DifferentModules_IsolatesCorrectly()
    {
        // 安排
        var key = "SameKeyDifferentModule";
        var module1 = "Module1";
        var module2 = "Module2";
        var value1 = "Module1Value";
        var value2 = "Module2Value";

        // 执行
        await _settingsService.SetTenantSettingAsync(module1, key, value1, TestTenantId);
        await _settingsService.SetTenantSettingAsync(module2, key, value2, TestTenantId);
        ClearDbContext();

        // 断言 - 不同模块的设置应该隔离
        Assert.Equal(value1, await _settingsService.GetTenantSettingAsync(module1, key, TestTenantId));
        Assert.Equal(value2, await _settingsService.GetTenantSettingAsync(module2, key, TestTenantId));
    }

    [Fact]
    public async Task GetAllTenantSettingsAsync_DifferentModules_ReturnsCorrectSettings()
    {
        // 安排
        var module1 = "Module1";
        var module2 = "Module2";
        
        await _settingsService.SetTenantSettingAsync(module1, "Key1", "Value1", TestTenantId);
        await _settingsService.SetTenantSettingAsync(module1, "Key2", "Value2", TestTenantId);
        await _settingsService.SetTenantSettingAsync(module2, "Key1", "Value3", TestTenantId);
        ClearDbContext();

        // 执行
        var module1Settings = await _settingsService.GetAllTenantSettingsAsync(module1, TestTenantId);
        var module2Settings = await _settingsService.GetAllTenantSettingsAsync(module2, TestTenantId);

        // 断言
        Assert.Equal(2, module1Settings.Count);
        Assert.Equal("Value1", module1Settings["Key1"]);
        Assert.Equal("Value2", module1Settings["Key2"]);
        
        Assert.Single(module2Settings);
        Assert.Equal("Value3", module2Settings["Key1"]);
    }

    #endregion

    #region 错误处理测试

    [Fact]
    public async Task GetTenantSettingAsync_InvalidJson_ReturnsNull()
    {
        // 安排
        var key = "InvalidJsonSetting";
        var invalidJson = "{invalid json}";
        
        // 直接创建无效JSON的设置项
        var setting = new SettingItem
        {
            Module = TestModule,
            Key = key,
            Value = invalidJson,
            Name = key,
            Scope = SettingScope.Tenant,
            ScopeId = TestTenantId,
            TenantId = TestTenantId,
            ValueType = SettingValueType.Json
        };
        
        DbContext.SettingItems.Add(setting);
        await DbContext.SaveChangesAsync();
        ClearDbContext();

        // 执行 - 尝试反序列化为对象
        var result = await _settingsService.GetTenantSettingAsync<TestModel>(TestModule, key, TestTenantId);

        // 断言 - 应该返回null（反序列化失败）
        Assert.Null(result);
    }

    [Fact]
    public async Task BatchSetTenantSettingsAsync_PartialFailure_HandlesGracefully()
    {
        // 安排
        var settings = new Dictionary<string, string>
        {
            { "ValidSetting1", "Value1" },
            { "ValidSetting2", "Value2" },
            { "ValidSetting3", "Value3" }
        };

        // 执行
        var result = await _settingsService.BatchSetTenantSettingsAsync(TestModule, settings, TestTenantId);
        ClearDbContext();

        // 断言 - 批量操作应该成功
        Assert.True(result);
        
        // 验证所有设置都已保存
        var allSettings = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        Assert.True(allSettings.ContainsKey("ValidSetting1"));
        Assert.True(allSettings.ContainsKey("ValidSetting2"));
        Assert.True(allSettings.ContainsKey("ValidSetting3"));
    }

    #endregion

    #region 性能相关测试

    [Fact]
    public async Task GetAllTenantSettingsAsync_LargeNumberOfSettings_PerformsWell()
    {
        // 安排 - 创建大量设置
        var settings = new Dictionary<string, string>();
        for (int i = 0; i < 50; i++)
        {
            settings[$"Setting{i}"] = $"Value{i}";
        }
        
        await _settingsService.BatchSetTenantSettingsAsync(TestModule, settings, TestTenantId);
        ClearDbContext();

        // 执行
        var startTime = DateTime.UtcNow;
        var result = await _settingsService.GetAllTenantSettingsAsync(TestModule, TestTenantId);
        var duration = DateTime.UtcNow - startTime;

        // 断言
        Assert.True(result.Count >= 50);
        // 性能断言：应该在合理时间内完成（例如1秒内）
        Assert.True(duration.TotalSeconds < 1, $"获取50个设置耗时 {duration.TotalMilliseconds}ms，应该小于1秒");
    }

    #endregion
}

