using System.Security.Claims;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 当前用户实现类，用于获取当前HTTP上下文中的用户信息
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 获取当前HTTP上下文中的用户主体
        /// </summary>
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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
    }
}