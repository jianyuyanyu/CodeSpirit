using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeSpiritNavigation(this IServiceCollection services)
        {
            services.AddSingleton<INavigationService, NavigationService>();
            return services;
        }

        public static async Task UseCodeSpiritNavigationAsync(this IApplicationBuilder builder)
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
        }
    }
}
