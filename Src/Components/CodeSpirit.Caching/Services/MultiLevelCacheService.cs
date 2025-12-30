using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace CodeSpirit.Caching.Services;

/// <summary>
/// 多级缓存服务实现
/// </summary>
public class MultiLevelCacheService : ICacheService
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IDistributedLockProvider? _lockProvider;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly CachingOptions _options;
    
    // 用于跟踪L1缓存中的键，以便在L2更新时失效
    private readonly ConcurrentDictionary<string, byte> _l1Keys = new();
    
    // JSON序列化设置，支持接口和抽象类的序列化/反序列化
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto, // 自动处理类型信息
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="memoryCache">内存缓存</param>
    /// <param name="distributedCache">分布式缓存</param>
    /// <param name="lockProvider">分布式锁提供程序</param>
    /// <param name="keyGenerator">缓存键生成器</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="logger">日志记录器</param>
    public MultiLevelCacheService(
        IMemoryCache? memoryCache,
        IDistributedCache? distributedCache,
        IDistributedLockProvider? lockProvider,
        ICacheKeyGenerator keyGenerator,
        IOptions<CachingOptions> options,
        ILogger<MultiLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _lockProvider = lockProvider;
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 验证配置
        if (!_options.Validate())
        {
            throw new InvalidOperationException("缓存配置无效");
        }

        // 至少需要一种缓存
        if (_memoryCache == null && _distributedCache == null)
        {
            throw new InvalidOperationException("至少需要配置一种缓存（内存缓存或分布式缓存）");
        }
    }

    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，如果不存在则返回默认值</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var fullKey = _keyGenerator.GenerateKey("data", key);

        try
        {
            // 尝试从L1缓存获取
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                if (_memoryCache.TryGetValue(fullKey, out var l1Value))
                {
                    _logger.LogDebug("L1缓存命中: {Key}", fullKey);
                    return (T?)l1Value;
                }
            }

            // 尝试从L2缓存获取
            if (_options.EnableL2Cache && _distributedCache != null)
            {
                var l2Data = await _distributedCache.GetAsync(fullKey, cancellationToken);
                if (l2Data != null)
                {
                    _logger.LogDebug("L2缓存命中: {Key}", fullKey);
                    
                    var l2Value = DeserializeValue<T>(l2Data);
                    
                    // 回填到L1缓存
                    if (_options.EnableL1Cache && _memoryCache != null && l2Value != null)
                    {
                        var l1Options = CreateMemoryCacheOptions(CacheOptions.L1Only(_options.DefaultL1Expiration));
                        _memoryCache.Set(fullKey, l2Value, l1Options);
                        _l1Keys.TryAdd(fullKey, 0);
                        _logger.LogDebug("L2缓存值回填到L1缓存: {Key}", fullKey);
                    }
                    
                    return l2Value;
                }
            }

            _logger.LogDebug("缓存未命中: {Key}", fullKey);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存时发生错误: {Key}", fullKey);
            return default;
        }
    }

    /// <summary>
    /// 异步获取缓存值（内部方法，使用已生成的完整键）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="fullKey">已生成的完整缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，如果不存在则返回默认值</returns>
    private async Task<T?> GetAsyncInternal<T>(string fullKey, CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试从L1缓存获取
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                if (_memoryCache.TryGetValue(fullKey, out var l1Value))
                {
                    _logger.LogDebug("L1缓存命中: {Key}", fullKey);
                    return (T?)l1Value;
                }
            }

            // 尝试从L2缓存获取
            if (_options.EnableL2Cache && _distributedCache != null)
            {
                var l2Data = await _distributedCache.GetAsync(fullKey, cancellationToken);
                if (l2Data != null)
                {
                    _logger.LogDebug("L2缓存命中: {Key}", fullKey);
                    
                    var l2Value = DeserializeValue<T>(l2Data);
                    
                    // 回填到L1缓存
                    if (_options.EnableL1Cache && _memoryCache != null && l2Value != null)
                    {
                        var l1Options = CreateMemoryCacheOptions(CacheOptions.L1Only(_options.DefaultL1Expiration));
                        _memoryCache.Set(fullKey, l2Value, l1Options);
                        _l1Keys.TryAdd(fullKey, 0);
                        _logger.LogDebug("L2缓存值回填到L1缓存: {Key}", fullKey);
                    }
                    
                    return l2Value;
                }
            }

            _logger.LogDebug("缓存未命中: {Key}", fullKey);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存时发生错误: {Key}", fullKey);
            return default;
        }
    }

    /// <summary>
    /// 异步获取缓存值，如果不存在则通过工厂方法创建并缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值</returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey("data", key);

        // 先尝试获取现有值（直接使用已生成的完整键）
        var existingValue = await GetAsyncInternal<T>(fullKey, cancellationToken);
        if (existingValue != null)
        {
            return existingValue;
        }

        // 使用分布式锁防止缓存击穿
        if (options.EnableBreakthroughProtection && _lockProvider != null)
        {
            var lockKey = _keyGenerator.GenerateKey("lock", key);
            
            try
            {
                using var lockHandle = await _lockProvider.AcquireLockAsync(lockKey, options.LockTimeout);
                
                // 再次检查缓存（双重检查锁定模式）
                existingValue = await GetAsyncInternal<T>(fullKey, cancellationToken);
                if (existingValue != null)
                {
                    return existingValue;
                }

                // 调用工厂方法获取值
                _logger.LogDebug("调用工厂方法获取值: {Key}", fullKey);
                var value = await factory();
                
                // 缓存新值（直接使用已生成的完整键）
                await SetAsyncInternal(fullKey, value, options, cancellationToken);
                
                return value;
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("获取分布式锁超时，直接调用工厂方法: {Key}", fullKey);
                // 锁超时，直接调用工厂方法（可能导致重复计算，但避免阻塞）
                return await factory();
            }
        }
        else
        {
            // 不使用锁保护，直接调用工厂方法
            var value = await factory();
            await SetAsyncInternal(fullKey, value, options, cancellationToken);
            return value;
        }
    }

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SetAsync<T>(string key, T value, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        options ??= CacheOptions.Default;
        var fullKey = _keyGenerator.GenerateKey("data", key);

        try
        {
            var tasks = new List<Task>();

            // 设置L1缓存
            if (ShouldUseL1Cache(options) && _memoryCache != null)
            {
                var l1Options = CreateMemoryCacheOptions(options);
                _memoryCache.Set(fullKey, value, l1Options);
                _l1Keys.TryAdd(fullKey, 0);
                _logger.LogDebug("已设置L1缓存: {Key}", fullKey);
            }

            // 设置L2缓存
            if (ShouldUseL2Cache(options) && _distributedCache != null)
            {
                var l2Options = CreateDistributedCacheOptions(options);
                var serializedValue = SerializeValue(value);
                tasks.Add(_distributedCache.SetAsync(fullKey, serializedValue, l2Options, cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("已设置L2缓存: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存时发生错误: {Key}", fullKey);
            throw;
        }
    }

    /// <summary>
    /// 异步设置缓存值（内部方法，使用已生成的完整键）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="fullKey">已生成的完整缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SetAsyncInternal<T>(string fullKey, T value, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= CacheOptions.Default;

        try
        {
            var tasks = new List<Task>();

            // 设置L1缓存
            if (ShouldUseL1Cache(options) && _memoryCache != null)
            {
                var l1Options = CreateMemoryCacheOptions(options);
                _memoryCache.Set(fullKey, value, l1Options);
                _l1Keys.TryAdd(fullKey, 0);
                _logger.LogDebug("已设置L1缓存: {Key}", fullKey);
            }

            // 设置L2缓存
            if (ShouldUseL2Cache(options) && _distributedCache != null)
            {
                var l2Options = CreateDistributedCacheOptions(options);
                var serializedValue = SerializeValue(value);
                tasks.Add(_distributedCache.SetAsync(fullKey, serializedValue, l2Options, cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("已设置L2缓存: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存时发生错误: {Key}", fullKey);
            throw;
        }
    }

    /// <summary>
    /// 异步移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var fullKey = _keyGenerator.GenerateKey("data", key);

        try
        {
            var tasks = new List<Task>();

            // 从L1缓存移除
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                _memoryCache.Remove(fullKey);
                _l1Keys.TryRemove(fullKey, out _);
                _logger.LogDebug("已从L1缓存移除: {Key}", fullKey);
            }

            // 从L2缓存移除
            if (_options.EnableL2Cache && _distributedCache != null)
            {
                tasks.Add(_distributedCache.RemoveAsync(fullKey, cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("已从L2缓存移除: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除缓存时发生错误: {Key}", fullKey);
            throw;
        }
    }

    /// <summary>
    /// 异步按模式移除缓存项
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentNullException(nameof(pattern));

        try
        {
            // 从L1缓存移除匹配的键
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                var keysToRemove = _l1Keys.Keys
                    .Where(k => IsPatternMatch(k, pattern))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                    _l1Keys.TryRemove(key, out _);
                }

                _logger.LogDebug("已从L1缓存移除 {Count} 个匹配项: {Pattern}", keysToRemove.Count, pattern);
            }

            // L2缓存的模式移除需要具体的分布式缓存实现支持
            // 这里暂时不实现，因为标准的IDistributedCache接口不支持模式操作
            _logger.LogWarning("L2缓存不支持按模式移除，仅移除了L1缓存中的匹配项: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按模式移除缓存时发生错误: {Pattern}", pattern);
            throw;
        }
    }

    /// <summary>
    /// 异步检查缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var fullKey = _keyGenerator.GenerateKey("data", key);

        try
        {
            // 检查L1缓存
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                if (_memoryCache.TryGetValue(fullKey, out _))
                {
                    return true;
                }
            }

            // 检查L2缓存
            if (_options.EnableL2Cache && _distributedCache != null)
            {
                var l2Data = await _distributedCache.GetAsync(fullKey, cancellationToken);
                return l2Data != null;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存存在性时发生错误: {Key}", fullKey);
            return false;
        }
    }

    /// <summary>
    /// 异步刷新缓存项的过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var fullKey = _keyGenerator.GenerateKey("data", key);

        try
        {
            // L1缓存的刷新通过重新访问实现
            if (_options.EnableL1Cache && _memoryCache != null)
            {
                if (_memoryCache.TryGetValue(fullKey, out var value))
                {
                    // 重新设置以刷新过期时间
                    var options = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.DefaultL1Expiration,
                        SlidingExpiration = _options.DefaultSlidingExpiration
                    };
                    _memoryCache.Set(fullKey, value, options);
                }
            }

            // L2缓存的刷新
            if (_options.EnableL2Cache && _distributedCache != null)
            {
                await _distributedCache.RefreshAsync(fullKey, cancellationToken);
                _logger.LogDebug("已刷新L2缓存: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新缓存时发生错误: {Key}", fullKey);
            throw;
        }
    }

    /// <summary>
    /// 判断是否应该使用L1缓存
    /// </summary>
    /// <param name="options">缓存选项</param>
    /// <returns>是否使用L1缓存</returns>
    private bool ShouldUseL1Cache(CacheOptions options)
    {
        return _options.EnableL1Cache && 
               (options.Level == CacheLevel.L1Only || options.Level == CacheLevel.Both || options.Level == CacheLevel.Auto);
    }

    /// <summary>
    /// 判断是否应该使用L2缓存
    /// </summary>
    /// <param name="options">缓存选项</param>
    /// <returns>是否使用L2缓存</returns>
    private bool ShouldUseL2Cache(CacheOptions options)
    {
        return _options.EnableL2Cache && 
               (options.Level == CacheLevel.L2Only || options.Level == CacheLevel.Both || options.Level == CacheLevel.Auto);
    }

    /// <summary>
    /// 创建内存缓存选项
    /// </summary>
    /// <param name="options">缓存选项</param>
    /// <returns>内存缓存选项</returns>
    private MemoryCacheEntryOptions CreateMemoryCacheOptions(CacheOptions options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions();

        // 标记是否有显式设置的过期时间
        bool hasExplicitExpiration = false;

        if (options.L1Expiration.HasValue)
        {
            memoryCacheOptions.AbsoluteExpirationRelativeToNow = options.L1Expiration;
            hasExplicitExpiration = true;
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            memoryCacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            hasExplicitExpiration = true;
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            memoryCacheOptions.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpiration.Value);
            hasExplicitExpiration = true;
        }
        else
        {
            memoryCacheOptions.AbsoluteExpirationRelativeToNow = _options.DefaultL1Expiration;
        }

        // ✅ 修复：仅在未显式设置任何过期时间时才应用默认滑动过期
        // 如果显式设置了任何过期时间，则不应用默认滑动过期，避免 TTL 被覆盖
        if (options.SlidingExpiration.HasValue)
        {
            memoryCacheOptions.SlidingExpiration = options.SlidingExpiration;
        }
        else if (!hasExplicitExpiration && _options.DefaultSlidingExpiration.HasValue)
        {
            // 仅在未设置任何显式过期时间时才应用默认滑动过期
            memoryCacheOptions.SlidingExpiration = _options.DefaultSlidingExpiration;
        }

        memoryCacheOptions.Priority = options.Priority switch
        {
            CachePriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
            CachePriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
            CachePriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
            CachePriority.NeverRemove => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
            _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
        };

        return memoryCacheOptions;
    }

    /// <summary>
    /// 创建分布式缓存选项
    /// </summary>
    /// <param name="options">缓存选项</param>
    /// <returns>分布式缓存选项</returns>
    private DistributedCacheEntryOptions CreateDistributedCacheOptions(CacheOptions options)
    {
        var distributedCacheOptions = new DistributedCacheEntryOptions();

        // 标记是否有显式设置的过期时间
        bool hasExplicitExpiration = false;

        if (options.L2Expiration.HasValue)
        {
            distributedCacheOptions.AbsoluteExpirationRelativeToNow = options.L2Expiration;
            hasExplicitExpiration = true;
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            distributedCacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            hasExplicitExpiration = true;
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            distributedCacheOptions.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpiration.Value);
            hasExplicitExpiration = true;
        }
        else
        {
            distributedCacheOptions.AbsoluteExpirationRelativeToNow = _options.DefaultL2Expiration;
        }

        // ✅ 修复：仅在未显式设置任何过期时间时才应用默认滑动过期
        // 如果显式设置了任何过期时间，则不应用默认滑动过期，避免 TTL 被覆盖
        if (options.SlidingExpiration.HasValue)
        {
            distributedCacheOptions.SlidingExpiration = options.SlidingExpiration;
        }
        else if (!hasExplicitExpiration && _options.DefaultSlidingExpiration.HasValue)
        {
            // 仅在未设置任何显式过期时间时才应用默认滑动过期
            distributedCacheOptions.SlidingExpiration = _options.DefaultSlidingExpiration;
        }

        return distributedCacheOptions;
    }

    /// <summary>
    /// 序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要序列化的值</param>
    /// <returns>序列化后的字节数组</returns>
    private byte[] SerializeValue<T>(T value)
    {
        if (value == null)
            return Array.Empty<byte>();

        // 关键：传递泛型参数类型 T 给 SerializeObject，以便 TypeNameHandling.Auto 能正确处理接口和抽象类
        var json = JsonConvert.SerializeObject(value, typeof(T), _jsonSettings);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// 反序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="data">要反序列化的字节数组</param>
    /// <returns>反序列化后的值</returns>
    private T? DeserializeValue<T>(byte[] data)
    {
        if (data == null || data.Length == 0)
            return default;

        var json = Encoding.UTF8.GetString(data);
        
        try
        {
            // 尝试使用配置的设置反序列化
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }
        catch (JsonSerializationException ex) when (typeof(T).IsInterface || typeof(T).IsAbstract)
        {
            // 如果是接口或抽象类反序列化失败，尝试查找具体实现类型
            _logger.LogWarning(ex, "接口/抽象类 {Type} 反序列化失败，尝试自动推断具体类型", typeof(T).Name);
            
            // 尝试根据命名空间推断具体类型
            var concreteType = TryGetConcreteType<T>();
            if (concreteType != null)
            {
                try
                {
                    var result = JsonConvert.DeserializeObject(json, concreteType, _jsonSettings);
                    _logger.LogInformation("成功使用具体类型 {ConcreteType} 反序列化接口 {InterfaceType}", 
                        concreteType.Name, typeof(T).Name);
                    return (T?)result;
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "使用推断的具体类型 {ConcreteType} 反序列化失败", concreteType.Name);
                }
            }
            
            // 如果都失败了，记录错误并返回默认值
            _logger.LogError(ex, "无法反序列化类型 {Type}，JSON: {Json}", typeof(T).Name, json);
            return default;
        }
    }
    
    /// <summary>
    /// 尝试获取接口或抽象类的具体实现类型
    /// </summary>
    /// <typeparam name="T">接口或抽象类型</typeparam>
    /// <returns>具体实现类型，如果找不到则返回null</returns>
    private Type? TryGetConcreteType<T>()
    {
        var interfaceType = typeof(T);
        
        // 特殊处理：ITenantInfo -> TenantInfo
        if (interfaceType.Name == "ITenantInfo")
        {
            var tenantInfoType = Type.GetType("CodeSpirit.MultiTenant.Models.TenantInfo, CodeSpirit.MultiTenant");
            if (tenantInfoType != null)
            {
                return tenantInfoType;
            }
        }
        
        // 通用处理：尝试查找同命名空间下去掉 I 前缀的类
        if (interfaceType.IsInterface && interfaceType.Name.StartsWith("I"))
        {
            var potentialTypeName = interfaceType.Name.Substring(1); // 去掉 I 前缀
            var potentialFullName = interfaceType.Namespace?.Replace(".Abstractions", ".Models") + "." + potentialTypeName;
            
            // 尝试从所有已加载的程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(potentialFullName);
                    if (type != null && interfaceType.IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        return type;
                    }
                }
                catch
                {
                    // 忽略加载错误
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// 检查键是否匹配模式
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="pattern">模式</param>
    /// <returns>是否匹配</returns>
    private static bool IsPatternMatch(string key, string pattern)
    {
        // 简单的通配符匹配实现
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }
        
        return key.Contains(pattern);
    }
}
