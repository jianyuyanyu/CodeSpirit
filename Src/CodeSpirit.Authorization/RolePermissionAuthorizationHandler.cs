using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CodeSpirit.Authorization
{
    public class RolePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<RolePermissionAuthorizationHandler> logger;

        public RolePermissionAuthorizationHandler(ILogger<RolePermissionAuthorizationHandler> logger)
        {
            this.logger = logger;
        }
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
                if (roles.Contains("Admin"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                // 检查权限
                PermissionAttribute permissionAttribute = endpoint.Metadata.GetMetadata<PermissionAttribute>();
                string permissionCode;
                if (permissionAttribute?.Code != null)
                {
                    permissionCode = permissionAttribute.Code;
                }
                else
                {
                    // 获取请求方法和终结点名称
                    string requestMethod = httpContext.Request.Method;
                    string endpointName = endpoint.DisplayName ?? endpoint.ToString();
                    string rawCode = $"{requestMethod}:{endpointName}";
                    permissionCode = rawCode.GenerateShortCode();
                    logger.LogInformation(rawCode + " => " + permissionCode);
                }

                if (userPermissions.Contains(permissionCode))
                {
                    logger.LogInformation("User {UserId} has permission {PermissionCode}", context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, permissionCode);

                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
