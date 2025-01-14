using System.Linq;
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.IdentityApi.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class PermissionAttribute : Attribute
    {
        public string Permission { get; }

        public PermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }

    public interface IPermissionService
    {
        /// <summary>
        /// 检查当前用户是否具有指定的权限。
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>如果具有权限，返回 true；否则返回 false。</returns>
        bool HasPermission(string permission);
    }



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
