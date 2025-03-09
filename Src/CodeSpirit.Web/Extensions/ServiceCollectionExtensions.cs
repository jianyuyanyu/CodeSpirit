using CodeSpirit.Aggregator.Services;
using CodeSpirit.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Web.Extensions
{
    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册应用程序服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 注册JWT认证服务
            services.AddScoped<IJwtAuthService, JwtAuthService>();
            
            return services;
        }

        public static IServiceCollection AddProxyServices(this IServiceCollection services)
        {
            // 注册HTTP客户端工厂
            services.AddHttpClient();

            // 注册聚合器服务
            services.AddSingleton<IAggregatorService, JsonNetAggregatorService>();

            return services;
        }
    }
}