using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace CodeSpirit.Authorization
{
    /// <summary>
    /// 基于角色权限的授权处理程序
    /// </summary>
    public class RolePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // 获取当前HTTP上下文和终结点
            var httpContext = context.Resource as HttpContext;
            var endpoint = httpContext?.GetEndpoint();

            // 如果终结点标记为允许匿名访问，则直接授权通过
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (context.User?.Claims != null)
            {
                // 获取用户的所有角色声明
                var roles = context.User.Claims
                    .Where(c => c.Type == "role")
                    .Select(c => c.Value);

                // 如果用户是管理员，则直接授权通过
                if (roles.Contains("Administrator"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                // 获取终结点上的权限特性
                var permissionAttribute = endpoint?.Metadata.GetMetadata<PermissionAttribute>();
                if (permissionAttribute != null)
                {
                    // 获取用户的所有权限声明
                    var userPermissions = context.User.Claims
                        .Where(c => c.Type == "permissions")
                        .Select(c => c.Value);

                    // 检查用户是否拥有所需权限
                    if (userPermissions.Contains(permissionAttribute.Name))
                    {
                        context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}

