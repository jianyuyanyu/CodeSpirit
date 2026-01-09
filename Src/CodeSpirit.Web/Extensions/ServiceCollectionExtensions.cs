using AutoMapper;
using CodeSpirit.Aggregator.Services;
using CodeSpirit.Shared.DependencyInjection;
using CodeSpirit.Shared.Services;
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
            
            // 注册 AutoMapper - 手动注册以避免版本冲突
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(ServiceCollectionExtensions).Assembly);
            });
            var mapper = mapperConfig.CreateMapper();
            services.AddSingleton<IMapper>(mapper);
            
            // 使用Scrutor自动注册标记接口的服务 - 注册CodeSpirit.Shared程序集中的服务
            var assembliesToScan = new[]
            {
                typeof(ServiceCollectionExtensions).Assembly, // CodeSpirit.Web程序集
                typeof(IAiTaskService).Assembly // CodeSpirit.Shared程序集
            };
            services.AddDependencyInjectionWithScrutor(assembliesToScan);
            
            return services;
        }

        public static IServiceCollection AddProxyServices(this IServiceCollection services)
        {
            // 注册HTTP客户端工厂
            services.AddHttpClient();

            // 注册聚合器服务
            services.AddSingleton<IAggregatorService, JsonNetAggregatorService>();

            // 注册客户端IP地址获取服务
            services.AddSingleton<IClientIpService, ClientIpService>();

            return services;
        }
    }
}