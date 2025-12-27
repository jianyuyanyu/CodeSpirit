using CodeSpirit.Amis.Helpers;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Services.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册导航服务（重构后）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCodeSpiritNavigation(this IServiceCollection services)
        {
            // 注册核心服务
            services.AddSingleton<INavigationTreeBuilder, NavigationTreeBuilder>();
            services.AddSingleton<INavigationCacheManager, NavigationCacheManager>();
            services.AddSingleton<INavigationFilterService, NavigationFilterService>();
            services.AddSingleton<INavigationService, NavigationService>();
            
            // 注册本地化服务（Scoped，因为需要访问 HttpContext）
            // 注意：CultureResolver 应该在 Amis 组件中注册，如果没有注册则这里注册一个
            if (!services.Any(s => s.ServiceType == typeof(CultureResolver)))
            {
                services.AddScoped<CultureResolver>();
            }
            services.AddScoped<INavigationLocalizationService, NavigationLocalizationService>();

            // 注册所有过滤器
            services.AddSingleton<INavigationFilter, PlatformFilter>();
            services.AddSingleton<INavigationFilter, PermissionFilter>();
            services.AddSingleton<INavigationFilter, AuthenticationFilter>();
            services.AddSingleton<INavigationFilter, VersionFilter>();
            services.AddSingleton<INavigationFilter, DeviceFilter>();
            services.AddSingleton<INavigationFilter, ExperimentalFilter>();
            services.AddSingleton<INavigationFilter, GroupFilter>();
            services.AddSingleton<INavigationFilter, TagFilter>();

            return services;
        }

        /// <summary>
        /// 初始化导航服务
        /// </summary>
        /// <param name="builder">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static async Task<IApplicationBuilder> UseCodeSpiritNavigationAsync(this IApplicationBuilder builder)
        {
            try
            {
                using var scope = builder.ApplicationServices.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INavigationService>();
                await service.InitializeNavigationTree();
            }
            catch (Exception ex)
            {
                var logger = builder.ApplicationServices.GetService<ILogger<NavigationService>>();
                logger?.LogError(ex, "Failed to initialize navigation tree. Application will continue with empty navigation.");
            }

            return builder;
        }
    }
}
