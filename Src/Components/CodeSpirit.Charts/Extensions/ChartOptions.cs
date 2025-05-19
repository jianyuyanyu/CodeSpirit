namespace CodeSpirit.Charts.Extensions
{
    /// <summary>
    /// 图表配置选项
    /// </summary>
    public class ChartOptions
    {
        /// <summary>
        /// 默认图表提供者
        /// </summary>
        public string DefaultProvider { get; set; } = "echarts";

        /// <summary>
        /// 默认主题
        /// </summary>
        public string DefaultTheme { get; set; } = "light";

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpiration { get; set; } = 30;

        /// <summary>
        /// 最大缓存大小（项）
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;
        
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
        /// 主题配置
        /// </summary>
        public Dictionary<string, object> ThemeConfigurations { get; set; } = new Dictionary<string, object>();
    }
}