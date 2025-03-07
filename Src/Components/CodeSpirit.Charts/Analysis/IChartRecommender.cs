using CodeSpirit.Charts.Models;

namespace CodeSpirit.Charts.Analysis
{
    /// <summary>
    /// 图表推荐器接口
    /// </summary>
    public interface IChartRecommender
    {
        /// <summary>
        /// 推荐最适合的图表类型
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>推荐的图表类型</returns>
        ChartType RecommendChartType(object data);
        
        /// <summary>
        /// 生成完整的图表配置
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="preferredType">首选图表类型（如果为null则自动推荐）</param>
        /// <returns>图表配置</returns>
        ChartConfig GenerateChartConfig(object data, ChartType? preferredType = null);
        
        /// <summary>
        /// 推荐多个适合的图表类型及评分
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="maxCount">最大推荐数量</param>
        /// <returns>图表类型及评分</returns>
        Dictionary<ChartType, double> RecommendChartTypes(object data, int maxCount = 3);
        
        /// <summary>
        /// 根据数据分析结果优化图表配置
        /// </summary>
        /// <param name="config">初始图表配置</param>
        /// <param name="data">数据对象</param>
        /// <returns>优化后的图表配置</returns>
        ChartConfig OptimizeChartConfig(ChartConfig config, object data);
    }
} 