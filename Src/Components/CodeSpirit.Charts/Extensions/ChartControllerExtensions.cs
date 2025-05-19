using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Charts.Extensions;

/// <summary>
/// 控制器图表扩展类
/// </summary>
public static class ChartControllerExtensions
{
    /// <summary>
    /// 获取图表服务
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <returns>图表服务</returns>
    public static IChartService GetChartService(this ControllerBase controller)
    {
        return controller.HttpContext.RequestServices.GetRequiredService<IChartService>();
    }

    /// <summary>
    /// 创建图表配置
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>图表配置</returns>
    public static async Task<object> CreateChartConfigAsync(
        this ControllerBase controller,
        string chartType,
        object data,
        object? options = null)
    {
        var chartService = controller.GetChartService();
        return await chartService.CreateChartConfigAsync(chartType, data, options);
    }

    /// <summary>
    /// 创建图表配置
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="chartType">图表类型</param>
    /// <param name="data">图表数据</param>
    /// <param name="options">配置选项</param>
    /// <returns>图表配置</returns>
    public static async Task<object> CreateChartConfigAsync(
        this ControllerBase controller,
        string providerName,
        string chartType,
        object data,
        object? options = null)
    {
        var chartService = controller.GetChartService();
        return await chartService.CreateChartConfigAsync(providerName, chartType, data, options);
    }

    /// <summary>
    /// 获取 Amis 图表配置
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="options">Amis 配置选项</param>
    /// <returns>Amis 配置</returns>
    public static async Task<object> GetAmisChartConfigAsync(
        this ControllerBase controller,
        object chartConfig,
        string providerName,
        object? options = null)
    {
        var chartService = controller.GetChartService();
        return await chartService.GetAmisConfigAsync(chartConfig, providerName, options);
    }

    /// <summary>
    /// 推荐图表类型
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="data">数据</param>
    /// <param name="providerName">提供者名称</param>
    /// <returns>推荐的图表类型列表</returns>
    public static async Task<IEnumerable<string>> RecommendChartTypesAsync(
        this ControllerBase controller,
        object data,
        string? providerName = null)
    {
        var chartService = controller.GetChartService();
        return await chartService.RecommendChartTypesAsync(data, providerName);
    }

    /// <summary>
    /// 导出图表数据
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="format">导出格式</param>
    /// <param name="options">导出选项</param>
    /// <returns>文件结果</returns>
    public static async Task<FileResult> ExportChartDataAsync(
        this ControllerBase controller,
        object chartConfig,
        string format,
        object? options = null)
    {
        var chartService = controller.GetChartService();
        var data = await chartService.ExportChartDataAsync(chartConfig, format, options);

        string contentType = format.ToLowerInvariant() switch
        {
            "csv" => "text/csv",
            "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "json" => "application/json",
            _ => "application/octet-stream"
        };

        string fileName = $"chart-export-{DateTime.Now:yyyyMMddHHmmss}.{format.ToLowerInvariant()}";

        return controller.File(data, contentType, fileName);
    }

    /// <summary>
    /// 导出图表图片
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="chartConfig">图表配置</param>
    /// <param name="providerName">提供者名称</param>
    /// <param name="options">导出选项</param>
    /// <returns>文件结果</returns>
    public static async Task<FileResult> ExportChartImageAsync(
        this ControllerBase controller,
        object chartConfig,
        string providerName,
        object? options = null)
    {
        var chartService = controller.GetChartService();
        var data = await chartService.ExportChartImageAsync(chartConfig, providerName, options);

        string fileName = $"chart-image-{DateTime.Now:yyyyMMddHHmmss}.png";

        return controller.File(data, "image/png", fileName);
    }

    /// <summary>
    /// 返回图表结果
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="config">图表配置</param>
    /// <param name="data">数据</param>
    /// <returns>Action结果</returns>
    public static IActionResult ChartResult(this ControllerBase controller, ChartConfig config, object data)
    {
        var echartGenerator = controller.HttpContext.RequestServices.GetRequiredService<IEChartConfigGenerator>();
        var result = echartGenerator.GenerateCompleteEChartConfig(config, data);
        return controller.Ok(result);
    }

    /// <summary>
    /// 获取图表推荐
    /// </summary>
    /// <param name="controller">控制器</param>
    /// <param name="data">数据</param>
    /// <param name="maxCount">最大数量</param>
    /// <returns>Action结果</returns>
    public static async Task<IActionResult> ChartRecommendations(this ControllerBase controller, object data, int maxCount = 3)
    {
        var recommender = controller.HttpContext.RequestServices.GetRequiredService<IChartRecommender>();
        var results = await recommender.RecommendChartTypesAsync(data, "echarts");
        var items = results.Take(maxCount);
        return controller.Ok(new { recommendations = items });
    }
}