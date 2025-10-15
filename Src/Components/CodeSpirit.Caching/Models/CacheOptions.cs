namespace CodeSpirit.Caching.Models;

/// <summary>
/// 缓存选项
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// 缓存级别
    /// </summary>
    public CacheLevel Level { get; set; } = CacheLevel.Both;

    /// <summary>
    /// 绝对过期时间
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; set; }

    /// <summary>
    /// 相对于当前时间的绝对过期时间
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// 滑动过期时间
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// L1缓存（内存缓存）的过期时间
    /// </summary>
    public TimeSpan? L1Expiration { get; set; }

    /// <summary>
    /// L2缓存（分布式缓存）的过期时间
    /// </summary>
    public TimeSpan? L2Expiration { get; set; }

    /// <summary>
    /// 是否启用缓存击穿保护
    /// </summary>
    public bool EnableBreakthroughProtection { get; set; } = true;

    /// <summary>
    /// 缓存击穿保护的锁超时时间
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 缓存优先级
    /// </summary>
    public CachePriority Priority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// 是否启用缓存压缩
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// 压缩阈值（字节）
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// 自定义标签，用于批量操作
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// 创建默认缓存选项
    /// </summary>
    /// <returns>默认缓存选项</returns>
    public static CacheOptions Default => new();

    /// <summary>
    /// 创建仅使用L1缓存的选项
    /// </summary>
    /// <param name="expiration">过期时间</param>
    /// <returns>L1缓存选项</returns>
    public static CacheOptions L1Only(TimeSpan? expiration = null) => new()
    {
        Level = CacheLevel.L1Only,
        AbsoluteExpirationRelativeToNow = expiration
    };

    /// <summary>
    /// 创建仅使用L2缓存的选项
    /// </summary>
    /// <param name="expiration">过期时间</param>
    /// <returns>L2缓存选项</returns>
    public static CacheOptions L2Only(TimeSpan? expiration = null) => new()
    {
        Level = CacheLevel.L2Only,
        AbsoluteExpirationRelativeToNow = expiration
    };

    /// <summary>
    /// 创建使用两级缓存的选项
    /// </summary>
    /// <param name="l1Expiration">L1缓存过期时间</param>
    /// <param name="l2Expiration">L2缓存过期时间</param>
    /// <returns>两级缓存选项</returns>
    public static CacheOptions Both(TimeSpan? l1Expiration = null, TimeSpan? l2Expiration = null) => new()
    {
        Level = CacheLevel.Both,
        L1Expiration = l1Expiration,
        L2Expiration = l2Expiration
    };
}
