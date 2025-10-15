namespace CodeSpirit.Caching.DistributedLock;

/// <summary>
/// 分布式锁提供程序接口
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// 异步获取分布式锁
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="timeout">获取锁的超时时间</param>
    /// <param name="ttl">锁的生存时间，如果未在此时间内释放，锁将自动过期</param>
    /// <returns>表示锁的对象，实现IDisposable接口用于释放锁</returns>
    Task<IDisposable> AcquireLockAsync(string key, TimeSpan timeout, TimeSpan? ttl = null);
    
    /// <summary>
    /// 异步获取分布式锁，使用默认超时时间
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="ttl">锁的生存时间，如果未在此时间内释放，锁将自动过期</param>
    /// <returns>表示锁的对象，实现IDisposable接口用于释放锁</returns>
    Task<IDisposable> AcquireLockAsync(string key, TimeSpan? ttl = null);
    
    /// <summary>
    /// 尝试异步获取分布式锁（不等待）
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="ttl">锁的生存时间</param>
    /// <returns>锁对象，如果获取失败返回null</returns>
    Task<IDisposable?> TryAcquireLockAsync(string key, TimeSpan? ttl = null);
    
    /// <summary>
    /// 异步释放分布式锁
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <returns>是否成功释放锁</returns>
    Task<bool> ReleaseLockAsync(string key);
    
    /// <summary>
    /// 异步检查锁是否存在
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <returns>锁是否存在</returns>
    Task<bool> IsLockedAsync(string key);

    /// <summary>
    /// 异步延长锁的过期时间
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="ttl">新的生存时间</param>
    /// <returns>是否成功延长</returns>
    Task<bool> ExtendLockAsync(string key, TimeSpan ttl);
}
