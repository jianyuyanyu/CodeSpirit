using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 分布式租户存储实现
/// 基于IDistributedCache，支持多实例部署和数据持久化
/// </summary>
public class DistributedTenantStore : ITenantStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedTenantStore> _logger;
    private readonly TenantOptions _options;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;
    
    // 缓存键前缀
    private const string TenantKeyPrefix = "Tenant:";
    private const string ActiveTenantsKey = "ActiveTenants:List";
    private const string TenantExistsPrefix = "TenantExists:";
    
    // 用于跟踪本地已知的租户ID，避免重复查询缓存
    private readonly ConcurrentDictionary<string, bool> _localTenantRegistry;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">多租户配置选项</param>
    public DistributedTenantStore(
        IDistributedCache cache,
        ILogger<DistributedTenantStore> logger,
        IOptions<TenantOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _localTenantRegistry = new ConcurrentDictionary<string, bool>();
        
        // 配置默认缓存选项
        _defaultCacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2)
        };
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo?> GetTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        _logger.LogWarning("==== DistributedTenantStore.GetTenantAsync 被调用，租户ID: {TenantId} ====", tenantId);

        try
        {
            var cacheKey = GetTenantCacheKey(tenantId);
            var cachedData = await _cache.GetAsync(cacheKey);
            
            if (cachedData != null)
            {
                var json = Encoding.UTF8.GetString(cachedData);
                var tenant = JsonConvert.DeserializeObject<TenantInfo>(json);
                
                // 更新本地注册表
                _localTenantRegistry.TryAdd(tenantId, true);
                
                _logger.LogDebug("成功从分布式缓存获取租户信息: {TenantId}", tenantId);
                return tenant;
            }

            _logger.LogDebug("分布式缓存中未找到租户: {TenantId}", tenantId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从分布式缓存获取租户信息失败: {TenantId}", tenantId);
            return null;
        }
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <returns>活跃租户列表</returns>
    public async Task<IEnumerable<ITenantInfo>> GetActiveTenantsAsync()
    {
        try
        {
            var cachedData = await _cache.GetAsync(ActiveTenantsKey);
            
            if (cachedData != null)
            {
                var json = Encoding.UTF8.GetString(cachedData);
                var tenantIds = JsonConvert.DeserializeObject<List<string>>(json);
                
                if (tenantIds?.Any() == true)
                {
                    var tenants = new List<ITenantInfo>();
                    
                    // 并行获取所有租户信息
                    var tasks = tenantIds.Select(async tenantId =>
                    {
                        var tenant = await GetTenantAsync(tenantId);
                        return tenant;
                    });
                    
                    var results = await Task.WhenAll(tasks);
                    tenants.AddRange(results.Where(t => t != null && t.IsActive));
                    
                    _logger.LogDebug("成功从分布式缓存获取 {Count} 个活跃租户", tenants.Count);
                    return tenants;
                }
            }

            _logger.LogDebug("分布式缓存中未找到活跃租户列表");
            return Enumerable.Empty<ITenantInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从分布式缓存获取活跃租户列表失败");
            return Enumerable.Empty<ITenantInfo>();
        }
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>创建结果</returns>
    public async Task<bool> CreateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null || string.IsNullOrWhiteSpace(tenantInfo.TenantId))
        {
            return false;
        }

        try
        {
            // 检查租户是否已存在
            var exists = await TenantExistsAsync(tenantInfo.TenantId);
            if (exists)
            {
                _logger.LogWarning("租户已存在: {TenantId}", tenantInfo.TenantId);
                return false;
            }

            // 缓存租户信息
            var cacheKey = GetTenantCacheKey(tenantInfo.TenantId);
            var json = JsonConvert.SerializeObject(tenantInfo);
            var data = Encoding.UTF8.GetBytes(json);
            
            await _cache.SetAsync(cacheKey, data, _defaultCacheOptions);

            // 更新租户存在性标记
            await SetTenantExistsAsync(tenantInfo.TenantId, true);

            // 如果是活跃租户，更新活跃租户列表
            if (tenantInfo.IsActive)
            {
                await AddToActiveTenantsListAsync(tenantInfo.TenantId);
            }

            // 更新本地注册表
            _localTenantRegistry.TryAdd(tenantInfo.TenantId, true);

            _logger.LogInformation("成功创建租户到分布式缓存: {TenantId}", tenantInfo.TenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建租户到分布式缓存失败: {TenantId}", tenantInfo.TenantId);
            return false;
        }
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="tenantInfo">租户信息</param>
    /// <returns>更新结果</returns>
    public async Task<bool> UpdateTenantAsync(ITenantInfo tenantInfo)
    {
        if (tenantInfo == null || string.IsNullOrWhiteSpace(tenantInfo.TenantId))
        {
            return false;
        }

        try
        {
            // 获取原有租户信息以比较状态变化
            var existingTenant = await GetTenantAsync(tenantInfo.TenantId);
            
            // 更新租户信息
            var cacheKey = GetTenantCacheKey(tenantInfo.TenantId);
            var json = JsonConvert.SerializeObject(tenantInfo);
            var data = Encoding.UTF8.GetBytes(json);
            
            await _cache.SetAsync(cacheKey, data, _defaultCacheOptions);

            // 处理活跃状态变化
            if (existingTenant != null)
            {
                // 从活跃变为非活跃
                if (existingTenant.IsActive && !tenantInfo.IsActive)
                {
                    await RemoveFromActiveTenantsListAsync(tenantInfo.TenantId);
                }
                // 从非活跃变为活跃
                else if (!existingTenant.IsActive && tenantInfo.IsActive)
                {
                    await AddToActiveTenantsListAsync(tenantInfo.TenantId);
                }
            }
            else if (tenantInfo.IsActive)
            {
                // 新的活跃租户
                await AddToActiveTenantsListAsync(tenantInfo.TenantId);
            }

            _logger.LogInformation("成功更新租户到分布式缓存: {TenantId}", tenantInfo.TenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新租户到分布式缓存失败: {TenantId}", tenantInfo.TenantId);
            return false;
        }
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>删除结果</returns>
    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        try
        {
            // 获取租户信息以确定是否需要从活跃列表移除
            var tenant = await GetTenantAsync(tenantId);
            
            // 删除租户信息
            var cacheKey = GetTenantCacheKey(tenantId);
            await _cache.RemoveAsync(cacheKey);

            // 删除租户存在性标记
            var existsKey = GetTenantExistsCacheKey(tenantId);
            await _cache.RemoveAsync(existsKey);

            // 从活跃租户列表移除
            if (tenant?.IsActive == true)
            {
                await RemoveFromActiveTenantsListAsync(tenantId);
            }

            // 从本地注册表移除
            _localTenantRegistry.TryRemove(tenantId, out _);

            _logger.LogInformation("成功从分布式缓存删除租户: {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从分布式缓存删除租户失败: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 检查租户是否存在
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否存在</returns>
    public async Task<bool> TenantExistsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        // 首先检查本地注册表
        if (_localTenantRegistry.ContainsKey(tenantId))
        {
            return true;
        }

        try
        {
            // 检查存在性缓存
            var existsKey = GetTenantExistsCacheKey(tenantId);
            var existsData = await _cache.GetAsync(existsKey);
            
            if (existsData != null)
            {
                var exists = Encoding.UTF8.GetString(existsData) == "true";
                if (exists)
                {
                    _localTenantRegistry.TryAdd(tenantId, true);
                }
                return exists;
            }

            // 如果存在性缓存中没有，尝试直接获取租户信息
            var tenant = await GetTenantAsync(tenantId);
            var tenantExists = tenant != null;
            
            // 缓存存在性结果
            await SetTenantExistsAsync(tenantId, tenantExists);
            
            return tenantExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查租户存在性失败: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 生成租户缓存键
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>缓存键</returns>
    private static string GetTenantCacheKey(string tenantId)
    {
        return $"{TenantKeyPrefix}{tenantId}";
    }

    /// <summary>
    /// 生成租户存在性缓存键
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>缓存键</returns>
    private static string GetTenantExistsCacheKey(string tenantId)
    {
        return $"{TenantExistsPrefix}{tenantId}";
    }

    /// <summary>
    /// 设置租户存在性标记
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="exists">是否存在</param>
    /// <returns></returns>
    private async Task SetTenantExistsAsync(string tenantId, bool exists)
    {
        try
        {
            var existsKey = GetTenantExistsCacheKey(tenantId);
            var data = Encoding.UTF8.GetBytes(exists.ToString().ToLower());
            
            // 存在性标记使用较短的过期时间
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2)
            };
            
            await _cache.SetAsync(existsKey, data, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置租户存在性标记失败: {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// 添加到活跃租户列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    private async Task AddToActiveTenantsListAsync(string tenantId)
    {
        try
        {
            // 获取现有列表
            var tenantIds = await GetActiveTenantsListAsync();
            
            // 添加新租户ID（如果不存在）
            if (!tenantIds.Contains(tenantId))
            {
                tenantIds.Add(tenantId);
                await SaveActiveTenantsListAsync(tenantIds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "添加到活跃租户列表失败: {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// 从活跃租户列表移除
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    private async Task RemoveFromActiveTenantsListAsync(string tenantId)
    {
        try
        {
            // 获取现有列表
            var tenantIds = await GetActiveTenantsListAsync();
            
            // 移除租户ID
            if (tenantIds.Remove(tenantId))
            {
                await SaveActiveTenantsListAsync(tenantIds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从活跃租户列表移除失败: {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// 获取活跃租户ID列表
    /// </summary>
    /// <returns>租户ID列表</returns>
    private async Task<List<string>> GetActiveTenantsListAsync()
    {
        try
        {
            var cachedData = await _cache.GetAsync(ActiveTenantsKey);
            
            if (cachedData != null)
            {
                var json = Encoding.UTF8.GetString(cachedData);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取活跃租户列表失败");
        }
        
        return new List<string>();
    }

    /// <summary>
    /// 保存活跃租户ID列表
    /// </summary>
    /// <param name="tenantIds">租户ID列表</param>
    /// <returns></returns>
    private async Task SaveActiveTenantsListAsync(List<string> tenantIds)
    {
        try
        {
            var json = JsonConvert.SerializeObject(tenantIds);
            var data = Encoding.UTF8.GetBytes(json);
            
            await _cache.SetAsync(ActiveTenantsKey, data, _defaultCacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存活跃租户列表失败");
        }
    }
}
