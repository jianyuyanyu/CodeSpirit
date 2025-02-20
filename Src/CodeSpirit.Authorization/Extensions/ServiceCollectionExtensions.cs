using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Authorization.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeSpiritAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IPermissionService, PermissionService>();
            services.AddSingleton<IHasPermissionService, PermissionService>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DynamicPermissions", policy =>
                    policy.Requirements.Add(new PermissionRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, RolePermissionAuthorizationHandler>();
            return services;
        }

        public static async void UseCodeSpiritAuthorization(this IApplicationBuilder builder)
        {
            // 执行权限初始化
            var service = builder.ApplicationServices.GetRequiredService<IPermissionService>();
            await service.InitializePermissionTree();
        }
    }
}
