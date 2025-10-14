using CodeSpirit.Aggregator.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.DistributedLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.Internal
{
    /// <summary>
    /// 内部用户信息访问控制器
    /// </summary>
    [DisableAggregator]
    [DisplayName("内部用户信息")]
    [Module("default")]
    [Route("api/identity/internal/users")]
    [NoAudit("内部用户信息访问控制器不需要审计")]
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<InternalUsersController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ITenantContext _tenantContext;

        // 缓存相关常量
        private const string USER_CACHE_KEY_PREFIX = "internal_user_";
        private const string USER_USERNAME_CACHE_KEY_PREFIX = "internal_user_username_";
        private const string USER_BATCH_CACHE_KEY_PREFIX = "internal_users_batch_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15); // 用户信息缓存时间相对较短

        // 分布式锁相关常量
        private const string USER_LOCK_KEY_PREFIX = "lock_user_";
        private const string USER_USERNAME_LOCK_KEY_PREFIX = "lock_user_username_";
        private const string USER_BATCH_LOCK_KEY_PREFIX = "lock_users_batch_";
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userService">用户服务</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryCache">内存缓存</param>
        /// <param name="distributedLockProvider">分布式锁提供程序</param>
        /// <param name="tenantContext">租户上下文</param>
        public InternalUsersController(
            IUserService userService, 
            ILogger<InternalUsersController> logger, 
            IMemoryCache memoryCache, 
            IDistributedLockProvider distributedLockProvider,
            ITenantContext tenantContext)
        {
            _userService = userService;
            _logger = logger;
            _memoryCache = memoryCache;
            _distributedLockProvider = distributedLockProvider;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// 生成包含租户ID的缓存键
        /// </summary>
        /// <param name="baseKey">基础缓存键</param>
        /// <returns>包含租户ID的缓存键</returns>
        private string GetTenantCacheKey(string baseKey)
        {
            var tenantId = _tenantContext.TenantId ?? "default";
            return $"{baseKey}:tenant:{tenantId}";
        }

        /// <summary>
        /// 生成包含租户ID的分布式锁键
        /// </summary>
        /// <param name="baseLockKey">基础锁键</param>
        /// <returns>包含租户ID的锁键</returns>
        private string GetTenantLockKey(string baseLockKey)
        {
            var tenantId = _tenantContext.TenantId ?? "default";
            return $"{baseLockKey}:tenant:{tenantId}";
        }

        /// <summary>
        /// 清除指定用户的缓存（租户感知）
        /// </summary>
        /// <param name="userId">用户ID</param>
        private void ClearUserCache(long userId)
        {
            var baseUserCacheKey = $"{USER_CACHE_KEY_PREFIX}{userId}";
            var userCacheKey = GetTenantCacheKey(baseUserCacheKey);
            _memoryCache.Remove(userCacheKey);
            
            var tenantId = _tenantContext.TenantId ?? "default";
            _logger.LogDebug("已清除用户缓存: {UserId}, 租户ID: {TenantId}", userId, tenantId);
        }

        /// <summary>
        /// 清除指定用户名的缓存（租户感知）
        /// </summary>
        /// <param name="username">用户名</param>
        private void ClearUserUsernameCache(string username)
        {
            var normalizedUsername = username.ToLowerInvariant();
            var baseUsernameCacheKey = $"{USER_USERNAME_CACHE_KEY_PREFIX}{normalizedUsername}";
            var usernameCacheKey = GetTenantCacheKey(baseUsernameCacheKey);
            _memoryCache.Remove(usernameCacheKey);
            
            var tenantId = _tenantContext.TenantId ?? "default";
            _logger.LogDebug("已清除用户名缓存: {Username}, 租户ID: {TenantId}", username, tenantId);
        }


        /// <summary>
        /// 获取多个用户信息
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>用户列表</returns>
        [HttpGet]
        [DisplayName("获取内部用户列表")]
        public async Task<ActionResult<PageList<UserDto>>> GetInternalUsers([FromQuery] UserQueryDto queryDto)
        {
            PageList<UserDto> users = await _userService.GetUsersAsync(queryDto);
            return Ok(users);
        }

        /// <summary>
        /// 获取单个用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取内部用户详情")]
        public async Task<ActionResult<UserDto>> GetInternalUser(long id)
        {
            _logger.LogDebug("开始获取内部用户信息: {UserId}", id);
            
            try
            {
                var cacheKey = $"{USER_CACHE_KEY_PREFIX}{id}";
                
                // 先尝试从缓存获取
                if (_memoryCache.TryGetValue(cacheKey, out UserDto cachedUser))
                {
                    _logger.LogDebug("从缓存中获取到用户信息: {UserId}", id);
                    return Ok(cachedUser);
                }

                _logger.LogDebug("缓存中未找到用户信息，准备从数据库查询: {UserId}", id);

                // 使用分布式锁防止缓存击穿
                var lockKey = $"{USER_LOCK_KEY_PREFIX}{id}";
                using var distributedLock = await _distributedLockProvider.AcquireLockAsync(lockKey, LockTimeout, LockTtl);
                
                _logger.LogDebug("已获取分布式锁: {LockKey}", lockKey);

                // 再次检查缓存，可能在等待锁的过程中其他线程已经缓存了数据
                if (_memoryCache.TryGetValue(cacheKey, out cachedUser))
                {
                    _logger.LogDebug("在获取锁后从缓存中获取到用户信息: {UserId}", id);
                    return Ok(cachedUser);
                }

                _logger.LogDebug("正在从数据库查询用户: {UserId}", id);
                UserDto userDto = await _userService.GetAsync(id);
                
                if (userDto != null)
                {
                    // 将结果缓存
                    _memoryCache.Set(cacheKey, userDto, CacheExpiration);
                    _logger.LogDebug("已缓存用户信息: {UserId}, 过期时间: {Expiration}", id, CacheExpiration);
                }
                else
                {
                    // 缓存空结果，避免频繁查询不存在的用户
                    _memoryCache.Set<UserDto>(cacheKey, null, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("已缓存用户不存在的结果: {UserId}", id);
                }

                _logger.LogDebug("成功返回用户信息: {UserId}", id);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内部用户信息时发生异常: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <returns>用户详情</returns>
        [HttpGet("by-username/{username}")]
        [DisplayName("根据用户名获取用户信息")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            var tenantId = _tenantContext.TenantId ?? "default";
            _logger.LogDebug("开始根据用户名获取用户信息: {Username}, 租户ID: {TenantId}", username, tenantId);
            
            try
            {
                var normalizedUsername = username.ToLowerInvariant();
                var baseCacheKey = $"{USER_USERNAME_CACHE_KEY_PREFIX}{normalizedUsername}";
                var cacheKey = GetTenantCacheKey(baseCacheKey);
                
                // 先尝试从缓存获取
                if (_memoryCache.TryGetValue(cacheKey, out UserDto cachedUser))
                {
                    _logger.LogDebug("从缓存中获取到用户名对应的用户信息: {Username}, 租户ID: {TenantId}", username, tenantId);
                    if (cachedUser == null)
                    {
                        return NotFound(new ApiResponse(404, "用户不存在"));
                    }
                    return Ok(cachedUser);
                }

                _logger.LogDebug("缓存中未找到用户名对应的用户信息，准备从数据库查询: {Username}, 租户ID: {TenantId}", username, tenantId);

                // 使用分布式锁防止缓存击穿
                var baseLockKey = $"{USER_USERNAME_LOCK_KEY_PREFIX}{normalizedUsername}";
                var lockKey = GetTenantLockKey(baseLockKey);
                using var distributedLock = await _distributedLockProvider.AcquireLockAsync(lockKey, LockTimeout, LockTtl);
                
                _logger.LogDebug("已获取分布式锁: {LockKey}", lockKey);

                // 再次检查缓存，可能在等待锁的过程中其他线程已经缓存了数据
                if (_memoryCache.TryGetValue(cacheKey, out cachedUser))
                {
                    _logger.LogDebug("在获取锁后从缓存中获取到用户名对应的用户信息: {Username}, 租户ID: {TenantId}", username, tenantId);
                    if (cachedUser == null)
                    {
                        return NotFound(new ApiResponse(404, "用户不存在"));
                    }
                    return Ok(cachedUser);
                }

                _logger.LogDebug("正在从数据库查询用户名: {Username}, 租户ID: {TenantId}", username, tenantId);
                
                // 查找指定用户名的用户
                UserQueryDto queryDto = new UserQueryDto
                {
                    Keywords = username,
                    Page = 1,
                    PerPage = 1
                };

                var result = await _userService.GetUsersAsync(queryDto);
                var userDto = result.Items.FirstOrDefault();

                if (userDto != null)
                {
                    // 将结果缓存（同时缓存到用户ID和用户名两个维度，都包含租户信息）
                    _memoryCache.Set(cacheKey, userDto, CacheExpiration);
                    
                    var baseUserIdCacheKey = $"{USER_CACHE_KEY_PREFIX}{userDto.Id}";
                    var userIdCacheKey = GetTenantCacheKey(baseUserIdCacheKey);
                    _memoryCache.Set(userIdCacheKey, userDto, CacheExpiration);
                    
                    _logger.LogDebug("已缓存用户名对应的用户信息: {Username}, UserId: {UserId}, 租户ID: {TenantId}, 过期时间: {Expiration}", 
                        username, userDto.Id, tenantId, CacheExpiration);
                    
                    return Ok(userDto);
                }
                else
                {
                    // 缓存空结果，避免频繁查询不存在的用户名
                    _memoryCache.Set<UserDto>(cacheKey, null, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("已缓存用户名不存在的结果: {Username}, 租户ID: {TenantId}", username, tenantId);
                    
                    return NotFound(new ApiResponse(404, "用户不存在"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户信息时发生异常: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// 批量获取用户信息
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>用户列表</returns>
        [HttpPost("batch")]
        [DisplayName("批量获取用户信息")]
        public async Task<ActionResult<List<UserDto>>> GetUsersByIds([FromBody] List<long> ids)
        {
            _logger.LogDebug("开始批量获取用户信息，用户数量: {Count}", ids.Count);
            
            try
            {
                List<UserDto> users = new List<UserDto>();
                List<long> missedIds = new List<long>();

                // 首先尝试从缓存获取所有用户
                foreach (var id in ids)
                {
                    var cacheKey = $"{USER_CACHE_KEY_PREFIX}{id}";
                    if (_memoryCache.TryGetValue(cacheKey, out UserDto cachedUser))
                    {
                        if (cachedUser != null)
                        {
                            users.Add(cachedUser);
                            _logger.LogDebug("从缓存中获取到用户信息: {UserId}", id);
                        }
                        else
                        {
                            _logger.LogDebug("缓存中用户不存在: {UserId}", id);
                        }
                    }
                    else
                    {
                        missedIds.Add(id);
                    }
                }

                _logger.LogDebug("缓存命中 {CachedCount} 个用户，需要从数据库查询 {MissedCount} 个用户", 
                    users.Count, missedIds.Count);

                // 对于缓存中没有的用户，使用分布式锁逐个查询
                foreach (var id in missedIds)
                {
                    try
                    {
                        var cacheKey = $"{USER_CACHE_KEY_PREFIX}{id}";
                        var lockKey = $"{USER_LOCK_KEY_PREFIX}{id}";
                        
                        using var distributedLock = await _distributedLockProvider.AcquireLockAsync(lockKey, LockTimeout, LockTtl);
                        _logger.LogDebug("已获取用户分布式锁: {LockKey}", lockKey);

                        // 再次检查缓存，可能在等待锁的过程中其他线程已经缓存了数据
                        if (_memoryCache.TryGetValue(cacheKey, out UserDto cachedUser))
                        {
                            if (cachedUser != null)
                            {
                                users.Add(cachedUser);
                                _logger.LogDebug("在获取锁后从缓存中获取到用户信息: {UserId}", id);
                            }
                            continue;
                        }

                        _logger.LogDebug("正在从数据库查询用户: {UserId}", id);
                        var user = await _userService.GetAsync(id);
                        
                        if (user != null)
                        {
                            users.Add(user);
                            // 缓存用户信息
                            _memoryCache.Set(cacheKey, user, CacheExpiration);
                            _logger.LogDebug("已缓存用户信息: {UserId}", id);
                        }
                        else
                        {
                            // 缓存空结果
                            _memoryCache.Set<UserDto>(cacheKey, null, TimeSpan.FromMinutes(5));
                            _logger.LogDebug("已缓存用户不存在的结果: {UserId}", id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取用户信息失败，忽略该用户: {UserId}", id);
                        // 忽略获取失败的用户
                        continue;
                    }
                }

                _logger.LogDebug("成功返回 {Count} 个用户信息", users.Count);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取用户信息时发生异常");
                throw;
            }
        }
    }
}