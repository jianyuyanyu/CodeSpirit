using System.Security.Claims;
using CodeSpirit.Core;
using CodeSpirit.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 当前用户实现类，用于获取当前HTTP上下文中的用户信息
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;

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
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) ? userId : null;
            }
        }

        /// <summary>
        /// 获取当前用户名
        /// 从Name声明中获取用户名
        /// </summary>
        public string UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

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
        /// 判断当前用户是否属于指定角色
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <returns>如果用户属于该角色返回true，否则返回false</returns>
        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

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

                // 定义缓存键
                string cacheKey = $"UserPermissions:{Id.Value}";

                // 尝试从缓存中获取权限
                var cachedPermissions = _cache.GetAsync<HashSet<string>>(cacheKey).GetAwaiter().GetResult();
                if (cachedPermissions != null)
                {
                    return cachedPermissions;
                }

                // 如果缓存中没有，则从claims中读取
                var claimsPermissions = User?.FindAll("permissions")
                    .Select(c => c.Value)
                    .ToHashSet() ?? new HashSet<string>();

                // 将从claims中读取的权限存入缓存（如果有权限）
                if (claimsPermissions.Count > 0)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    };

                    _cache.SetAsync(cacheKey, claimsPermissions, cacheOptions).GetAwaiter().GetResult();
                }

                return claimsPermissions;
            }
        }
    }
}