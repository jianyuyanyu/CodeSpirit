using CodeSpirit.Aggregator.Middlewares;
using CodeSpirit.Aggregator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CodeSpirit.Aggregator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加CodeSpirit聚合器服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureGlobalRules">配置全局聚合规则的委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCodeSpiritAggregator(
            this IServiceCollection services, 
            Action<IGlobalAggregatorConfigurationService> configureGlobalRules = null)
        {
            // 如果提供了全局规则配置委托，使用工厂方法注册并应用配置
            if (configureGlobalRules != null)
            {
                services.AddSingleton<IGlobalAggregatorConfigurationService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<GlobalAggregatorConfigurationService>>();
                    var globalConfigService = new GlobalAggregatorConfigurationService(logger);
                    configureGlobalRules(globalConfigService);
                    return globalConfigService;
                });
            }
            else
            {
                // 否则使用默认注册
                services.AddSingleton<IGlobalAggregatorConfigurationService, GlobalAggregatorConfigurationService>();
            }
            
            // 注册聚合头部服务
            services.AddSingleton<IAggregationHeaderService, AggregationHeaderService>();

            return services;
        }

        /// <summary>
        /// 使用CodeSpirit聚合器中间件
        /// </summary>
        /// <param name="builder">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseCodeSpiritAggregator(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<AggregationHeaderMiddleware>();
            return builder;
        }

        /// <summary>
        /// 配置常用的全局聚合规则
        /// </summary>
        /// <param name="globalConfig">全局聚合器配置服务</param>
        public static void ConfigureCommonGlobalRules(this IGlobalAggregatorConfigurationService globalConfig)
        {
            // 配置CreatedBy字段的全局规则
            globalConfig.RegisterGlobalRule(
                "CreatedBy", 
                "http://identity/api/identity/internal/users/{value}.data.name", 
                "{field}");

            // 配置UpdatedBy字段的全局规则
            globalConfig.RegisterGlobalRule(
                "UpdatedBy", 
                "http://identity/api/identity/internal/users/{value}.data.name", 
                "{field}");
        }
    }
}
