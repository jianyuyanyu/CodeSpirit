using System.Text.Json;
using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Charts.Extensions
{
    /// <summary>
    /// 图表控制器扩展方法
    /// </summary>
    public static class ChartControllerExtensions
    {
        /// <summary>
        /// 返回ECharts配置JSON结果
        /// </summary>
        public static IActionResult ChartResult(this ControllerBase controller, ChartConfig config, object data)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentNullException.ThrowIfNull(config);
            
            var serviceProvider = controller.HttpContext.RequestServices;
            var echartGenerator = serviceProvider.GetService<IEChartConfigGenerator>();
            
            if (echartGenerator == null)
            {
                return controller.BadRequest("未注册EChartConfigGenerator服务");
            }
            
            var echartConfig = echartGenerator.GenerateCompleteEChartConfig(config, data);
            return new JsonResult(echartConfig);
        }
        
        /// <summary>
        /// 返回使用自动推荐的ECharts配置JSON结果
        /// </summary>
        public static IActionResult AutoChartResult(this ControllerBase controller, object data, ChartType? preferredType = null)
        {
            ArgumentNullException.ThrowIfNull(controller);
            
            var serviceProvider = controller.HttpContext.RequestServices;
            var recommender = serviceProvider.GetService<IChartRecommender>();
            var echartGenerator = serviceProvider.GetService<IEChartConfigGenerator>();
            
            if (recommender == null || echartGenerator == null)
            {
                return controller.BadRequest("未注册图表服务");
            }
            
            var config = recommender.GenerateChartConfig(data, preferredType);
            var echartConfig = echartGenerator.GenerateCompleteEChartConfig(config, data);
            return new JsonResult(echartConfig);
        }
        
        /// <summary>
        /// 返回多种图表推荐选项
        /// </summary>
        public static IActionResult ChartRecommendations(this ControllerBase controller, object data, int maxCount = 3)
        {
            ArgumentNullException.ThrowIfNull(controller);
            
            var serviceProvider = controller.HttpContext.RequestServices;
            var recommender = serviceProvider.GetService<IChartRecommender>();
            var echartGenerator = serviceProvider.GetService<IEChartConfigGenerator>();
            
            if (recommender == null || echartGenerator == null)
            {
                return controller.BadRequest("未注册图表服务");
            }
            
            // 获取推荐的图表类型
            var recommendations = recommender.RecommendChartTypes(data, maxCount);
            
            // 为每种推荐的图表类型生成配置
            var result = new List<object>();
            foreach (var recommendation in recommendations)
            {
                var config = recommender.GenerateChartConfig(data, recommendation.Key);
                var echartConfig = echartGenerator.GenerateCompleteEChartConfig(config, data);
                
                result.Add(new 
                {
                    Type = recommendation.Key.ToString(),
                    Score = recommendation.Value,
                    Config = echartConfig
                });
            }
            
            return new JsonResult(result);
        }
    }
} 