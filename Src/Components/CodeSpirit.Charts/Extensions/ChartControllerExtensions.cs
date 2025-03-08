using CodeSpirit.Charts.Analysis;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
        /// 自动获取当前调用方法的信息，从特性中读取配置
        /// </summary>
        public static IActionResult AutoChartResult(this ControllerBase controller, object data, ChartType? preferredType = null, [CallerMemberName] string? caller = null)
        {
            ArgumentNullException.ThrowIfNull(controller);

            var serviceProvider = controller.HttpContext.RequestServices;
            var recommender = serviceProvider.GetService<IChartRecommender>();
            var echartGenerator = serviceProvider.GetService<IEChartConfigGenerator>();
            var chartService = serviceProvider.GetService<IChartService>();

            if (recommender == null || echartGenerator == null)
            {
                return controller.BadRequest("未注册图表服务");
            }

            // 根据是否获取到方法信息使用不同的处理方式
            ChartConfig config;

            MethodInfo? methodInfo = null;
            if (!string.IsNullOrEmpty(caller))
            {
                methodInfo = controller.GetType().GetMethod(caller);
            }

            if (methodInfo != null && chartService != null)
            {
                // 如果获取到方法信息并且有图表服务，则使用方法特性生成配置
                config = chartService.AnalyzeAndGenerateChartAsync(data, methodInfo).GetAwaiter().GetResult();

                // 如果指定了首选图表类型，则覆盖
                if (preferredType.HasValue)
                {
                    config.Type = preferredType.Value;
                }
            }
            else
            {
                // 如果没有获取到方法信息或没有图表服务，则使用默认的推荐器生成配置
                config = recommender.GenerateChartConfig(data, preferredType);
            }

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