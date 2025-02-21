using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using CodeSpirit.Core.Attributes;

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
                string permissionName = null;
                if (permissionAttribute?.Name != null)
                {
                    permissionName = permissionAttribute.Name;
                }
                else
                {
                    // 如果没有显式指定权限名称，则根据路由生成默认的权限代码
                    string controllerName = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName;
                    string actionName = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ActionName;
                    
                    if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                    {
                        string modulePrefix = endpoint.Metadata.GetMetadata<ModuleAttribute>()?.Name ?? "default";
                        // 生成格式：{module}_{controller}_{action}，与 PermissionService 保持一致
                        permissionName = $"{modulePrefix}_{controllerName.ToCamelCase()}_{actionName.ToCamelCase()}";
                    }
                    else
                    {
                        logger.LogWarning("Unable to determine permission name for endpoint {EndpointDisplayName}", endpoint.DisplayName);
                        return Task.CompletedTask;
                    }
                }

                var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
                if (permissionService.HasPermission(permissionName, userPermissions))
                {
                    logger.LogInformation("User {UserId} has permission {PermissionCode}", context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, permissionName);
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
