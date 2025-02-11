using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Authorization
{
    public static class Extensions
    {
        public static IServiceCollection AddCodeSpiritAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IPermissionService,PermissionService>();
            services.AddSingleton<IHasPermissionService, PermissionService>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DynamicPermissions", policy =>
                    policy.Requirements.Add(new PermissionRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, RolePermissionAuthorizationHandler>();
            return services;
        }
    }
}
