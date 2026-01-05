using System.Collections.Concurrent;
using System.Reflection;
using CodeSpirit.Settings.Attributes;

namespace CodeSpirit.Settings.Helpers;

/// <summary>
/// 设置 DTO 辅助类，用于从特性中提取模块和配置键信息
/// </summary>
public static class SettingsDtoHelper
{
    // 缓存：Type -> (Module, Key)，确保每个类型只反射一次
    private static readonly ConcurrentDictionary<Type, (string Module, string Key)> _cache = new();
    
    /// <summary>
    /// 获取泛型类型的设置键信息
    /// </summary>
    /// <typeparam name="T">设置 DTO 类型</typeparam>
    /// <returns>模块名称和配置键的元组</returns>
    /// <exception cref="InvalidOperationException">当类型未标记 [SettingsDto] 特性时抛出</exception>
    public static (string Module, string Key) GetSettingsKey<T>() where T : class
    {
        return GetSettingsKey(typeof(T));
    }
    
    /// <summary>
    /// 获取类型的设置键信息
    /// </summary>
    /// <param name="type">设置 DTO 类型</param>
    /// <returns>模块名称和配置键的元组</returns>
    /// <exception cref="InvalidOperationException">当类型未标记 [SettingsDto] 特性时抛出</exception>
    public static (string Module, string Key) GetSettingsKey(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        
        return _cache.GetOrAdd(type, t =>
        {
            var attribute = t.GetCustomAttribute<SettingsDtoAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"类型 {t.Name} 未标记 [SettingsDto] 特性，" +
                    $"请添加 [SettingsDto(\"Module\", \"Key\")] 特性或使用带参数的方法。");
            }
            return (attribute.Module, attribute.Key);
        });
    }
    
    /// <summary>
    /// 清除缓存（主要用于单元测试）
    /// </summary>
    public static void ClearCache() => _cache.Clear();
}

