using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodeSpirit.Caching.Services;

/// <summary>
/// Redis缓存管理服务实现
/// </summary>
public class RedisCacheManagementService : ICacheManagementService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheManagementService> _logger;
    private readonly CachingOptions _options;
    private readonly IDatabase _database;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis连接</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="logger">日志记录器</param>
    public RedisCacheManagementService(
        IConnectionMultiplexer redis,
        IOptions<CachingOptions> options,
        ILogger<RedisCacheManagementService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();
    }

    /// <summary>
    /// 获取缓存键列表（支持模式匹配和分页）
    /// </summary>
    public async Task<PageList<CacheKeyInfo>> GetKeysAsync(
        string? pattern = null,
        string? tenantId = null,
        int page = 1,
        int perPage = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var searchPattern = BuildSearchPattern(pattern, tenantId);
            
            var allKeys = new List<RedisKey>();
            
            // 使用 SCAN 命令避免阻塞
            await foreach (var key in server.KeysAsync(pattern: searchPattern))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 如果指定了租户ID，进一步过滤
                if (!string.IsNullOrEmpty(tenantId) && !IsTenantKey(key, tenantId))
                    continue;

                allKeys.Add(key);
            }

            // 内存分页
            var total = allKeys.Count;
            var skip = (page - 1) * perPage;
            var pagedKeys = allKeys.Skip(skip).Take(perPage).ToList();

            // 批量获取键信息
            var keyInfos = new List<CacheKeyInfo>();
            foreach (var key in pagedKeys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var info = await GetKeyInfoAsync(key);
                if (info != null)
                {
                    keyInfos.Add(info);
                }
            }

            _logger.LogDebug("获取缓存键列表成功，模式: {Pattern}, 租户: {TenantId}, 总数: {Total}, 当前页: {Page}", 
                searchPattern, tenantId ?? "全部", total, page);

            return new PageList<CacheKeyInfo>(keyInfos, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存键列表失败，模式: {Pattern}, 租户: {TenantId}", pattern, tenantId);
            throw;
        }
    }

    /// <summary>
    /// 获取指定缓存键的详细信息
    /// </summary>
    public async Task<CacheValueInfo?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var redisKey = (RedisKey)key;
            
            // 检查键是否存在
            if (!await _database.KeyExistsAsync(redisKey))
            {
                return null;
            }

            // 获取键类型
            var type = await _database.KeyTypeAsync(redisKey);
            var typeName = type.ToString().ToLowerInvariant();

            // 获取TTL
            var ttl = await _database.KeyTimeToLiveAsync(redisKey);
            var ttlSeconds = ttl?.TotalSeconds ?? -1;

            // 获取值
            string valueJson = string.Empty;
            long? size = null;

            switch (type)
            {
                case RedisType.String:
                    var stringValue = await _database.StringGetAsync(redisKey);
                    valueJson = stringValue.HasValue ? stringValue.ToString() : string.Empty;
                    size = stringValue.HasValue ? Encoding.UTF8.GetByteCount(stringValue.ToString()) : 0;
                    break;

                case RedisType.Hash:
                    var hashFields = await _database.HashGetAllAsync(redisKey);
                    var hashDict = hashFields.ToDictionary(
                        h => h.Name.ToString(),
                        h => h.Value.ToString());
                    valueJson = JsonSerializer.Serialize(hashDict);
                    size = hashFields.Sum(h => h.Name.Length() + h.Value.Length());
                    break;

                case RedisType.List:
                    var listLength = await _database.ListLengthAsync(redisKey);
                    var listValues = new List<string>();
                    for (int i = 0; i < Math.Min(listLength, 100); i++) // 限制最多100个元素
                    {
                        var item = await _database.ListGetByIndexAsync(redisKey, i);
                        if (item.HasValue)
                            listValues.Add(item.ToString());
                    }
                    valueJson = JsonSerializer.Serialize(listValues);
                    size = listValues.Sum(v => Encoding.UTF8.GetByteCount(v));
                    break;

                case RedisType.Set:
                    var setMembers = await _database.SetMembersAsync(redisKey);
                    var setValues = setMembers.Select(m => m.ToString()).ToList();
                    valueJson = JsonSerializer.Serialize(setValues);
                    size = setValues.Sum(v => Encoding.UTF8.GetByteCount(v));
                    break;

                case RedisType.SortedSet:
                    var sortedSetMembers = await _database.SortedSetRangeByRankAsync(redisKey, 0, 99); // 限制最多100个元素
                    var sortedSetValues = sortedSetMembers.Select(m => m.ToString()).ToList();
                    valueJson = JsonSerializer.Serialize(sortedSetValues);
                    size = sortedSetValues.Sum(v => Encoding.UTF8.GetByteCount(v));
                    break;

                default:
                    valueJson = $"不支持的类型: {typeName}";
                    break;
            }

            // 尝试获取内存大小（如果Redis支持MEMORY USAGE命令）
            try
            {
                var memoryUsage = await _database.ExecuteAsync("MEMORY", "USAGE", key);
                if (memoryUsage.Type == ResultType.Integer)
                {
                    size = (long)memoryUsage;
                }
            }
            catch
            {
                // MEMORY命令可能不支持，忽略错误
            }

            var info = new CacheValueInfo
            {
                Key = key,
                Type = typeName,
                Value = valueJson,
                Ttl = (long)ttlSeconds,
                Size = size
            };

            _logger.LogDebug("获取缓存值详情成功，键: {Key}, 类型: {Type}", key, typeName);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存值详情失败，键: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 删除指定的缓存键
    /// </summary>
    public async Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var redisKey = (RedisKey)key;
            var deleted = await _database.KeyDeleteAsync(redisKey);
            
            _logger.LogInformation("删除缓存键: {Key}, 结果: {Deleted}", key, deleted);
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存键失败，键: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    public async Task<long> DeleteByPatternAsync(string pattern, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentNullException(nameof(pattern));

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var searchPattern = BuildSearchPattern(pattern, tenantId);
            
            var keysToDelete = new List<RedisKey>();
            
            // 使用 SCAN 命令收集要删除的键
            await foreach (var key in server.KeysAsync(pattern: searchPattern))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 如果指定了租户ID，进一步过滤
                if (!string.IsNullOrEmpty(tenantId) && !IsTenantKey(key, tenantId))
                    continue;

                keysToDelete.Add(key);
            }

            // 批量删除
            long deletedCount = 0;
            if (keysToDelete.Count > 0)
            {
                deletedCount = await _database.KeyDeleteAsync(keysToDelete.ToArray());
            }

            _logger.LogInformation("按模式批量删除缓存键，模式: {Pattern}, 租户: {TenantId}, 删除数量: {DeletedCount}", 
                searchPattern, tenantId ?? "全部", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按模式批量删除缓存键失败，模式: {Pattern}, 租户: {TenantId}", pattern, tenantId);
            throw;
        }
    }

    /// <summary>
    /// 清空所有缓存（清空当前数据库）
    /// </summary>
    public async Task<bool> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync(_database.Database);
            
            _logger.LogWarning("清空所有缓存成功，数据库: {Database}", _database.Database);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空所有缓存失败");
            throw;
        }
    }

    /// <summary>
    /// 检查缓存键是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var redisKey = (RedisKey)key;
            return await _database.KeyExistsAsync(redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存键是否存在失败，键: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 获取键信息
    /// </summary>
    private async Task<CacheKeyInfo?> GetKeyInfoAsync(RedisKey key)
    {
        try
        {
            var keyString = key.ToString();
            
            // 检查键是否存在
            if (!await _database.KeyExistsAsync(key))
            {
                return null;
            }

            // 获取键类型
            var type = await _database.KeyTypeAsync(key);
            var typeName = type.ToString().ToLowerInvariant();

            // 获取TTL
            var ttl = await _database.KeyTimeToLiveAsync(key);
            var ttlSeconds = ttl?.TotalSeconds ?? -1;

            // 尝试获取内存大小
            long? size = null;
            try
            {
                var memoryUsage = await _database.ExecuteAsync("MEMORY", "USAGE", keyString);
                if (memoryUsage.Type == ResultType.Integer)
                {
                    size = (long)memoryUsage;
                }
            }
            catch
            {
                // MEMORY命令可能不支持，忽略错误
            }

            return new CacheKeyInfo
            {
                Key = keyString,
                Type = typeName,
                Ttl = (long)ttlSeconds,
                Size = size
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取键信息失败，键: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 构建搜索模式
    /// </summary>
    private string BuildSearchPattern(string? pattern, string? tenantId)
    {
        var searchPattern = pattern ?? "*";

        // 如果指定了租户ID，在模式中添加租户过滤
        if (!string.IsNullOrEmpty(tenantId))
        {
            // 如果模式不包含租户信息，添加租户前缀
            if (!searchPattern.Contains("tenant:" + tenantId, StringComparison.OrdinalIgnoreCase))
            {
                // 检查是否是框架缓存键格式：CodeSpirit:*:data:*
                if (searchPattern.StartsWith(_options.KeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // 在 data 之前插入 tenant:{tenantId}
                    searchPattern = searchPattern.Replace(
                        $"{_options.KeyPrefix}:*:data:",
                        $"{_options.KeyPrefix}:*:data:tenant:{tenantId}:",
                        StringComparison.OrdinalIgnoreCase);
                }
                else if (!searchPattern.Contains("tenant:", StringComparison.OrdinalIgnoreCase))
                {
                    // 如果模式中没有租户信息，添加通配符匹配租户
                    searchPattern = $"*tenant:{tenantId}*{searchPattern}";
                }
            }
        }

        return searchPattern;
    }

    /// <summary>
    /// 检查键是否属于指定租户
    /// </summary>
    private bool IsTenantKey(RedisKey key, string tenantId)
    {
        var keyString = key.ToString();
        
        // 检查键中是否包含租户ID
        // 格式可能是：CodeSpirit:*:data:tenant:{tenantId}:* 或 *tenant:{tenantId}*
        return keyString.Contains($"tenant:{tenantId}", StringComparison.OrdinalIgnoreCase) ||
               keyString.Contains($"Tenant:{tenantId}", StringComparison.OrdinalIgnoreCase);
    }
}

