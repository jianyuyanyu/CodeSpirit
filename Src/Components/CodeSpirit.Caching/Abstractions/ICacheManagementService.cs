using CodeSpirit.Core;

namespace CodeSpirit.Caching.Abstractions;

/// <summary>
/// 缓存管理服务接口
/// 提供缓存键的查询、删除等管理功能
/// </summary>
public interface ICacheManagementService
{
    /// <summary>
    /// 获取缓存键列表（支持模式匹配和分页）
    /// </summary>
    /// <param name="pattern">键名模式（支持通配符，如 CodeSpirit:*:user:*）</param>
    /// <param name="tenantId">租户ID，用于过滤租户相关的缓存键</param>
    /// <param name="page">页码</param>
    /// <param name="perPage">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存键列表</returns>
    Task<PageList<CacheKeyInfo>> GetKeysAsync(
        string? pattern = null,
        string? tenantId = null,
        int page = 1,
        int perPage = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定缓存键的详细信息
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存键详细信息</returns>
    Task<CacheValueInfo?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的缓存键
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    /// <param name="pattern">键名模式（支持通配符）</param>
    /// <param name="tenantId">租户ID，用于过滤租户相关的缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的键数量</returns>
    Task<long> DeleteByPatternAsync(string pattern, string? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空所有缓存（清空当前数据库）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否清空成功</returns>
    Task<bool> ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查缓存键是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// 缓存键信息
/// </summary>
public class CacheKeyInfo
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型（string, hash, list, set, zset等）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（秒），-1表示永不过期，-2表示键不存在
    /// </summary>
    public long Ttl { get; set; }

    /// <summary>
    /// 内存大小（字节），如果Redis不支持则返回null
    /// </summary>
    public long? Size { get; set; }
}

/// <summary>
/// 缓存值信息
/// </summary>
public class CacheValueInfo
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型（string, hash, list, set, zset等）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 值内容（JSON格式）
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（秒），-1表示永不过期，-2表示键不存在
    /// </summary>
    public long Ttl { get; set; }

    /// <summary>
    /// 内存大小（字节），如果Redis不支持则返回null
    /// </summary>
    public long? Size { get; set; }
}

