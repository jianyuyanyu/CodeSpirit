using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Navigation.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeSpiritNavigation(this IServiceCollection services)
        {
            services.AddSingleton<INavigationService, NavigationService>();
            return services;
        }

        public static async void UseCodeSpiritNavigation(this IApplicationBuilder builder)
        {
            // 执行导航初始化
            var service = builder.ApplicationServices.GetRequiredService<INavigationService>();
            await service.InitializeNavigationTree();
        }
    }
}
