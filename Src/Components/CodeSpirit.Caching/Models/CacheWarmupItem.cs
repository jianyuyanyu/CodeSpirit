namespace CodeSpirit.Caching.Models;

/// <summary>
/// 缓存预热项
/// </summary>
public class CacheWarmupItem
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 值工厂方法
    /// </summary>
    public Func<Task<object>>? Factory { get; set; }

    /// <summary>
    /// 缓存选项
    /// </summary>
    public CacheOptions? Options { get; set; }

    /// <summary>
    /// 预热优先级
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 是否并行执行
    /// </summary>
    public bool Parallel { get; set; } = true;

    /// <summary>
    /// 预热超时时间
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 预热失败时的重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 预热失败时的重试间隔
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 自定义标签
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// 创建预热项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <returns>预热项</returns>
    public static CacheWarmupItem Create(string key, Func<Task<object>> factory, CacheOptions? options = null)
    {
        return new CacheWarmupItem
        {
            Key = key,
            Factory = factory,
            Options = options
        };
    }

    /// <summary>
    /// 创建带泛型的预热项
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <returns>预热项</returns>
    public static CacheWarmupItem Create<T>(string key, Func<Task<T>> factory, CacheOptions? options = null)
    {
        return new CacheWarmupItem
        {
            Key = key,
            Factory = async () => (object?)await factory(),
            Options = options
        };
    }
}
