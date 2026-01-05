using CodeSpirit.Settings.Attributes;
using CodeSpirit.Settings.Helpers;
using System;
using Xunit;

namespace CodeSpirit.Settings.Tests.Helpers;

/// <summary>
/// SettingsDtoHelper 测试类
/// </summary>
public class SettingsDtoHelperTests
{
    /// <summary>
    /// 测试 DTO 类（带特性）
    /// </summary>
    [SettingsDto("TestModule", "TestKey")]
    public class TestSettingsDto
    {
        public string Name { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 测试 DTO 类（不带特性）
    /// </summary>
    public class DtoWithoutAttribute
    {
        public string Name { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 测试：带有效特性时返回模块和键
    /// </summary>
    [Fact]
    public void GetSettingsKey_WithValidAttribute_ReturnsModuleAndKey()
    {
        // 执行
        var (module, key) = SettingsDtoHelper.GetSettingsKey<TestSettingsDto>();
        
        // 断言
        Assert.Equal("TestModule", module);
        Assert.Equal("TestKey", key);
    }
    
    /// <summary>
    /// 测试：不带特性时抛出异常
    /// </summary>
    [Fact]
    public void GetSettingsKey_WithoutAttribute_ThrowsException()
    {
        // 执行和断言
        var exception = Assert.Throws<InvalidOperationException>(() => 
            SettingsDtoHelper.GetSettingsKey<DtoWithoutAttribute>());
        
        Assert.Contains("未标记 [SettingsDto] 特性", exception.Message);
    }
    
    /// <summary>
    /// 测试：缓存机制，只反射一次
    /// </summary>
    [Fact]
    public void GetSettingsKey_CachesResult_OnlyReflectsOnce()
    {
        // 清除缓存以确保测试干净
        SettingsDtoHelper.ClearCache();
        
        // 第一次调用
        var result1 = SettingsDtoHelper.GetSettingsKey<TestSettingsDto>();
        
        // 第二次调用（应该从缓存获取）
        var result2 = SettingsDtoHelper.GetSettingsKey<TestSettingsDto>();
        
        // 断言：结果应该相同
        Assert.Equal(result1, result2);
        Assert.Equal("TestModule", result1.Module);
        Assert.Equal("TestKey", result1.Key);
    }
    
    /// <summary>
    /// 测试：使用 Type 参数的重载方法
    /// </summary>
    [Fact]
    public void GetSettingsKey_WithTypeParameter_ReturnsModuleAndKey()
    {
        // 执行
        var (module, key) = SettingsDtoHelper.GetSettingsKey(typeof(TestSettingsDto));
        
        // 断言
        Assert.Equal("TestModule", module);
        Assert.Equal("TestKey", key);
    }
    
    /// <summary>
    /// 测试：Type 参数为 null 时抛出异常
    /// </summary>
    [Fact]
    public void GetSettingsKey_WithNullType_ThrowsArgumentNullException()
    {
        // 执行和断言
        Assert.Throws<ArgumentNullException>(() => 
            SettingsDtoHelper.GetSettingsKey(null!));
    }
}

