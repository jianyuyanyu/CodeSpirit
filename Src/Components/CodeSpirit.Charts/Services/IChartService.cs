using CodeSpirit.Charts.Models;
using CodeSpirit.Core.DependencyInjection;

namespace CodeSpirit.Charts.Services
{
    /// <summary>
    /// 图表服务接口
    /// </summary>
    public interface IChartService : IScopedDependency
    {
        /// <summary>
        /// 根据控制器方法生成图表配置
        /// </summary>
        /// <param name="methodInfo">控制器方法信息</param>
        /// <returns>图表配置</returns>
        Task<ChartConfig> GenerateChartConfigAsync(MethodInfo methodInfo);
        
        /// <summary>
        /// 根据数据自动推荐图表类型
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>推荐的图表类型</returns>
        Task<ChartType> RecommendChartTypeAsync(object data);
        
        /// <summary>
        /// 获取图表数据
        /// </summary>
        /// <param name="dataSource">数据源配置</param>
        /// <returns>图表数据</returns>
        Task<object> GetChartDataAsync(ChartDataSource dataSource);
        
        /// <summary>
        /// 导出图表为图片
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>图片字节数组</returns>
        Task<byte[]> ExportChartAsImageAsync(ChartConfig config);
        
        /// <summary>
        /// 导出图表数据为Excel
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>Excel字节数组</returns>
        Task<byte[]> ExportChartDataAsExcelAsync(ChartConfig config);
        
        /// <summary>
        /// 分析数据自动生成图表
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>图表配置</returns>
        Task<ChartConfig> AnalyzeAndGenerateChartAsync(object data);
        
        /// <summary>
        /// 保存图表配置
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>图表ID</returns>
        Task<string> SaveChartConfigAsync(ChartConfig config);
        
        /// <summary>
        /// 获取图表配置
        /// </summary>
        /// <param name="id">图表ID</param>
        /// <returns>图表配置</returns>
        Task<ChartConfig> GetChartConfigAsync(string id);
        
        /// <summary>
        /// 生成图表JSON配置
        /// </summary>
        /// <param name="config">图表配置</param>
        /// <returns>JSON格式的图表配置</returns>
        Task<JObject> GenerateChartJsonAsync(ChartConfig config);
        
        /// <summary>
        /// 获取推荐的多个图表类型
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="maxCount">最大推荐数量</param>
        /// <returns>图表类型及评分</returns>
        Task<Dictionary<ChartType, double>> GetRecommendedChartTypesAsync(object data, int maxCount = 3);
    }
} 