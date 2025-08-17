using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.Shared.DependencyInjection
{
    /// <summary>
    /// IServiceCollection 扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        // 缓存已扫描的程序集结果
        private static readonly ConcurrentDictionary<Assembly, Type[]> _typeCache = new();

        /// <summary>
        /// 使用Scrutor进行依赖注入自动注册
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="assemblies">要扫描的程序集</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDependencyInjectionWithScrutor(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[] { Assembly.GetCallingAssembly() };
            }

            // 注册 Scoped 服务（有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<IScopedDependency>()
                    .Where(type => type.GetInterfaces().Any(i =>
                        i != typeof(IScopedDependency) &&
                        i != typeof(ITransientDependency) &&
                        i != typeof(ISingletonDependency))))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // 注册 Scoped 服务（注册为自身，包括有业务接口和没有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<IScopedDependency>())
                .AsSelf()
                .WithScopedLifetime());

            // 注册 Transient 服务（有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<ITransientDependency>()
                    .Where(type => type.GetInterfaces().Any(i =>
                        i != typeof(IScopedDependency) &&
                        i != typeof(ITransientDependency) &&
                        i != typeof(ISingletonDependency))))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            // 注册 Transient 服务（注册为自身，包括有业务接口和没有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<ITransientDependency>())
                .AsSelf()
                .WithTransientLifetime());

            // 注册 Singleton 服务（有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<ISingletonDependency>()
                    .Where(type => type.GetInterfaces().Any(i =>
                        i != typeof(IScopedDependency) &&
                        i != typeof(ITransientDependency) &&
                        i != typeof(ISingletonDependency))))
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            // 注册 Singleton 服务（注册为自身，包括有业务接口和没有业务接口的）
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
                .AsSelf()
                .WithSingletonLifetime());

            return services;
        }

        /// <summary>
        /// 使用Scrutor进行高级依赖注入配置
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="assemblies">要扫描的程序集</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddAdvancedDependencyInjection(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[] { Assembly.GetCallingAssembly() };
            }

            services.Scan(scan => scan
                .FromAssemblies(assemblies)

                // 注册所有以Service结尾的类
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Service") &&
                           !type.IsAbstract &&
                           !type.IsInterface))
                .AsImplementedInterfaces()
                .WithScopedLifetime()

                // 注册所有Repository
                .AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Repository") &&
                           !type.IsAbstract &&
                           !type.IsInterface))
                .AsImplementedInterfaces()
                .WithScopedLifetime()

                // 注册标记接口的服务
                .AddClasses(classes => classes.AssignableTo<IScopedDependency>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()

                .AddClasses(classes => classes.AssignableTo<ITransientDependency>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()

                .AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            return services;
        }

        /// <summary>
        /// 使用Scrutor进行装饰器模式注册
        /// </summary>
        /// <typeparam name="TService">服务接口</typeparam>
        /// <typeparam name="TDecorator">装饰器实现</typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDecorator<TService, TDecorator>(
            this IServiceCollection services)
            where TService : class
            where TDecorator : class, TService
        {
            services.Decorate<TService, TDecorator>();
            return services;
        }

        /// <summary>
        /// 清除类型缓存
        /// </summary>
        public static void ClearTypeCache()
        {
            _typeCache.Clear();
        }
    }
}