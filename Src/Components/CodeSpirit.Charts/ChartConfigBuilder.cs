using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Charts
{
    /// <summary>
    /// 图表配置构建器
    /// </summary>
    public class ChartConfigBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ChartConfig _config;
        private readonly IChartRecommender? _recommender;
        private readonly IEChartConfigGenerator? _echartGenerator;
        
        private const string CacheKeyPrefix = "ChartConfig:";
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartConfigBuilder(
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _config = new ChartConfig();
        }
        
        /// <summary>
        /// 构造函数（带服务注入）
        /// </summary>
        public ChartConfigBuilder(
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor,
            IChartRecommender recommender,
            IEChartConfigGenerator echartGenerator)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _config = new ChartConfig();
            _recommender = recommender;
            _echartGenerator = echartGenerator;
        }
        
        /// <summary>
        /// 为控制器方法生成图表配置
        /// </summary>
        /// <param name="endpoint">HTTP端点</param>
        /// <returns>图表JSON配置</returns>
        public async Task<JObject> BuildChartConfigAsync(Endpoint endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }
            
            // 从端点获取控制器方法
            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor == null)
            {
                return null;
            }
            
            // 检查方法是否有Chart特性
            var methodInfo = actionDescriptor.MethodInfo;
            var chartAttr = methodInfo.GetCustomAttribute<ChartAttribute>();
            if (chartAttr == null)
            {
                return null;
            }
            
            // 尝试从缓存获取配置
            var cacheKey = $"{CacheKeyPrefix}{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
            if (_memoryCache.TryGetValue(cacheKey, out JObject cachedConfig))
            {
                return cachedConfig;
            }
            
            // 生成图表配置
            var chartService = _serviceProvider.GetRequiredService<IChartService>();
            var chartConfig = await chartService.GenerateChartConfigAsync(methodInfo);
            
            // 配置API URL
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var apiUrl = $"{baseUrl}{request.Path}";
            
            chartConfig.DataSource = new ChartDataSource
            {
                Type = DataSourceType.Api,
                ApiUrl = apiUrl,
                Method = "GET"
            };
            
            // 生成JSON配置
            var chartJson = await chartService.GenerateChartJsonAsync(chartConfig);
            
            // 缓存配置
            _memoryCache.Set(cacheKey, chartJson, TimeSpan.FromMinutes(30));
            
            return chartJson;
        }
        
        /// <summary>
        /// 为数据生成图表配置
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="preferredType">首选图表类型</param>
        /// <returns>图表JSON配置</returns>
        public async Task<JObject> BuildChartConfigForDataAsync(object data, ChartType? preferredType = null)
        {
            // 分析数据并生成图表配置
            var chartRecommender = _serviceProvider.GetRequiredService<IChartRecommender>();
            var chartConfig = chartRecommender.GenerateChartConfig(data, preferredType);
            
            // 生成JSON配置
            var chartService = _serviceProvider.GetRequiredService<IChartService>();
            return await chartService.GenerateChartJsonAsync(chartConfig);
        }
        
        /// <summary>
        /// 获取推荐的多个图表配置
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="maxCount">最大推荐数量</param>
        /// <returns>多个图表配置</returns>
        public async Task<Dictionary<ChartType, JObject>> GetRecommendedChartConfigsAsync(object data, int maxCount = 3)
        {
            // 获取推荐的图表类型
            var chartRecommender = _serviceProvider.GetRequiredService<IChartRecommender>();
            var recommendedTypes = chartRecommender.RecommendChartTypes(data, maxCount);
            
            // 为每种类型生成配置
            var result = new Dictionary<ChartType, JObject>();
            var chartService = _serviceProvider.GetRequiredService<IChartService>();
            
            foreach (var type in recommendedTypes.Keys)
            {
                var config = chartRecommender.GenerateChartConfig(data, type);
                var json = await chartService.GenerateChartJsonAsync(config);
                result[type] = json;
            }
            
            return result;
        }
        
        /// <summary>
        /// 生成ECharts配置对象
        /// </summary>
        public object GenerateEChartConfig()
        {
            if (_echartGenerator != null)
            {
                return _echartGenerator.GenerateEChartConfig(_config);
            }
            
            throw new InvalidOperationException("未注入EChartConfigGenerator服务");
        }
        
        /// <summary>
        /// 生成ECharts配置JSON字符串
        /// </summary>
        public string GenerateEChartConfigJson()
        {
            if (_echartGenerator != null)
            {
                return _echartGenerator.GenerateEChartConfigJson(_config);
            }
            
            throw new InvalidOperationException("未注入EChartConfigGenerator服务");
        }
        
        /// <summary>
        /// 生成完整的ECharts配置对象（包含数据）
        /// </summary>
        public object GenerateCompleteEChartConfig(object data)
        {
            if (_echartGenerator != null)
            {
                return _echartGenerator.GenerateCompleteEChartConfig(_config, data);
            }
            
            throw new InvalidOperationException("未注入EChartConfigGenerator服务");
        }
        
        /// <summary>
        /// 设置图表标题
        /// </summary>
        public ChartConfigBuilder SetTitle(string title)
        {
            _config.Title = title;
            return this;
        }
        
        /// <summary>
        /// 设置图表副标题
        /// </summary>
        public ChartConfigBuilder SetSubtitle(string subtitle)
        {
            _config.Subtitle = subtitle;
            return this;
        }
    }
} 