using CodeSpirit.Core.Authorization;

namespace CodeSpirit.IdentityApi.Authorization
{
    public class PermissionService : IPermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool HasPermission(string permission)
        {
            return true;
            //var user = _httpContextAccessor.HttpContext?.User;
            //if (user == null || !user.Identity.IsAuthenticated)
            //    return false;

            //// 假设权限信息存储在用户的声明中，声明类型为 "Permission"
            //return user.Claims.Any(c => c.Type == "Permission" && c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }
    }
}
