using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Shared.Extensions.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static Lazy<T> GetRequiredServiceLazy<T>(this IServiceProvider serviceProvider) where T : class
        {
            return new Lazy<T>(() =>
            {
                T service = serviceProvider.GetService<T>();

                if (service == null)
                {
                    throw new InvalidOperationException($"Cannot resolve required service: {typeof(T)}");
                }

                return service;
            });
        }

        public static Lazy<T> GetServiceLazy<T>(this IServiceProvider serviceProvider) where T : class
        {
            return new Lazy<T>(() =>
            {
                return serviceProvider.GetService<T>();
            });
        }
    }
}
