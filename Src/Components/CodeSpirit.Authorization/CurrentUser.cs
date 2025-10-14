using System.Security.Claims;
using CodeSpirit.Core;
using CodeSpirit.Core.Constants;
using CodeSpirit.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Reflection;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 当前用户实现类，用于获取当前HTTP上下文中的用户信息
    /// </summary>
    public class CurrentUser : ISettableCurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;
        
        // 可设置的用户信息字段（用于事件处理等场景）
        private long? _overrideUserId;
        private string _overrideTenantId;
        private string _overrideUserName;
        
        // 缓存反射结果以提升性能
        private static readonly PropertyInfo _tenantInfoNameProperty = 
            typeof(object).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ITenantInfo")?
            .GetProperty("Name") ?? 
            typeof(object).GetProperty("Name"); // 备用方案

        /// <summary>
        /// 获取当前HTTP上下文中的用户主体
        /// </summary>
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        /// <param name="cache">分布式缓存</param>
        public CurrentUser(IHttpContextAccessor httpContextAccessor, IDistributedCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        /// <summary>
        /// 获取当前用户ID
        /// 从NameIdentifier声明中解析用户ID，如果解析失败返回null
        /// </summary>
        public long? Id
        {
            get
            {
                // 优先使用覆盖值（用于事件处理等场景）
                if (_overrideUserId.HasValue)
                {
                    return _overrideUserId;
                }
                
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) ? userId : null;
            }
        }

        /// <summary>
        /// 获取当前用户名
        /// 从Name声明中获取用户名
        /// </summary>
        public string UserName
        {
            get
            {
                // 优先使用覆盖值（用于事件处理等场景）
                if (!string.IsNullOrEmpty(_overrideUserName))
                {
                    return _overrideUserName;
                }
                
                return User?.FindFirst(ClaimTypes.Name)?.Value;
            }
        }

        /// <summary>
        /// 获取当前用户的所有角色
        /// 从Role声明中获取所有角色信息
        /// </summary>
        public string[] Roles => User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray() ?? Array.Empty<string>();

        /// <summary>
        /// 判断当前用户是否已认证
        /// </summary>
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// 获取当前用户的所有声明
        /// </summary>
        public IEnumerable<Claim> Claims => User?.Claims ?? Enumerable.Empty<Claim>();

        /// <summary>
        /// 获取当前用户的租户ID
        /// 优先从JWT声明中获取，如果没有则从HttpContext中获取
        /// </summary>
        public string? TenantId
        {
            get
            {
                // 优先使用覆盖值（用于事件处理等场景）
                if (!string.IsNullOrEmpty(_overrideTenantId))
                {
                    return _overrideTenantId;
                }
                
                // 先从JWT声明中获取租户ID
                var tenantIdClaim = User?.FindFirst("TenantId");
                if (!string.IsNullOrEmpty(tenantIdClaim?.Value))
                {
                    return tenantIdClaim.Value;
                }

                // 如果JWT声明中没有，从HttpContext Items中获取（多租户中间件设置）
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Items.ContainsKey("TenantId") == true)
                {
                    return httpContext.Items["TenantId"] as string;
                }

                return null;
            }
        }

        /// <summary>
        /// 获取当前用户的租户名称
        /// 优先从JWT声明中获取，如果没有则从HttpContext中获取
        /// </summary>
        public string? TenantName
        {
            get
            {
                // 先从JWT声明中获取租户名称
                var tenantNameClaim = User?.FindFirst("TenantName");
                if (!string.IsNullOrEmpty(tenantNameClaim?.Value))
                {
                    return tenantNameClaim.Value;
                }

                // 如果JWT声明中没有，从HttpContext Items中获取租户信息
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Items.ContainsKey("TenantInfo") == true)
                {
                    var tenantInfo = httpContext.Items["TenantInfo"];
                    if (tenantInfo != null)
                    {
                        // 尝试直接访问Name属性（大多数情况下都有此属性）
                        try
                        {
                            var nameProperty = tenantInfo.GetType().GetProperty("Name");
                            return nameProperty?.GetValue(tenantInfo) as string;
                        }
                        catch
                        {
                            // 如果获取失败，返回null而不是抛出异常
                            return null;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// 判断当前用户是否属于指定角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>如果用户属于该角色返回true，否则返回false</returns>
        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

        /// <summary>
        /// 判断用户是否属于指定租户
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <returns>如果用户属于该租户返回true，否则返回false</returns>
        public bool IsInTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                return false;
            }

            return string.Equals(TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        #region ISettableCurrentUser 实现

        /// <summary>
        /// 设置当前用户ID（用于事件处理等场景）
        /// </summary>
        /// <param name="userId">用户ID</param>
        public void SetUserId(long? userId)
        {
            _overrideUserId = userId;
        }

        /// <summary>
        /// 设置当前租户ID（用于事件处理等场景）
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        public void SetTenantId(string tenantId)
        {
            _overrideTenantId = tenantId;
        }

        /// <summary>
        /// 设置当前用户名（用于事件处理等场景）
        /// </summary>
        /// <param name="userName">用户名</param>
        public void SetUserName(string userName)
        {
            _overrideUserName = userName;
        }

        /// <summary>
        /// 重置为原始状态，清除所有覆盖值
        /// </summary>
        public void Reset()
        {
            _overrideUserId = null;
            _overrideTenantId = null;
            _overrideUserName = null;
        }

        #endregion

        /// <summary>
        /// 权限集合
        /// </summary>
        public HashSet<string> Permissions
        {
            get
            {
                // 如果用户未认证，返回空集合
                if (!IsAuthenticated || Id == null)
                {
                    return new HashSet<string>();
                }

                // 检查租户ID是否存在
                if (string.IsNullOrEmpty(TenantId))
                {
                    return new HashSet<string>();
                }

                //TODO: 后续针对客户端用户可以将少量权限直接存入Claims中，减少缓存依赖
                // 定义缓存键，必须包含租户信息
                string cacheKey = CacheKeys.GetUserPermissionsCacheKey(Id.Value, TenantId);

                // 尝试从缓存中获取权限
                var cachedPermissions = _cache.GetAsync<HashSet<string>>(cacheKey).GetAwaiter().GetResult();
                if (cachedPermissions != null)
                {
                    return cachedPermissions;
                }

                //// 如果缓存中没有，则从claims中读取
                //var claimsPermissions = User?.FindAll("permissions")
                //    .Select(c => c.Value)
                //    .ToHashSet() ?? new HashSet<string>();

                //// 将从claims中读取的权限存入缓存（如果有权限）
                //if (claimsPermissions.Count > 0)
                //{
                //    var cacheOptions = new DistributedCacheEntryOptions
                //    {
                //        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                //        SlidingExpiration = TimeSpan.FromMinutes(30)
                //    };

                //    _cache.SetAsync(cacheKey, claimsPermissions, cacheOptions).GetAwaiter().GetResult();
                //}

                return new HashSet<string>();
            }
        }
    }
}