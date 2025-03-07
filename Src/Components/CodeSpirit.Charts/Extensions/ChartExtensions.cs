using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Extensions
{
    /// <summary>
    /// 图表扩展方法
    /// </summary>
    public static class ChartExtensions
    {
        /// <summary>
        /// 添加智能图表服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCharts(this IServiceCollection services, Action<ChartOptions>? configureOptions = null)
        {
            // 配置选项
            var options = new ChartOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            
            // 注册核心服务
            services.AddSingleton<IDataAnalyzer, DataAnalyzer>();
            services.AddSingleton<IChartRecommender, ChartRecommender>();
            services.AddScoped<IChartService, ChartService>();
            services.AddSingleton<ChartConfigBuilder>();
            services.AddSingleton<IEChartConfigGenerator, EChartConfigGenerator>();
            
            // 注册HTTP客户端，用于API数据源
            services.AddHttpClient();
            
            // 确保日志服务可用
            if (!services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
            {
                services.AddLogging();
            }
            
            return services;
        }
        
        /// <summary>
        /// 添加CodeSpirit图表服务（兼容性方法）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCodeSpiritCharts(this IServiceCollection services)
        {
            return services.AddCharts();
        }
    }
    
    /// <summary>
    /// 图表配置选项
    /// </summary>
    public class ChartOptions
    {
        /// <summary>
        /// 默认主题
        /// </summary>
        public string DefaultTheme { get; set; } = "light";
        
        /// <summary>
        /// 是否启用AI分析
        /// </summary>
        public bool EnableAI { get; set; } = true;
        
        /// <summary>
        /// 最大数据点数量
        /// </summary>
        public int MaxDataPoints { get; set; } = 10000;
        
        /// <summary>
        /// 是否启用导出功能
        /// </summary>
        public bool EnableExport { get; set; } = true;
        
        /// <summary>
        /// 默认缓存时间（分钟）
        /// </summary>
        public int CacheMinutes { get; set; } = 30;
    }
} 