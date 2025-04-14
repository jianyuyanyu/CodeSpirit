using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Authorization.Services;

namespace CodeSpirit.Authorization.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeSpiritAuthorization(this IServiceCollection services)
        {
            // 权限树服务保持Singleton生命周期
            services.AddSingleton<IPermissionService, PermissionService>();
            
            // 权限检查服务使用Scoped生命周期，提高性能
            services.AddScoped<IHasPermissionService, HasPermissionService>();
            
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
