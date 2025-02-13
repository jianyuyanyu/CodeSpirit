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
        /// 批量注册依赖注入
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="assemblies">要扫描的程序集</param>
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, params Assembly[] assemblies)
        {
            // 预先获取依赖注入标记接口类型，避免重复获取
            var singletonType = typeof(ISingletonDependency);
            var scopedType = typeof(IScopedDependency);
            var transientType = typeof(ITransientDependency);

            // 记录扫描的程序集
            Console.WriteLine($"Scanning assemblies: {string.Join(", ", assemblies.Select(a => a.GetName().Name))}");

            foreach (var assembly in assemblies)
            {
                // 从缓存中获取或扫描程序集
                var types = _typeCache.GetOrAdd(assembly, asm =>
                {
                    var foundTypes = asm.GetTypes()
                        .Where(type => !type.IsInterface && !type.IsAbstract)
                        .ToArray();
                    Console.WriteLine($"Found {foundTypes.Length} concrete types in {asm.GetName().Name}");
                    return foundTypes;
                });

                // 使用 HashSet 存储已注册的类型，避免重复注册
                var registeredTypes = new HashSet<Type>();

                foreach (var type in types)
                {
                    if (registeredTypes.Contains(type))
                        continue;

                    // 获取类实现的接口
                    var interfaces = type.GetInterfaces();

                    // 注册服务
                    void RegisterService(ServiceLifetime lifetime)
                    {
                        // 获取该类型实现的所有非依赖注入标记接口
                        var serviceInterfaces = interfaces
                            .Where(i => i != singletonType && 
                                   i != scopedType && 
                                   i != transientType)
                            .ToList(); // 移除了命名空间限制，以便更灵活地注册服务

                        if (serviceInterfaces.Any())
                        {
                            // 为每个服务接口注册实现
                            foreach (var serviceInterface in serviceInterfaces)
                            {
                                services.Add(new ServiceDescriptor(serviceInterface, type, lifetime));
                                Console.WriteLine($"Registered {serviceInterface.Name} -> {type.Name} as {lifetime}");
                            }
                        }
                        else
                        {
                            // 如果没有找到匹配的接口，则注册类型本身
                            services.Add(new ServiceDescriptor(type, type, lifetime));
                            Console.WriteLine($"Registered {type.Name} as {lifetime}");
                        }
                        registeredTypes.Add(type);
                    }

                    // 根据依赖注入标记接口选择生命周期
                    if (interfaces.Contains(singletonType))
                    {
                        RegisterService(ServiceLifetime.Singleton);
                    }
                    else if (interfaces.Contains(scopedType))
                    {
                        RegisterService(ServiceLifetime.Scoped);
                    }
                    else if (interfaces.Contains(transientType))
                    {
                        RegisterService(ServiceLifetime.Transient);
                    }
                }
            }
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