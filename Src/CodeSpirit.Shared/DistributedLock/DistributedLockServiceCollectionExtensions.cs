using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace CodeSpirit.Shared.DistributedLock
{
    /// <summary>
    /// 分布式锁服务集合扩展
    /// </summary>
    public static class DistributedLockServiceCollectionExtensions
    {
        /// <summary>
        /// 添加基于Redis的分布式锁服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisDistributedLock(
            this IServiceCollection services,
            Action<RedisDistributedLockOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
            
            services.Configure(configureOptions);
            services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
            
            return services;
        }
        
        /// <summary>
        /// 添加基于Redis的分布式锁服务（使用现有Redis连接）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <param name="redisConnectionFactory">Redis连接工厂</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRedisDistributedLock(
            this IServiceCollection services,
            Action<RedisDistributedLockOptions> configureOptions,
            Func<IServiceProvider, IConnectionMultiplexer> redisConnectionFactory)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
            
            if (redisConnectionFactory == null)
                throw new ArgumentNullException(nameof(redisConnectionFactory));
            
            services.Configure(configureOptions);
            services.TryAddSingleton(redisConnectionFactory);
            services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
            
            return services;
        }
    }
} 