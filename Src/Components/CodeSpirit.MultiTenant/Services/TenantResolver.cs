using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 租户解析器实现
/// 支持从Header、Query、Subdomain等多种方式解析租户
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TenantResolver> _logger;
    private readonly ITenantStore _tenantStore;
    private readonly TenantOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <param name="cache">分布式缓存</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="tenantStore">租户存储</param>
    /// <param name="options">租户配置选项</param>
    public TenantResolver(
        IHttpContextAccessor httpContextAccessor,
        IDistributedCache cache,
        ILogger<TenantResolver> logger,
        ITenantStore tenantStore,
        IOptions<TenantOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _logger = logger;
        _tenantStore = tenantStore;
        _options = options.Value;
    }

    /// <summary>
    /// 解析当前请求的租户ID
    /// </summary>
    public async Task<string?> ResolveTenantIdAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return _options.DefaultTenantId;
        }

        string? tenantId = null;

        // 1. 从Header中解析
        if (_options.ResolveFromHeader)
        {
            tenantId = httpContext.Request.Headers[_options.TenantHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从Header解析到租户ID: {TenantId}", tenantId);
                return tenantId;
            }
        }

        // 2. 从Query参数中解析
        if (_options.ResolveFromQuery)
        {
            tenantId = httpContext.Request.Query[_options.TenantQueryName].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从Query解析到租户ID: {TenantId}", tenantId);
                return tenantId;
            }
        }

        // 3. 从子域名中解析
        if (_options.ResolveFromSubdomain)
        {
            var host = httpContext.Request.Host.Host;
            var parts = host.Split('.');
            if (parts.Length > 2) // 至少有子域名
            {
                tenantId = parts[0];
                _logger.LogDebug("从子域名解析到租户ID: {TenantId}", tenantId);
                return tenantId;
            }
        }

        // 4. 从路径中解析
        if (_options.ResolveFromPath)
        {
            var path = httpContext.Request.Path.Value;
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0 && segments[0].StartsWith(_options.TenantPathPrefix))
                {
                    tenantId = segments[0].Substring(_options.TenantPathPrefix.Length);
                    _logger.LogDebug("从路径解析到租户ID: {TenantId}", tenantId);
                    return tenantId;
                }
            }
        }

        // 5. 返回默认租户ID
        return _options.DefaultTenantId;
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    public async Task<ITenantInfo?> GetTenantInfoAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        // 先从缓存获取
        if (_options.EnableTenantCache)
        {
            var cacheKey = $"tenant_info_{tenantId}";
            var cachedInfo = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedInfo))
            {
                return JsonConvert.DeserializeObject<TenantInfo>(cachedInfo);
            }
        }

        // 从存储获取
        var tenantInfo = await _tenantStore.GetTenantAsync(tenantId);
        if (tenantInfo != null && _options.EnableTenantCache)
        {
            // 缓存租户信息
            var cacheKey = $"tenant_info_{tenantId}";
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(tenantInfo), 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
                });
        }

        return tenantInfo;
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    public async Task<IEnumerable<ITenantInfo>> GetActiveTenantInfosAsync()
    {
        return await _tenantStore.GetActiveTenantsAsync();
    }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    public async Task<ITenantInfo?> GetCurrentTenantInfoAsync()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        return await GetTenantInfoAsync(tenantId);
    }
} 