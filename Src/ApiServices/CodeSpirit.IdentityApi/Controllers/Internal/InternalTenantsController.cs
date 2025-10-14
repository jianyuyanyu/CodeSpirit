using CodeSpirit.Aggregator.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.IdentityApi.Dtos.Tenant;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DistributedLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Internal
{
    /// <summary>
    /// 内部租户信息访问控制器
    /// 提供租户存储所需的内部API接口
    /// </summary>
    [DisableAggregator]
    [DisplayName("内部租户信息")]
    [Module("default")]
    [Route("api/identity/internal/tenants")]
    [NoAudit("内部租户信息访问控制器不需要审计")]
    public class InternalTenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        private readonly IDataFilter _dataFilter;
        private readonly ILogger<InternalTenantsController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedLockProvider _distributedLockProvider;

        // 缓存相关常量
        private const string TENANT_CACHE_KEY_PREFIX = "internal_tenant_";
        private const string TENANT_EXISTS_CACHE_KEY_PREFIX = "tenant_exists_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        // 分布式锁相关常量
        private const string TENANT_LOCK_KEY_PREFIX = "lock_tenant_";
        private const string TENANT_EXISTS_LOCK_KEY_PREFIX = "lock_tenant_exists_";
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 清除指定租户的缓存
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        private void ClearTenantCache(string tenantId)
        {
            var tenantCacheKey = $"{TENANT_CACHE_KEY_PREFIX}{tenantId}";
            var existsCacheKey = $"{TENANT_EXISTS_CACHE_KEY_PREFIX}{tenantId}";
            
            _memoryCache.Remove(tenantCacheKey);
            _memoryCache.Remove(existsCacheKey);
            
            _logger.LogDebug("已清除租户缓存: {TenantId}", tenantId);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tenantService">租户服务</param>
        /// <param name="dataFilter">数据筛选器</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryCache">内存缓存</param>
        /// <param name="distributedLockProvider">分布式锁提供程序</param>
        public InternalTenantsController(ITenantService tenantService, IDataFilter dataFilter, ILogger<InternalTenantsController> logger, IMemoryCache memoryCache, IDistributedLockProvider distributedLockProvider)
        {
            _tenantService = tenantService;
            _dataFilter = dataFilter;
            _logger = logger;
            _memoryCache = memoryCache;
            _distributedLockProvider = distributedLockProvider;
        }

        /// <summary>
        /// 根据租户ID获取租户信息
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>租户信息</returns>
        [HttpGet("{tenantId}")]
        [DisplayName("获取内部租户信息")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> GetInternalTenant(string tenantId)
        {
            _logger.LogInformation("开始获取内部租户信息: {TenantId}", tenantId);
            
            try
            {
                var cacheKey = $"{TENANT_CACHE_KEY_PREFIX}{tenantId}";
                
                // 先尝试从缓存获取
                if (_memoryCache.TryGetValue(cacheKey, out InternalTenantDto cachedDto))
                {
                    _logger.LogInformation("从缓存中获取到租户信息: {TenantId}", tenantId);
                    return Ok(ApiResponse<InternalTenantDto>.Success(cachedDto));
                }

                _logger.LogInformation("缓存中未找到租户信息，准备从数据库查询: {TenantId}", tenantId);

                // 使用分布式锁防止缓存击穿
                var lockKey = $"{TENANT_LOCK_KEY_PREFIX}{tenantId}";
                using var distributedLock = await _distributedLockProvider.AcquireLockAsync(lockKey, LockTimeout, LockTtl);
                
                _logger.LogInformation("已获取分布式锁: {LockKey}", lockKey);

                // 再次检查缓存，可能在等待锁的过程中其他线程已经缓存了数据
                if (_memoryCache.TryGetValue(cacheKey, out cachedDto))
                {
                    _logger.LogInformation("在获取锁后从缓存中获取到租户信息: {TenantId}", tenantId);
                    return Ok(ApiResponse<InternalTenantDto>.Success(cachedDto));
                }

                // 禁用多租户筛选器来查询指定租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogInformation("已禁用多租户筛选器，正在查询租户: {TenantId}", tenantId);
                    
                    var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                    if (tenant == null)
                    {
                        _logger.LogWarning("租户不存在: {TenantId}", tenantId);
                        
                        // 缓存不存在的结果，避免频繁查询不存在的租户
                        var notFoundResult = ApiResponse<InternalTenantDto>.Error(404, "租户不存在");
                        _memoryCache.Set<InternalTenantDto>(cacheKey, null, TimeSpan.FromMinutes(5)); // 不存在的租户缓存时间较短
                        _logger.LogInformation("已缓存租户不存在的结果: {TenantId}", tenantId);
                        
                        return NotFound(notFoundResult);
                    }

                    _logger.LogInformation("成功找到租户: {TenantId}, Name: {Name}, IsActive: {IsActive}", 
                        tenant.TenantId, tenant.Name, tenant.IsActive);

                    var dto = new InternalTenantDto
                    {
                        TenantId = tenant.TenantId,
                        Name = tenant.Name,
                        DisplayName = tenant.DisplayName,
                        Description = tenant.Description,
                        Strategy = tenant.Strategy,
                        IsActive = tenant.IsActive,
                        Domain = tenant.Domain,
                        LogoUrl = tenant.LogoUrl,
                        MaxUsers = tenant.MaxUsers,
                        StorageLimit = tenant.StorageLimit,
                        ExpiresAt = tenant.ExpiresAt,
                        CreatedAt = tenant.CreatedAt,
                        ThemeConfig = tenant.ThemeConfig,
                        Configuration = tenant.Configuration
                    };

                    // 将结果缓存
                    _memoryCache.Set(cacheKey, dto, CacheExpiration);
                    _logger.LogInformation("已缓存租户信息: {TenantId}, 过期时间: {Expiration}", tenantId, CacheExpiration);

                    _logger.LogInformation("成功返回租户信息: {TenantId}", tenantId);
                    return Ok(ApiResponse<InternalTenantDto>.Success(dto));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内部租户信息时发生异常: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// 检查租户是否存在
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>是否存在</returns>
        [HttpHead("{tenantId}")]
        [DisplayName("检查租户是否存在")]
        public async Task<ActionResult> CheckTenantExists(string tenantId)
        {
            _logger.LogDebug("开始检查租户是否存在: {TenantId}", tenantId);
            
            try
            {
                var cacheKey = $"{TENANT_EXISTS_CACHE_KEY_PREFIX}{tenantId}";
                
                // 先尝试从缓存获取存在性信息
                if (_memoryCache.TryGetValue(cacheKey, out bool cachedExists))
                {
                    _logger.LogDebug("从缓存中获取到租户存在性信息: {TenantId}, Exists: {Exists}", tenantId, cachedExists);
                    return cachedExists ? Ok() : NotFound();
                }

                _logger.LogDebug("缓存中未找到租户存在性信息，准备从数据库查询: {TenantId}", tenantId);

                // 使用分布式锁防止缓存击穿
                var lockKey = $"{TENANT_EXISTS_LOCK_KEY_PREFIX}{tenantId}";
                using var distributedLock = await _distributedLockProvider.AcquireLockAsync(lockKey, LockTimeout, LockTtl);
                
                _logger.LogDebug("已获取分布式锁: {LockKey}", lockKey);

                // 再次检查缓存，可能在等待锁的过程中其他线程已经缓存了数据
                if (_memoryCache.TryGetValue(cacheKey, out cachedExists))
                {
                    _logger.LogDebug("在获取锁后从缓存中获取到租户存在性信息: {TenantId}, Exists: {Exists}", tenantId, cachedExists);
                    return cachedExists ? Ok() : NotFound();
                }

                // 禁用多租户筛选器来检查租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogDebug("已禁用多租户筛选器，正在检查租户: {TenantId}", tenantId);
                    
                    var tenant = await _tenantService.GetByTenantIdAsync(tenantId);
                    var exists = tenant != null;
                    
                    // 缓存存在性结果
                    var cacheExpiration = exists ? CacheExpiration : TimeSpan.FromMinutes(5); // 不存在的租户缓存时间较短
                    _memoryCache.Set(cacheKey, exists, cacheExpiration);
                    _logger.LogDebug("已缓存租户存在性信息: {TenantId}, Exists: {Exists}, 过期时间: {Expiration}", 
                        tenantId, exists, cacheExpiration);
                    
                    if (!exists)
                    {
                        _logger.LogDebug("租户不存在: {TenantId}", tenantId);
                        return NotFound();
                    }

                    _logger.LogDebug("租户存在: {TenantId}, IsActive: {IsActive}", tenantId, tenant.IsActive);
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查租户存在性时发生异常: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// 获取活跃租户列表
        /// </summary>
        /// <returns>活跃租户列表</returns>
        [HttpGet("active")]
        [DisplayName("获取活跃租户列表")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternalTenantDto>>>> GetActiveTenants()
        {
            _logger.LogDebug("开始获取活跃租户列表");
            
            try
            {
                // 禁用多租户筛选器来获取所有活跃租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogDebug("已禁用多租户筛选器，正在查询活跃租户");
                    
                    // 使用分页查询获取活跃租户
                    var queryDto = new TenantQueryDto { IsActive = true, Page = 1, PerPage = 1000 };
                    var result = await _tenantService.GetPagedListAsync(queryDto);

                    _logger.LogDebug("查询到 {Count} 个活跃租户", result.Items.Count());

                    var dtos = new List<InternalTenantDto>();
                    foreach (var t in result.Items)
                    {
                        _logger.LogDebug("处理租户: {TenantId}", t.TenantId);
                        
                        // 获取完整的租户实体信息
                        var tenantEntity = await _tenantService.GetByTenantIdAsync(t.TenantId);
                        if (tenantEntity != null)
                        {
                            dtos.Add(new InternalTenantDto
                            {
                                TenantId = t.TenantId,
                                Name = t.Name,
                                DisplayName = t.DisplayName,
                                Description = t.Description,
                                Strategy = t.Strategy,
                                IsActive = t.IsActive,
                                Domain = t.Domain,
                                LogoUrl = t.LogoUrl,
                                MaxUsers = t.MaxUsers,
                                StorageLimit = t.StorageLimit,
                                ExpiresAt = t.ExpiresAt,
                                CreatedAt = t.CreatedAt,
                                ThemeConfig = tenantEntity.ThemeConfig,
                                Configuration = tenantEntity.Configuration
                            });
                        }
                        else
                        {
                            _logger.LogWarning("无法获取租户实体信息: {TenantId}", t.TenantId);
                        }
                    }

                    _logger.LogDebug("成功返回 {Count} 个活跃租户", dtos.Count);
                    return Ok(ApiResponse<IEnumerable<InternalTenantDto>>.Success(dtos));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃租户列表时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 创建租户
        /// </summary>
        /// <param name="createDto">创建数据</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        [DisplayName("创建内部租户")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> CreateInternalTenant([FromBody] TenantCreateDto createDto)
        {
            _logger.LogDebug("开始创建内部租户: {TenantId}", createDto.TenantId);
            
            try
            {
                // 检查是否尝试创建系统租户
                if (string.Equals(createDto.TenantId, "system", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("尝试创建系统保留租户ID: {TenantId}", createDto.TenantId);
                    return BadRequest(ApiResponse<InternalTenantDto>.Error(400, "不能创建租户ID为'system'的租户，该ID为系统保留"));
                }

                // 禁用多租户筛选器来创建租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogDebug("已禁用多租户筛选器，正在创建租户: {TenantId}", createDto.TenantId);
                    
                    var result = await _tenantService.CreateAsync(createDto);
                    
                    _logger.LogDebug("租户创建成功: {TenantId}, 正在获取完整信息", result.TenantId);
                    
                    // 获取完整的租户实体信息
                    var tenantEntity = await _tenantService.GetByTenantIdAsync(result.TenantId);
                    
                    var dto = new InternalTenantDto
                    {
                        TenantId = result.TenantId,
                        Name = result.Name,
                        DisplayName = result.DisplayName,
                        Description = result.Description,
                        Strategy = result.Strategy,
                        IsActive = result.IsActive,
                        Domain = result.Domain,
                        LogoUrl = result.LogoUrl,
                        MaxUsers = result.MaxUsers,
                        StorageLimit = result.StorageLimit,
                        ExpiresAt = result.ExpiresAt,
                        CreatedAt = result.CreatedAt,
                        ThemeConfig = tenantEntity?.ThemeConfig,
                        Configuration = tenantEntity?.Configuration
                    };

                    // 清除可能存在的缓存（特别是"不存在"的缓存）
                    ClearTenantCache(result.TenantId);

                    _logger.LogDebug("成功创建并返回租户信息: {TenantId}", result.TenantId);
                    return Ok(ApiResponse<InternalTenantDto>.Success(dto, "创建成功"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建内部租户时发生异常: {TenantId}", createDto.TenantId);
                throw;
            }
        }

        /// <summary>
        /// 更新租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="updateDto">更新数据</param>
        /// <returns>更新结果</returns>
        [HttpPut("{tenantId}")]
        [DisplayName("更新内部租户")]
        public async Task<ActionResult<ApiResponse<InternalTenantDto>>> UpdateInternalTenant(string tenantId, [FromBody] TenantUpdateDto updateDto)
        {
            _logger.LogDebug("开始更新内部租户: {TenantId}", tenantId);
            
            try
            {
                // 检查是否为系统租户且尝试禁用
                if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase) && !updateDto.IsActive)
                {
                    _logger.LogWarning("尝试禁用系统租户: {TenantId}", tenantId);
                    return BadRequest(ApiResponse<InternalTenantDto>.Error(400, "系统租户不能被禁用"));
                }

                // 禁用多租户筛选器来更新租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogDebug("已禁用多租户筛选器，正在更新租户: {TenantId}", tenantId);
                    
                    await _tenantService.UpdateAsync(tenantId, updateDto);
                    
                    _logger.LogDebug("租户更新成功: {TenantId}, 正在获取更新后的信息", tenantId);
                    
                    // 重新获取更新后的租户信息
                    var updatedTenant = await _tenantService.GetByTenantIdAsync(tenantId);
                    
                    var dto = new InternalTenantDto
                    {
                        TenantId = updatedTenant.TenantId,
                        Name = updatedTenant.Name,
                        DisplayName = updatedTenant.DisplayName,
                        Description = updatedTenant.Description,
                        Strategy = updatedTenant.Strategy,
                        IsActive = updatedTenant.IsActive,
                        Domain = updatedTenant.Domain,
                        LogoUrl = updatedTenant.LogoUrl,
                        MaxUsers = updatedTenant.MaxUsers,
                        StorageLimit = updatedTenant.StorageLimit,
                        ExpiresAt = updatedTenant.ExpiresAt,
                        CreatedAt = updatedTenant.CreatedAt,
                        ThemeConfig = updatedTenant.ThemeConfig,
                        Configuration = updatedTenant.Configuration
                    };

                    // 清除缓存，确保下次获取最新数据
                    ClearTenantCache(tenantId);

                    _logger.LogDebug("成功更新并返回租户信息: {TenantId}", tenantId);
                    return Ok(ApiResponse<InternalTenantDto>.Success(dto, "更新成功"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新内部租户时发生异常: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// 删除租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{tenantId}")]
        [DisplayName("删除内部租户")]
        public async Task<ActionResult<ApiResponse>> DeleteInternalTenant(string tenantId)
        {
            _logger.LogDebug("开始删除内部租户: {TenantId}", tenantId);
            
            try
            {
                // 检查是否为系统租户
                if (string.Equals(tenantId, "system", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("尝试删除系统租户: {TenantId}", tenantId);
                    return BadRequest(ApiResponse.Error(400, "系统租户不能被删除"));
                }

                // 禁用多租户筛选器来删除租户
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    _logger.LogDebug("已禁用多租户筛选器，正在删除租户: {TenantId}", tenantId);
                    
                    await _tenantService.DeleteAsync(tenantId);
                    
                    // 清除缓存
                    ClearTenantCache(tenantId);
                    
                    _logger.LogDebug("成功删除租户: {TenantId}", tenantId);
                    return Ok(ApiResponse.Success("删除成功"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除内部租户时发生异常: {TenantId}", tenantId);
                throw;
            }
        }
    }
}
