using CodeSpirit.Charts.Models;

namespace CodeSpirit.Charts.Services
{
    /// <summary>
    /// ECharts配置生成器接口
    /// </summary>
    public interface IEChartConfigGenerator
    {
        /// <summary>
        /// 将ChartConfig转换为ECharts配置对象
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>ECharts配置对象</returns>
        object GenerateEChartConfig(ChartConfig config);
        
        /// <summary>
        /// 将ChartConfig转换为ECharts配置JSON字符串
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>ECharts配置JSON字符串</returns>
        string GenerateEChartConfigJson(ChartConfig config);
        
        /// <summary>
        /// 将图表配置和数据一起转换为完整的ECharts配置对象
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <param name="data">图表数据</param>
        /// <returns>ECharts配置对象</returns>
        object GenerateCompleteEChartConfig(ChartConfig config, object data);
    }
} 