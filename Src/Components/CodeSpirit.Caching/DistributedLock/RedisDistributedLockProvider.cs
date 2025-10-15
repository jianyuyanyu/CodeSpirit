using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CodeSpirit.Caching.DistributedLock;

/// <summary>
/// 基于Redis的分布式锁提供程序
/// </summary>
public class RedisDistributedLockProvider : IDistributedLockProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockProvider> _logger;
    private readonly RedisDistributedLockOptions _options;

    // Lua脚本：原子性释放锁（只有持有锁的客户端才能释放）
    private const string ReleaseLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    // Lua脚本：原子性延长锁的过期时间
    private const string ExtendLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('expire', KEYS[1], ARGV[2])
        else
            return 0
        end";

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
        var retryCount = 0;

        try
        {
            // 尝试获取锁，直到超时
            while (!acquired && DateTime.UtcNow - startTime < timeout && retryCount < _options.MaxRetryCount)
            {
                acquired = await db.StringSetAsync(lockKey, lockValue, lockTtl, When.NotExists);

                if (!acquired)
                {
                    retryCount++;
                    // 短暂等待后重试
                    await Task.Delay(_options.RetryInterval);
                }
            }

            if (acquired)
            {
                _logger.LogDebug("已获得分布式锁: {LockKey}, 值: {LockValue}, 重试次数: {RetryCount}", 
                    lockKey, lockValue, retryCount);
                
                return new DistributedLock(
                    this, 
                    key, 
                    lockValue, 
                    _options.EnableWatchdog, 
                    _options.GetWatchdogInterval(), 
                    lockTtl);
            }
            else
            {
                _logger.LogWarning("获取分布式锁超时: {LockKey}, 超时时间: {Timeout}, 重试次数: {RetryCount}", 
                    lockKey, timeout, retryCount);
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
    /// 尝试异步获取分布式锁（不等待）
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="ttl">锁的生存时间</param>
    /// <returns>锁对象，如果获取失败返回null</returns>
    public async Task<IDisposable?> TryAcquireLockAsync(string key, TimeSpan? ttl = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var db = _redis.GetDatabase();
        var lockKey = GetFullLockKey(key);
        var lockValue = Guid.NewGuid().ToString();
        var lockTtl = ttl ?? _options.DefaultLockTimeout;

        try
        {
            var acquired = await db.StringSetAsync(lockKey, lockValue, lockTtl, When.NotExists);

            if (acquired)
            {
                _logger.LogDebug("成功获得分布式锁: {LockKey}, 值: {LockValue}", lockKey, lockValue);
                return new DistributedLock(
                    this, 
                    key, 
                    lockValue, 
                    _options.EnableWatchdog, 
                    _options.GetWatchdogInterval(), 
                    lockTtl);
            }
            else
            {
                _logger.LogDebug("获取分布式锁失败，锁已被占用: {LockKey}", lockKey);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试获取分布式锁时发生错误: {LockKey}", lockKey);
            return null;
        }
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
    /// 异步释放分布式锁（使用锁值验证）
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="lockValue">锁的值</param>
    /// <returns>是否成功释放锁</returns>
    public async Task<bool> ReleaseLockAsync(string key, string lockValue)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(lockValue))
            throw new ArgumentNullException(nameof(lockValue));

        var db = _redis.GetDatabase();
        var lockKey = GetFullLockKey(key);

        try
        {
            var result = await db.ScriptEvaluateAsync(ReleaseLockScript, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
            var released = (int)result == 1;
            
            if (released)
            {
                _logger.LogDebug("已释放分布式锁: {LockKey}, 值: {LockValue}", lockKey, lockValue);
            }
            else
            {
                _logger.LogWarning("释放分布式锁失败，锁值不匹配或已过期: {LockKey}, 值: {LockValue}", lockKey, lockValue);
            }
            return released;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放分布式锁时发生错误: {LockKey}, 值: {LockValue}", lockKey, lockValue);
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
    /// 异步延长锁的过期时间
    /// </summary>
    /// <param name="key">锁的键名</param>
    /// <param name="ttl">新的生存时间</param>
    /// <returns>是否成功延长</returns>
    public async Task<bool> ExtendLockAsync(string key, TimeSpan ttl)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var db = _redis.GetDatabase();
        var lockKey = GetFullLockKey(key);

        try
        {
            var result = await db.KeyExpireAsync(lockKey, ttl);
            if (result)
            {
                _logger.LogDebug("已延长分布式锁过期时间: {LockKey}, TTL: {TTL}", lockKey, ttl);
            }
            else
            {
                _logger.LogWarning("延长分布式锁过期时间失败，锁可能不存在: {LockKey}", lockKey);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长分布式锁过期时间时发生错误: {LockKey}", lockKey);
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
