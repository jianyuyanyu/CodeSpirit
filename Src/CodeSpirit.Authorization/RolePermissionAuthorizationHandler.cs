using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class RolePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permissions = endpoint?.Metadata.GetMetadata<PermissionAttribute>();
        if (permissions != null && context.User.HasClaim(c => c.Type == "permissions" && c.Value == permissions.Name))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

