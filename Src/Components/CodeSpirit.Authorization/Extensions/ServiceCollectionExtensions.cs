using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Authorization.Services;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.Authorization.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加CodeSpirit完整权限验证系统
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCodeSpiritAuthorization(this IServiceCollection services)
        {
            // 权限树服务保持Singleton生命周期
            services.AddSingleton<IPermissionService, PermissionService>();
            
            // 权限检查服务使用Scoped生命周期，提高性能
            services.AddScoped<IHasPermissionService, HasPermissionService>();
            
            // 添加授权策略和处理器
            services.AddAuthorization(options =>
            {
                // 原有的动态权限策略
                options.AddPolicy("DynamicPermissions", policy =>
                    policy.Requirements.Add(new PermissionRequirement()));
                
                // 新增的平台权限策略
                options.AddPolicy("Platform_System", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.System)));
                
                options.AddPolicy("Platform_Tenant", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.Tenant)));
                
                options.AddPolicy("Platform_Both", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.Both)));
            });
            
            // 注册权限验证处理器
            services.AddSingleton<IAuthorizationHandler, RolePermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PlatformAuthorizationHandler>();
            
            return services;
        }

        /// <summary>
        /// 仅添加平台权限验证（用于只需要平台权限的场景）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPlatformAuthorization(this IServiceCollection services)
        {
            // 注册平台权限验证处理器
            services.AddScoped<IAuthorizationHandler, PlatformAuthorizationHandler>();
            
            // 配置授权策略
            services.Configure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("Platform_System", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.System)));
                
                options.AddPolicy("Platform_Tenant", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.Tenant)));
                
                options.AddPolicy("Platform_Both", policy =>
                    policy.Requirements.Add(new PlatformRequirement(PlatformType.Both)));
            });

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
