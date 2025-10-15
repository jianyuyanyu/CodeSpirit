namespace CodeSpirit.Caching.Models;

/// <summary>
/// 缓存级别枚举
/// </summary>
public enum CacheLevel
{
    /// <summary>
    /// 仅使用L1缓存（内存缓存）
    /// </summary>
    L1Only = 1,

    /// <summary>
    /// 仅使用L2缓存（分布式缓存）
    /// </summary>
    L2Only = 2,

    /// <summary>
    /// 使用两级缓存（L1 + L2）
    /// </summary>
    Both = 3,

    /// <summary>
    /// 自动选择最佳缓存级别
    /// </summary>
    Auto = 4
}
