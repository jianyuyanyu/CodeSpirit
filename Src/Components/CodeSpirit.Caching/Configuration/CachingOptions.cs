using CodeSpirit.Caching.Models;

namespace CodeSpirit.Caching.Configuration;

/// <summary>
/// 缓存配置选项
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// 是否启用L1缓存（内存缓存）
    /// </summary>
    public bool EnableL1Cache { get; set; } = true;

    /// <summary>
    /// 是否启用L2缓存（分布式缓存）
    /// </summary>
    public bool EnableL2Cache { get; set; } = true;

    /// <summary>
    /// 默认缓存过期时间
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// L1缓存默认过期时间
    /// </summary>
    public TimeSpan DefaultL1Expiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// L2缓存默认过期时间
    /// </summary>
    public TimeSpan DefaultL2Expiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 默认滑动过期时间
    /// </summary>
    public TimeSpan? DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "CodeSpirit:Cache:";

    /// <summary>
    /// 是否启用缓存击穿保护
    /// </summary>
    public bool EnableBreakthroughProtection { get; set; } = true;

    /// <summary>
    /// 缓存击穿保护的锁超时时间
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 是否启用缓存压缩
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// 压缩阈值（字节）
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// 序列化器类型
    /// </summary>
    public CacheSerializerType SerializerType { get; set; } = CacheSerializerType.Json;

    /// <summary>
    /// 是否启用缓存统计
    /// </summary>
    public bool EnableStatistics { get; set; } = false;

    /// <summary>
    /// 统计数据保留时间
    /// </summary>
    public TimeSpan StatisticsRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// L1缓存配置
    /// </summary>
    public L1CacheOptions L1Cache { get; set; } = new();

    /// <summary>
    /// L2缓存配置
    /// </summary>
    public L2CacheOptions L2Cache { get; set; } = new();

    /// <summary>
    /// 预热配置
    /// </summary>
    public WarmupOptions Warmup { get; set; } = new();

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <returns>验证结果</returns>
    public bool Validate()
    {
        if (!EnableL1Cache && !EnableL2Cache)
        {
            return false; // 至少要启用一种缓存
        }

        if (DefaultExpiration <= TimeSpan.Zero)
        {
            return false;
        }

        if (EnableL1Cache && DefaultL1Expiration <= TimeSpan.Zero)
        {
            return false;
        }

        if (EnableL2Cache && DefaultL2Expiration <= TimeSpan.Zero)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// L1缓存（内存缓存）配置
/// </summary>
public class L1CacheOptions
{
    /// <summary>
    /// 内存缓存大小限制（MB）
    /// </summary>
    public int SizeLimit { get; set; } = 100;

    /// <summary>
    /// 压缩比例（0.0-1.0）
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;

    /// <summary>
    /// 扫描频率
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 默认优先级
    /// </summary>
    public CachePriority DefaultPriority { get; set; } = CachePriority.Normal;
}

/// <summary>
/// L2缓存（分布式缓存）配置
/// </summary>
public class L2CacheOptions
{
    /// <summary>
    /// 连接字符串名称
    /// </summary>
    public string ConnectionName { get; set; } = "cache";

    /// <summary>
    /// 数据库编号
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// 键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "L2:";

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// 是否在连接失败时中止
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;
}

/// <summary>
/// 预热配置
/// </summary>
public class WarmupOptions
{
    /// <summary>
    /// 是否启用自动预热
    /// </summary>
    public bool EnableAutoWarmup { get; set; } = false;

    /// <summary>
    /// 预热并发度
    /// </summary>
    public int Concurrency { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 预热超时时间
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 预热失败重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 预热失败重试间隔
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 是否在应用启动时预热
    /// </summary>
    public bool WarmupOnStartup { get; set; } = false;

    /// <summary>
    /// 启动预热延迟时间
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// 缓存序列化器类型
/// </summary>
public enum CacheSerializerType
{
    /// <summary>
    /// JSON序列化器
    /// </summary>
    Json = 1,

    /// <summary>
    /// MessagePack序列化器
    /// </summary>
    MessagePack = 2,

    /// <summary>
    /// 二进制序列化器
    /// </summary>
    Binary = 3
}
