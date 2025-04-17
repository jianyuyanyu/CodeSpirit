using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CodeSpirit.Shared.DistributedLock
{
    /// <summary>
    /// 基于Redis的分布式锁提供程序
    /// </summary>
    public class RedisDistributedLockProvider : IDistributedLockProvider
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisDistributedLockProvider> _logger;
        private readonly RedisDistributedLockOptions _options;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="redis">Redis连接</param>
        /// <param name="options">Redis分布式锁选项</param>
        /// <param name="logger">日志记录器</param>
        public RedisDistributedLockProvider(
            IConnectionMultiplexer redis,
            IOptions<RedisDistributedLockOptions> options,
            ILogger<RedisDistributedLockProvider> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 异步获取分布式锁
        /// </summary>
        /// <param name="key">锁的键名</param>
        /// <param name="timeout">获取锁的超时时间</param>
        /// <param name="ttl">锁的生存时间，如果未在此时间内释放，锁将自动过期</param>
        /// <returns>表示锁的对象，实现IDisposable接口用于释放锁</returns>
        public async Task<IDisposable> AcquireLockAsync(string key, TimeSpan timeout, TimeSpan? ttl = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var db = _redis.GetDatabase();
            var lockKey = GetFullLockKey(key);
            var lockValue = Guid.NewGuid().ToString();
            var lockTtl = ttl ?? _options.DefaultLockTimeout;
            var startTime = DateTime.UtcNow;
            var acquired = false;

            try
            {
                // 尝试获取锁，直到超时
                while (!acquired && DateTime.UtcNow - startTime < timeout)
                {
                    acquired = await db.StringSetAsync(lockKey, lockValue, lockTtl, When.NotExists);

                    if (!acquired)
                    {
                        // 短暂等待后重试
                        await Task.Delay(_options.RetryInterval);
                    }
                }

                if (acquired)
                {
                    _logger.LogDebug("已获得分布式锁: {LockKey}", lockKey);
                    return new DistributedLock(this, key);
                }
                else
                {
                    _logger.LogWarning("获取分布式锁超时: {LockKey}, 超时时间: {Timeout}", lockKey, timeout);
                    throw new TimeoutException($"获取分布式锁超时: {lockKey}");
                }
            }
            catch (Exception ex) when (ex is not TimeoutException)
            {
                _logger.LogError(ex, "获取分布式锁时发生错误: {LockKey}", lockKey);
                throw;
            }
        }

        /// <summary>
        /// 异步获取分布式锁，使用默认超时时间
        /// </summary>
        /// <param name="key">锁的键名</param>
        /// <param name="ttl">锁的生存时间，如果未在此时间内释放，锁将自动过期</param>
        /// <returns>表示锁的对象，实现IDisposable接口用于释放锁</returns>
        public Task<IDisposable> AcquireLockAsync(string key, TimeSpan? ttl = null)
        {
            return AcquireLockAsync(key, _options.DefaultAcquireTimeout, ttl);
        }

        /// <summary>
        /// 异步释放分布式锁
        /// </summary>
        /// <param name="key">锁的键名</param>
        /// <returns>是否成功释放锁</returns>
        public async Task<bool> ReleaseLockAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var db = _redis.GetDatabase();
            var lockKey = GetFullLockKey(key);

            try
            {
                var result = await db.KeyDeleteAsync(lockKey);
                if (result)
                {
                    _logger.LogDebug("已释放分布式锁: {LockKey}", lockKey);
                }
                else
                {
                    _logger.LogWarning("释放分布式锁失败，可能已过期: {LockKey}", lockKey);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放分布式锁时发生错误: {LockKey}", lockKey);
                return false;
            }
        }

        /// <summary>
        /// 异步检查锁是否存在
        /// </summary>
        /// <param name="key">锁的键名</param>
        /// <returns>锁是否存在</returns>
        public async Task<bool> IsLockedAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var db = _redis.GetDatabase();
            var lockKey = GetFullLockKey(key);

            try
            {
                return await db.KeyExistsAsync(lockKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查分布式锁状态时发生错误: {LockKey}", lockKey);
                return false;
            }
        }

        /// <summary>
        /// 获取完整的锁键名
        /// </summary>
        /// <param name="key">原始键名</param>
        /// <returns>带前缀的完整键名</returns>
        private string GetFullLockKey(string key) => $"{_options.KeyPrefix}{key}";
    }
} 