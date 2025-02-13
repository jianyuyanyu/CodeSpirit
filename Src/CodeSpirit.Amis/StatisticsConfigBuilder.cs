using Newtonsoft.Json.Linq;
using System.Reflection;
using CodeSpirit.Amis.Helpers;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// 统计图表配置生成器，用于构建统计相关的 AMIS 配置。
    /// </summary>
    public class StatisticsConfigBuilder
    {
        private readonly ControllerHelper _controllerHelper;

        /// <summary>
        /// 初始化统计图表配置生成器的新实例。
        /// </summary>
        /// <param name="controllerHelper">控制器帮助类</param>
        public StatisticsConfigBuilder(ControllerHelper controllerHelper)
        {
            _controllerHelper = controllerHelper;
        }

        /// <summary>
        /// 为指定控制器构建统计图表配置。
        /// </summary>
        /// <param name="controllerType">控制器类型</param>
        /// <returns>统计图表配置的 JSON 对象</returns>
        public JObject BuildStatisticsConfig(Type controllerType)
        {
            var routePrefix = _controllerHelper.GetControllerRoutePrefix(controllerType);
            var displayName = _controllerHelper.GetControllerDisplayName(controllerType);
            
            var statisticsMethods = GetStatisticsMethods(controllerType);
            if (!statisticsMethods.Any()) return null;

            return new JObject
            {
                ["type"] = "page",
                ["title"] = displayName,
                ["body"] = GenerateStatisticsBody(controllerType, routePrefix, statisticsMethods)
            };
        }

        /// <summary>
        /// 获取控制器中的统计相关方法。
        /// </summary>
        private IEnumerable<MethodInfo> GetStatisticsMethods(Type controllerType)
        {
            return controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => IsStatisticsMethod(method));
        }

        /// <summary>
        /// 判断方法是否为统计方法。
        /// </summary>
        private bool IsStatisticsMethod(MethodInfo method)
        {
            return HasHttpGetAttribute(method) && 
                   (HasStatisticsAttribute(method) || IsStatisticsMethodByName(method));
        }

        /// <summary>
        /// 检查方法是否有 HttpGet 特性。
        /// </summary>
        private bool HasHttpGetAttribute(MethodInfo method)
        {
            return method.GetCustomAttributes()
                .Any(attr => attr.GetType().Name.StartsWith("HttpGet"));
        }

        /// <summary>
        /// 检查方法是否有统计相关特性。
        /// </summary>
        private bool HasStatisticsAttribute(MethodInfo method)
        {
            return method.GetCustomAttributes().Any(attr =>
                attr.GetType().Name.Contains("Statistics") ||
                attr.GetType().Name.Contains("Chart") ||
                (attr is DisplayAttribute display &&
                 (display.Name?.Contains("统计") == true ||
                  display.Name?.Contains("图表") == true)));
        }

        /// <summary>
        /// 通过方法名判断是否为统计方法。
        /// </summary>
        private bool IsStatisticsMethodByName(MethodInfo method)
        {
            var name = method.Name.ToLower();
            return name.Contains("statistics") ||
                   name.Contains("chart") ||
                   name.Contains("report") ||
                   name.Contains("analytics");
        }

        /// <summary>
        /// 生成统计页面主体配置。
        /// </summary>
        private JArray GenerateStatisticsBody(Type controllerType, string routePrefix, IEnumerable<MethodInfo> statisticsMethods)
        {
            return new JArray
            {
                CreateStatisticsForm(controllerType, routePrefix, statisticsMethods)
            };
        }

        /// <summary>
        /// 创建统计表单配置，包含日期范围选择器和图表网格。
        /// </summary>
        private JObject CreateStatisticsForm(Type controllerType, string routePrefix, IEnumerable<MethodInfo> statisticsMethods)
        {
            return new JObject
            {
                ["type"] = "form",
                ["title"] = "查询条件",
                ["mode"] = "inline",
                ["body"] = new JArray
                {
                    CreateDateRangeFilter(),
                    CreateChartsGrid(controllerType, routePrefix, statisticsMethods)
                },
                ["actions"] = new JArray(),
                ["submitOnChange"] = true,
                ["trackExpression"] = "${dateRange}",
                ["messages"] = new JObject { ["fetchSuccess"] = "" }
            };
        }

        /// <summary>
        /// 创建日期范围选择器配置。
        /// </summary>
        private JObject CreateDateRangeFilter()
        {
            return new JObject
            {
                ["type"] = "input-date-range",
                ["name"] = "dateRange",
                ["label"] = "时间范围",
                ["format"] = "YYYY-MM-DD",
                ["value"] = "-30days,today"
            };
        }

        /// <summary>
        /// 创建图表网格配置。
        /// </summary>
        private JObject CreateChartsGrid(Type controllerType, string routePrefix, IEnumerable<MethodInfo> statisticsMethods)
        {
            return new JObject
            {
                ["type"] = "grid",
                ["columns"] = new JArray(
                    statisticsMethods.Select(method => CreateChartColumn(controllerType, routePrefix, method))
                )
            };
        }

        /// <summary>
        /// 创建单个图表列配置。
        /// </summary>
        private JObject CreateChartColumn(Type controllerType, string routePrefix, MethodInfo method)
        {
            return new JObject
            {
                ["md"] = 12 / Math.Min(2, 2), // 固定2列
                ["body"] = GenerateChartConfig(controllerType, routePrefix, method.Name, new { height = 300 })
            };
        }

        /// <summary>
        /// 生成图表配置。
        /// </summary>
        private JObject GenerateChartConfig(Type controllerType, string routePrefix, string methodName, object additionalProps)
        {
            var methodInfo = controllerType.GetMethod(methodName);
            if (methodInfo == null) return null;

            var route = _controllerHelper.GetMethodRoute(methodInfo);
            var displayName = _controllerHelper.GetMethodDisplayName(methodInfo);
            var chartId = $"chart-{route}";

            var config = new JObject
            {
                ["type"] = "chart",
                ["api"] = new JObject
                {
                    ["url"] = $"${{API_HOST}}/{routePrefix}/{route}?dateRange=${{dateRange}}",
                    ["method"] = "get"
                },
                ["title"] = displayName,
                ["name"] = chartId,
                ["id"] = chartId
            };

            AddAdditionalProperties(config, additionalProps);
            return config;
        }

        /// <summary>
        /// 添加额外的图表属性配置。
        /// </summary>
        private void AddAdditionalProperties(JObject config, object additionalProps)
        {
            if (additionalProps == null) return;

            foreach (var prop in additionalProps.GetType().GetProperties())
            {
                config[prop.Name.ToCamelCase()] = JToken.FromObject(prop.GetValue(additionalProps));
            }
        }
    }
} 