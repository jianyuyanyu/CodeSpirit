using CodeSpirit.Aggregator.Middlewares;
using CodeSpirit.Aggregator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Aggregator
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCodeSpiritAggregator(this IServiceCollection services)
        {
            services.AddSingleton<IAggregationHeaderService, AggregationHeaderService>();
            return services;
        }

        public static IApplicationBuilder UseCodeSpiritAggregator(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AggregationHeaderMiddleware>();
            return builder;
        }
    }
}
