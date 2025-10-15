using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.ExamApi.Extensions;

/// <summary>
/// 分布式缓存扩展方法
/// </summary>
public static class DistributedCacheExtensions
{
    /// <summary>
    /// 从缓存获取对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="cache">分布式缓存</param>
    /// <param name="key">缓存键</param>
    /// <returns>缓存的对象，不存在返回default</returns>
    public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key)
    {
        var data = await cache.GetAsync(key);
        if (data == null) return default;

        var json = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// 设置缓存对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="cache">分布式缓存</param>
    /// <param name="key">缓存键</param>
    /// <param name="value">要缓存的对象</param>
    /// <param name="options">缓存选项</param>
    public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options)
    {
        var json = JsonConvert.SerializeObject(value);
        var data = Encoding.UTF8.GetBytes(json);
        await cache.SetAsync(key, data, options);
    }

    /// <summary>
    /// 设置缓存对象（带过期时间）
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="cache">分布式缓存</param>
    /// <param name="key">缓存键</param>
    /// <param name="value">要缓存的对象</param>
    /// <param name="absoluteExpiration">绝对过期时间</param>
    /// <param name="slidingExpiration">滑动过期时间</param>
    public static async Task SetAsync<T>(
        this IDistributedCache cache, 
        string key, 
        T value, 
        TimeSpan? absoluteExpiration = null, 
        TimeSpan? slidingExpiration = null)
    {
        var options = new DistributedCacheEntryOptions();
        
        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
        }
        
        if (slidingExpiration.HasValue)
        {
            options.SlidingExpiration = slidingExpiration;
        }

        await SetAsync(cache, key, value, options);
    }
}

