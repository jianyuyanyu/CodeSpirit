using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Charts.Extensions
{
    /// <summary>
    /// 服务集合扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加CodeSpirit.Charts服务
        /// </summary>
        public static IServiceCollection AddCodeSpiritCharts(this IServiceCollection services)
        {
            // 注册数据分析器
            services.AddSingleton<IDataAnalyzer, DataAnalyzer>();
            
            // 注册图表推荐器
            services.AddSingleton<IChartRecommender, ChartRecommender>();
            
            // 注册ECharts配置生成器
            services.AddSingleton<IEChartConfigGenerator, EChartConfigGenerator>();
            
            return services;
        }
    }
} 