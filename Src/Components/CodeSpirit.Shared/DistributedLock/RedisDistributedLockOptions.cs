using System;

namespace CodeSpirit.Shared.DistributedLock
{
    /// <summary>
    /// Redis分布式锁选项
    /// </summary>
    public class RedisDistributedLockOptions
    {
        /// <summary>
        /// 锁键前缀
        /// </summary>
        public string KeyPrefix { get; set; } = "CodeSpirit:DistLock:";
        
        /// <summary>
        /// 默认锁超时时间（过期时间），默认为30秒
        /// </summary>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// 默认获取锁超时时间，默认为10秒
        /// </summary>
        public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(10);
        
        /// <summary>
        /// 重试获取锁的时间间隔，默认为100毫秒
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(100);
        
        /// <summary>
        /// 是否启用看门狗机制自动续期（未实现）
        /// </summary>
        public bool EnableWatchdog { get; set; } = false;
        
        /// <summary>
        /// 看门狗自动续期间隔时间（未实现），默认为锁超时时间的2/3
        /// </summary>
        public TimeSpan? WatchdogInterval { get; set; } = null;
    }
} 