using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CodeSpirit.Authorization
{
    public class RolePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // 确保 Resource 是 HttpContext
            if (context.Resource is not HttpContext httpContext)
            {
                return Task.CompletedTask;
            }

            Endpoint endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                return Task.CompletedTask;
            }

            // 允许匿名访问的终结点直接授权
            if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (context.User?.Claims != null)
            {
                // 提前转换为 HashSet 提高查找效率
                HashSet<string> roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
                HashSet<string> userPermissions = context.User.FindAll("permissions").Select(c => c.Value).ToHashSet();

                // 管理员直接通过
                if (roles.Contains("Administrator"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                // 检查权限
                PermissionAttribute permissionAttribute = endpoint.Metadata.GetMetadata<PermissionAttribute>();
                if (permissionAttribute != null && userPermissions.Contains(permissionAttribute.Name))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
