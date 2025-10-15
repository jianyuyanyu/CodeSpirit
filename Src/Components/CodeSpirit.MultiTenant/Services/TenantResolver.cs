using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 租户解析器实现
/// 支持从Header、Query、Subdomain等多种方式解析租户
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheService _cache;
    private readonly ILogger<TenantResolver> _logger;
    private readonly ITenantStore _tenantStore;
    private readonly TenantOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <param name="cache">统一缓存服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="tenantStore">租户存储</param>
    /// <param name="options">租户配置选项</param>
    public TenantResolver(
        IHttpContextAccessor httpContextAccessor,
        ICacheService cache,
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
            _logger.LogWarning("HTTP上下文为空，无法解析租户ID");
            return await HandleTenantResolutionFailureAsync();
        }

        string? tenantId = null;

        // 1. 从Header中解析
        if (_options.ResolveFromHeader)
        {
            tenantId = httpContext.Request.Headers[_options.TenantHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从Header解析到租户ID: {TenantId}", tenantId);
                
                if (await ValidateTenantIdAsync(tenantId))
                {
                    return tenantId;
                }
                else
                {
                    _logger.LogWarning("从Header解析的租户ID无效: {TenantId}", tenantId);
                    tenantId = null;
                }
            }
        }

        // 2. 从Query参数中解析
        if (_options.ResolveFromQuery)
        {
            tenantId = httpContext.Request.Query[_options.TenantQueryName].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("从Query解析到租户ID: {TenantId}", tenantId);
                
                if (await ValidateTenantIdAsync(tenantId))
                {
                    return tenantId;
                }
                else
                {
                    _logger.LogWarning("从Query解析的租户ID无效: {TenantId}", tenantId);
                    tenantId = null;
                }
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
                
                if (await ValidateTenantIdAsync(tenantId))
                {
                    return tenantId;
                }
                else
                {
                    _logger.LogWarning("从子域名解析的租户ID无效: {TenantId}", tenantId);
                    tenantId = null;
                }
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
                    
                    if (await ValidateTenantIdAsync(tenantId))
                    {
                        return tenantId;
                    }
                    else
                    {
                        _logger.LogWarning("从路径解析的租户ID无效: {TenantId}", tenantId);
                        tenantId = null;
                    }
                }
            }
        }

        // 根据配置的失败策略处理无法解析租户ID的情况
        return await HandleTenantResolutionFailureAsync();
    }

    /// <summary>
    /// 验证租户ID是否有效
    /// </summary>
    /// <param name="tenantId">要验证的租户ID</param>
    /// <returns>租户ID是否有效</returns>
    private async Task<bool> ValidateTenantIdAsync(string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            // 如果禁用了租户验证，则认为任何非空租户ID都是有效的
            if (!_options.EnableTenantValidation)
            {
                return true;
            }

            var tenantInfo = await GetTenantInfoAsync(tenantId);
            if (tenantInfo == null)
            {
                _logger.LogWarning("租户不存在: {TenantId}", tenantId);
                return false;
            }

            if (!tenantInfo.IsActive)
            {
                _logger.LogWarning("租户已禁用: {TenantId}", tenantId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证租户ID时发生异常: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 处理租户解析失败的情况
    /// </summary>
    /// <returns>根据策略返回的租户ID或null</returns>
    private async Task<string?> HandleTenantResolutionFailureAsync()
    {
        _logger.LogWarning("无法从任何来源解析到有效的租户ID，使用失败策略: {Strategy}", _options.FailureStrategy);

        switch (_options.FailureStrategy)
        {
            case TenantResolutionFailureStrategy.UseDefault:
                // 只有在明确配置为UseDefault时才使用默认租户
                _logger.LogInformation("使用默认租户ID: {DefaultTenantId}", _options.DefaultTenantId);
                return _options.DefaultTenantId;

            case TenantResolutionFailureStrategy.ThrowException:
                var exception = new InvalidOperationException("无法解析租户ID，且配置为抛出异常策略");
                _logger.LogError(exception, "租户解析失败，抛出异常");
                throw exception;

            case TenantResolutionFailureStrategy.Return404:
                // 返回null，让上层处理为404
                _logger.LogWarning("租户解析失败，返回null以触发404响应");
                return null;

            default:
                // 默认情况下采用最安全的策略：返回null
                _logger.LogWarning("未知的失败策略，返回null以确保安全");
                return null;
        }
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

        // 使用二级缓存获取租户信息
        if (_options.EnableTenantCache)
        {
            var cacheKey = $"tenant_info_{tenantId}";
            
            return await _cache.GetOrSetAsync(
                cacheKey,
                async () => await _tenantStore.GetTenantAsync(tenantId),
                new CacheOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
                    Level = CacheLevel.Both, // 使用两级缓存提升性能
                    EnableBreakthroughProtection = true
                });
        }

        // 不启用缓存时直接从存储获取
        return await _tenantStore.GetTenantAsync(tenantId);
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
            // 修复：无法解析租户ID时记录警告并返回null
            _logger.LogWarning("无法解析当前租户ID，返回null");
            return null;
        }

        return await GetTenantInfoAsync(tenantId);
    }
} 