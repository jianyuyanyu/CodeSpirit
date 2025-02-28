using CodeSpirit.Web.Services;
namespace CodeSpirit.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
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