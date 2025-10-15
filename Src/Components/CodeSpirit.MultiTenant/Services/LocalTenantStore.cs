using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Data;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 本地租户存储实现
/// 用于Identity服务内部直接访问数据库，避免HTTP循环调用
/// 使用L1内存缓存提升性能
/// </summary>
public class LocalTenantStore : ITenantStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocalTenantStore> _logger;
    private readonly ICacheService _cache;
    private readonly LocalTenantStoreOptions _options;
    private readonly CacheOptions _defaultCacheOptions;

    // 缓存相关常量
    private const string TENANT_CACHE_KEY_PREFIX = "local_tenant_";
    private const string ACTIVE_TENANTS_CACHE_KEY = "local_active_tenants";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cache">统一缓存服务</param>
    /// <param name="options">本地租户存储配置选项</param>
    public LocalTenantStore(
        IServiceProvider serviceProvider,
        ILogger<LocalTenantStore> logger,
        ICacheService cache,
        IOptions<LocalTenantStoreOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // 配置默认缓存选项 - 使用L1内存缓存
        _defaultCacheOptions = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
            Level = CacheLevel.L1Only, // 仅使用本地内存缓存
            EnableBreakthroughProtection = false // 本地缓存不需要击穿保护
        };
    }

    /// <summary>
    /// 获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户信息</returns>
    public async Task<ITenantInfo> GetTenantAsync(string tenantId)
    {
        _logger.LogDebug("==== LocalTenantStore.GetTenantAsync 被调用，租户ID: {TenantId} ====", tenantId);
        
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            var cacheKey = $"{TENANT_CACHE_KEY_PREFIX}{tenantId}";
            
            // 先尝试从缓存获取
            var cachedTenant = await _cache.GetAsync<TenantInfo>(cacheKey);
            if (cachedTenant != null)
            {
                _logger.LogDebug("从缓存中获取到租户信息: {TenantId}", tenantId);
                return cachedTenant;
            }

            // 从数据库查询
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext == null)
            {
                _logger.LogError("无法获取DbContext实例");
                return null;
            }

            // 禁用多租户筛选器
            var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
            using (dataFilter?.Disable<IMultiTenant>())
            {
                _logger.LogDebug("已禁用多租户筛选器，正在查询租户: {TenantId}", tenantId);
                
                // 查询租户信息
                var tenantEntity = await dbContext.Set<TenantInfo>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId);

                if (tenantEntity == null)
                {
                    _logger.LogDebug("租户不存在: {TenantId}", tenantId);
                    return null;
                }

                _logger.LogDebug("成功找到租户: {TenantId}, Name: {Name}, IsActive: {IsActive}", 
                    tenantEntity.TenantId, tenantEntity.Name, tenantEntity.IsActive);

                // 缓存结果
                await _cache.SetAsync(cacheKey, tenantEntity, _defaultCacheOptions);
                _logger.LogDebug("已缓存租户信息: {TenantId}", tenantId);

                return tenantEntity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户信息时发生异常: {TenantId}", tenantId);
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
            // 先尝试从缓存获取
            var cachedTenants = await _cache.GetAsync<List<TenantInfo>>(ACTIVE_TENANTS_CACHE_KEY);
            if (cachedTenants != null)
            {
                _logger.LogDebug("从缓存中获取到活跃租户列表，数量: {Count}", cachedTenants.Count);
                return cachedTenants.Cast<ITenantInfo>();
            }

            // 从数据库查询
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext == null)
            {
                _logger.LogError("无法获取DbContext实例");
                return Enumerable.Empty<ITenantInfo>();
            }

            // 禁用多租户筛选器
            var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
            using (dataFilter?.Disable<IMultiTenant>())
            {
                _logger.LogDebug("已禁用多租户筛选器，正在查询活跃租户");
                
                var activeTenants = await dbContext.Set<TenantInfo>()
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .Take(_options.MaxActiveTenantsCount)
                    .ToListAsync();

                _logger.LogDebug("查询到 {Count} 个活跃租户", activeTenants.Count);

                // 缓存结果
                await _cache.SetAsync(ACTIVE_TENANTS_CACHE_KEY, activeTenants, _defaultCacheOptions);
                _logger.LogDebug("已缓存活跃租户列表");

                return activeTenants.Cast<ITenantInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃租户列表时发生异常");
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
        try
        {
            if (tenantInfo == null)
            {
                return false;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext == null)
            {
                _logger.LogError("无法获取DbContext实例");
                return false;
            }

            // 禁用多租户筛选器
            var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
            using (dataFilter?.Disable<IMultiTenant>())
            {
                _logger.LogDebug("正在创建租户: {TenantId}", tenantInfo.TenantId);
                
                var tenantEntity = new TenantInfo
                {
                    TenantId = tenantInfo.TenantId,
                    Name = tenantInfo.Name,
                    DisplayName = tenantInfo.DisplayName,
                    Description = tenantInfo.Description,
                    Strategy = tenantInfo.Strategy,
                    IsActive = tenantInfo.IsActive,
                    Domain = tenantInfo.Domain,
                    LogoUrl = tenantInfo.LogoUrl,
                    MaxUsers = (tenantInfo as TenantInfo)?.MaxUsers ?? 1000,
                    StorageLimit = (tenantInfo as TenantInfo)?.StorageLimit ?? 10240,
                    ExpiresAt = (tenantInfo as TenantInfo)?.ExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    ThemeConfig = tenantInfo.ThemeConfig ?? "{}",
                    Configuration = tenantInfo.Configuration ?? "{}"
                };

                dbContext.Set<TenantInfo>().Add(tenantEntity);
                await dbContext.SaveChangesAsync();

                // 清除相关缓存
                await ClearTenantCacheAsync(tenantInfo.TenantId);
                
                _logger.LogInformation("成功创建租户: {TenantId}", tenantInfo.TenantId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建租户时发生异常: {TenantId}", tenantInfo?.TenantId);
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
        try
        {
            if (tenantInfo == null)
            {
                return false;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext == null)
            {
                _logger.LogError("无法获取DbContext实例");
                return false;
            }

            // 禁用多租户筛选器
            var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
            using (dataFilter?.Disable<IMultiTenant>())
            {
                _logger.LogDebug("正在更新租户: {TenantId}", tenantInfo.TenantId);
                
                var existingTenant = await dbContext.Set<TenantInfo>()
                    .FirstOrDefaultAsync(t => t.TenantId == tenantInfo.TenantId);

                if (existingTenant == null)
                {
                    _logger.LogWarning("要更新的租户不存在: {TenantId}", tenantInfo.TenantId);
                    return false;
                }

                // 更新属性
                existingTenant.Name = tenantInfo.Name;
                existingTenant.DisplayName = tenantInfo.DisplayName;
                existingTenant.Description = tenantInfo.Description;
                existingTenant.Strategy = tenantInfo.Strategy;
                existingTenant.IsActive = tenantInfo.IsActive;
                existingTenant.Domain = tenantInfo.Domain;
                existingTenant.LogoUrl = tenantInfo.LogoUrl;
                existingTenant.MaxUsers = (tenantInfo as TenantInfo)?.MaxUsers ?? existingTenant.MaxUsers;
                existingTenant.StorageLimit = (tenantInfo as TenantInfo)?.StorageLimit ?? existingTenant.StorageLimit;
                existingTenant.ExpiresAt = (tenantInfo as TenantInfo)?.ExpiresAt ?? existingTenant.ExpiresAt;
                existingTenant.ThemeConfig = tenantInfo.ThemeConfig ?? "{}";
                existingTenant.Configuration = tenantInfo.Configuration ?? "{}";

                await dbContext.SaveChangesAsync();

                // 清除相关缓存
                await ClearTenantCacheAsync(tenantInfo.TenantId);
                
                _logger.LogInformation("成功更新租户: {TenantId}", tenantInfo.TenantId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新租户时发生异常: {TenantId}", tenantInfo?.TenantId);
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
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext == null)
            {
                _logger.LogError("无法获取DbContext实例");
                return false;
            }

            // 禁用多租户筛选器
            var dataFilter = scope.ServiceProvider.GetService<IDataFilter>();
            using (dataFilter?.Disable<IMultiTenant>())
            {
                _logger.LogDebug("正在删除租户: {TenantId}", tenantId);
                
                var existingTenant = await dbContext.Set<TenantInfo>()
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId);

                if (existingTenant == null)
                {
                    _logger.LogWarning("要删除的租户不存在: {TenantId}", tenantId);
                    return false;
                }

                dbContext.Set<TenantInfo>().Remove(existingTenant);
                await dbContext.SaveChangesAsync();

                // 清除相关缓存
                await ClearTenantCacheAsync(tenantId);
                
                _logger.LogInformation("成功删除租户: {TenantId}", tenantId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除租户时发生异常: {TenantId}", tenantId);
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
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            var tenant = await GetTenantAsync(tenantId);
            return tenant != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查租户是否存在时发生异常: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 清除指定租户的缓存
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    private async Task ClearTenantCacheAsync(string tenantId)
    {
        var tenantCacheKey = $"{TENANT_CACHE_KEY_PREFIX}{tenantId}";
        
        await _cache.RemoveAsync(tenantCacheKey);
        await _cache.RemoveAsync(ACTIVE_TENANTS_CACHE_KEY);
        
        _logger.LogDebug("已清除租户缓存: {TenantId}", tenantId);
    }
}

/// <summary>
/// 本地租户存储配置选项
/// </summary>
public class LocalTenantStoreOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "MultiTenant:LocalStore";

    /// <summary>
    /// 最大活跃租户数量限制
    /// </summary>
    public int MaxActiveTenantsCount { get; set; } = 1000;

    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
}
