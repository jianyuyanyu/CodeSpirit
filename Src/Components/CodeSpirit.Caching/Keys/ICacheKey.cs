using CodeSpirit.Caching.Models;

namespace CodeSpirit.Caching.Keys;

/// <summary>
/// 强类型缓存键接口
/// </summary>
/// <typeparam name="T">缓存值类型</typeparam>
public interface ICacheKey<T>
{
    /// <summary>
    /// 获取缓存键（不含全局KeyPrefix）
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// 获取缓存选项
    /// </summary>
    CacheOptions Options { get; }
    
    /// <summary>
    /// 获取缓存标签
    /// </summary>
    IReadOnlyList<string> Tags => Array.Empty<string>();
}

