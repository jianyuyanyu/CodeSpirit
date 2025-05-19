using CodeSpirit.Charts.AmisIntegration;
using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Core.Services;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Charts.Extensions
{
    /// <summary>
    /// 图表服务注册扩展类
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加图表服务 - 推荐使用的主要方法
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configure">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddChartServices(
            this IServiceCollection services,
            Action<ChartOptions>? configure = null)
        {
            // 配置选项
            var options = new ChartOptions();
            configure?.Invoke(options);
            services.Configure<ChartOptions>(opt =>
            {
                opt.DefaultProvider = options.DefaultProvider;
                opt.EnableCache = options.EnableCache;
                opt.CacheExpiration = options.CacheExpiration;
                opt.MaxCacheSize = options.MaxCacheSize;
                opt.DefaultTheme = options.DefaultTheme;
                opt.EnableAI = options.EnableAI;
                opt.EnableExport = options.EnableExport;
                
                foreach (var (key, value) in options.ThemeConfigurations)
                {
                    opt.ThemeConfigurations[key] = value;
                }
            });

            // 注册核心服务
            services.AddSingleton<IChartService, ChartService>();
            services.AddSingleton<IDataProcessor, DataProcessor>();
            services.AddSingleton<IChartRecommender, ChartRecommender>();
            services.AddSingleton<IChartThemeManager, ChartThemeManager>();
            
            // 注册数据分析服务
            //services.AddSingleton<IDataAnalyzer, DataAnalyzer>();

            // 注册 ECharts 提供者
            services.AddSingleton<IChartProvider, EChartsProvider>();
            services.AddSingleton<IChartRenderer, EChartsRenderer>();

            // 注册 Amis 集成服务
            services.AddSingleton<AmisChartConfigGenerator>();
            
            // 确保日志服务可用
            if (!services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
            {
                services.AddLogging();
            }
            
            // 注册HTTP客户端，用于API数据源
            services.AddHttpClient();

            return services;
        }

        /// <summary>
        /// 添加自定义图表提供者
        /// </summary>
        /// <typeparam name="TProvider">提供者类型</typeparam>
        /// <typeparam name="TRenderer">渲染器类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddChartProvider<TProvider, TRenderer>(this IServiceCollection services)
            where TProvider : class, IChartProvider
            where TRenderer : class, IChartRenderer
        {
            services.AddSingleton<IChartProvider, TProvider>();
            services.AddSingleton<IChartRenderer, TRenderer>();
            return services;
        }

        /// <summary>
        /// 添加自定义数据处理器
        /// </summary>
        /// <typeparam name="TProcessor">数据处理器类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDataProcessor<TProcessor>(this IServiceCollection services)
            where TProcessor : class, IDataProcessor
        {
            services.AddSingleton<IDataProcessor, TProcessor>();
            return services;
        }

        /// <summary>
        /// 添加自定义图表推荐器
        /// </summary>
        /// <typeparam name="TRecommender">图表推荐器类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddChartRecommender<TRecommender>(this IServiceCollection services)
            where TRecommender : class, IChartRecommender
        {
            services.AddSingleton<IChartRecommender, TRecommender>();
            return services;
        }

        /// <summary>
        /// 添加自定义主题管理器
        /// </summary>
        /// <typeparam name="TThemeManager">主题管理器类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddThemeManager<TThemeManager>(this IServiceCollection services)
            where TThemeManager : class, IChartThemeManager
        {
            services.AddSingleton<IChartThemeManager, TThemeManager>();
            return services;
        }
    }
}