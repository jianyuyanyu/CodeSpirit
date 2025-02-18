using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置缓存服务接口
/// </summary>
public interface IConfigCacheService
{
    /// <summary>
    /// 获取缓存的配置值
    /// </summary>
    Task<string> GetAsync(string key);

    /// <summary>
    /// 设置配置缓存
    /// </summary>
    Task SetAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// 移除配置缓存
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// 清除应用的所有配置缓存
    /// </summary>
    Task ClearAppConfigsAsync(string appId, string environment);
} 