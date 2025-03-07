namespace CodeSpirit.Charts.Attributes
{
    /// <summary>
    /// 图表特性，用于标记控制器方法生成图表
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class ChartAttribute : Attribute
    {
        /// <summary>
        /// 图表标题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 图表描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否启用自动分析
        /// </summary>
        public bool EnableAutoAnalysis { get; set; } = true;
        
        /// <summary>
        /// 是否启用自动刷新
        /// </summary>
        public bool AutoRefresh { get; set; }
        
        /// <summary>
        /// 自动刷新间隔（秒）
        /// </summary>
        public int RefreshInterval { get; set; } = 60;
        
        /// <summary>
        /// 是否显示工具箱
        /// </summary>
        public bool ShowToolbox { get; set; } = true;
        
        /// <summary>
        /// 图表主题
        /// </summary>
        public string Theme { get; set; } = "default";
        
        /// <summary>
        /// 图表高度
        /// </summary>
        public int Height { get; set; } = 400;
        
        /// <summary>
        /// 图表宽度
        /// </summary>
        public int Width { get; set; } = 0; // 0表示自适应
        
        /// <summary>
        /// 是否启用交互
        /// </summary>
        public bool EnableInteraction { get; set; } = true;
        
        /// <summary>
        /// 是否支持导出
        /// </summary>
        public bool EnableExport { get; set; } = true;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartAttribute()
        {
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">图表标题</param>
        public ChartAttribute(string title)
        {
            Title = title;
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="title">图表标题</param>
        /// <param name="description">图表描述</param>
        public ChartAttribute(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
} 