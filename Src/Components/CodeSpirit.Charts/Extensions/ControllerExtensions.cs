using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Charts.Extensions;

/// <summary>
/// 控制器扩展方法
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// 自动生成图表结果
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="data">数据</param>
    /// <param name="options">选项</param>
    /// <returns>图表配置</returns>
    public static async Task<IActionResult> AutoChartResult(this ControllerBase controller, object data, object? options = null)
    {
        // 获取服务
        var chartService = controller.HttpContext.RequestServices.GetService(typeof(IChartService)) as IChartService;
        var logger = controller.HttpContext.RequestServices.GetService<ILogger<object>>();
        
        if (chartService == null)
        {
            throw new InvalidOperationException("IChartService is not registered in the service collection");
        }

        logger?.LogInformation("AutoChartResult - 输入数据类型: {DataType}", data?.GetType().FullName);
        logger?.LogInformation("AutoChartResult - 输入数据内容: {Data}", Newtonsoft.Json.JsonConvert.SerializeObject(data));

        // 获取当前方法信息 - 改进的方式
        MethodInfo? methodInfo = null;
        
        // 尝试从ActionDescriptor获取方法信息
        if (controller.ControllerContext.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            methodInfo = controllerActionDescriptor.MethodInfo;
        }
        
        // 如果从ActionDescriptor获取失败，则尝试使用RouteData
        if (methodInfo == null)
        {
            var actionName = controller.RouteData.Values["action"]?.ToString() ?? string.Empty;
            methodInfo = controller.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => string.Equals(m.Name, actionName, StringComparison.OrdinalIgnoreCase));
        }
        
        // 如果仍然获取失败，则抛出异常
        if (methodInfo == null)
        {
            var actionName = controller.RouteData.Values["action"]?.ToString() ?? "unknown";
            var controllerName = controller.GetType().Name;
            throw new InvalidOperationException($"Unable to get method info for action '{actionName}' in controller '{controllerName}'");
        }

        // 获取图表特性
        var chartAttribute = methodInfo.GetCustomAttribute<ChartAttribute>();
        var chartTitle = methodInfo.GetCustomAttribute<ChartTitleAttribute>()?.Title;
        
        // 如果没有设置 ChartTitle，尝试从 Display 或 DisplayName 特性获取
        if (string.IsNullOrEmpty(chartTitle))
        {
            chartTitle = methodInfo.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>()?.DisplayName
                ?? methodInfo.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Name;
        }
        
        var chartDescription = methodInfo.GetCustomAttribute<ChartDescriptionAttribute>()?.Description;

        // 获取图表类型
        var chartType = chartAttribute?.ChartType ?? "auto";
        logger?.LogInformation("使用图表类型: {ChartType}", chartType);

        // 如果是自动模式，则使用推荐服务
        if (chartType == "auto")
        {
            try
            {
                var recommendations = await chartService.RecommendChartTypesAsync(data);
                chartType = recommendations.FirstOrDefault() ?? "line";
                logger?.LogInformation("自动推荐图表类型: {ChartType}", chartType);
                
                // 将推荐的图表类型添加到选项中
                if (options == null)
                {
                    options = new Dictionary<string, object>();
                }
                
                if (options is IDictionary<string, object> optionsDict)
                {
                    optionsDict["recommendedChartType"] = chartType;
                    optionsDict["allRecommendations"] = recommendations.ToList();
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "图表类型推荐失败，使用默认图表类型");
                chartType = "line";
            }
        }

        // 获取数据映射特性
        var chartDataAttribute = methodInfo.GetCustomAttribute<ChartDataAttribute>();
        var dimensionField = methodInfo.GetCustomAttribute<DimensionFieldAttribute>()?.FieldName;
        var metricFields = methodInfo.GetCustomAttribute<MetricFieldsAttribute>()?.FieldNames;

        // 记录数据映射信息
        logger?.LogInformation("ChartDataAttribute信息: CategoryField={CategoryField}, ValueField={ValueField}, XField={XField}, YField={YField}",
            chartDataAttribute?.CategoryField,
            chartDataAttribute?.ValueField,
            chartDataAttribute?.XField,
            chartDataAttribute?.YField);

        // 准备选项
        var chartOptions = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(chartTitle))
            chartOptions["title"] = chartTitle;
        
        if (!string.IsNullOrEmpty(chartDescription))
            chartOptions["description"] = chartDescription;
        
        if (!string.IsNullOrEmpty(dimensionField))
            chartOptions["dimensionField"] = dimensionField;
        
        if (metricFields != null)
            chartOptions["metricFields"] = metricFields;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.CategoryField))
            chartOptions["categoryField"] = chartDataAttribute.CategoryField;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.ValueField))
            chartOptions["valueField"] = chartDataAttribute.ValueField;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.XField))
            chartOptions["xField"] = chartDataAttribute.XField;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.YField))
            chartOptions["yField"] = chartDataAttribute.YField;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.SeriesField))
            chartOptions["seriesField"] = chartDataAttribute.SeriesField;
        
        if (!string.IsNullOrEmpty(chartDataAttribute?.LabelField))
            chartOptions["labelField"] = chartDataAttribute.LabelField;
        
        if (options != null)
            chartOptions["userOptions"] = options;

        logger?.LogInformation("图表选项: {Options}", Newtonsoft.Json.JsonConvert.SerializeObject(chartOptions));

        // 创建图表配置
        var chartConfig = await chartService.CreateChartConfigAsync(chartType, data, chartOptions);
        logger?.LogInformation("生成的图表配置: {Config}", Newtonsoft.Json.JsonConvert.SerializeObject(chartConfig));

        // 返回结果
        return controller.Ok(chartConfig);
    }
}