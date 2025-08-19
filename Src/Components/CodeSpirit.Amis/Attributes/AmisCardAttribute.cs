namespace CodeSpirit.Amis.Attributes
{
    /// <summary>
    /// 卡片模式配置特性，用于标记控制器支持卡片模式显示
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AmisCardAttribute : Attribute
    {
        /// <summary>
        /// 每页显示卡片数量，默认为6
        /// </summary>
        public int DefaultPerPage { get; set; } = 6;

        /// <summary>
        /// 是否允许切换每页显示数量，默认为false
        /// </summary>
        public bool SwitchPerPage { get; set; } = false;

        /// <summary>
        /// 空数据时的提示文本
        /// </summary>
        public string Placeholder { get; set; } = "暂无数据";

        /// <summary>
        /// 每行显示卡片数量，默认为2
        /// </summary>
        public int ColumnsCount { get; set; } = 2;

        /// <summary>
        /// 卡片头部标题字段
        /// </summary>
        public string TitleField { get; set; }

        /// <summary>
        /// 卡片头部子标题字段
        /// </summary>
        public string SubTitleField { get; set; }

        /// <summary>
        /// 卡片头部描述字段
        /// </summary>
        public string DescriptionField { get; set; }

        /// <summary>
        /// 卡片头部头像字段
        /// </summary>
        public string AvatarField { get; set; }

        /// <summary>
        /// 卡片高亮字段
        /// </summary>
        public string HighlightField { get; set; }

        /// <summary>
        /// 头部CSS类名
        /// </summary>
        public string HeaderClassName { get; set; } = "bg-white";

        /// <summary>
        /// 头像CSS类名
        /// </summary>
        public string AvatarClassName { get; set; } = "pull-left thumb-md avatar b-3x m-r";

        /// <summary>
        /// 卡片主体CSS类名
        /// </summary>
        public string BodyClassName { get; set; } = "padder";

        /// <summary>
        /// 卡片主体内容模板
        /// </summary>
        public string BodyTemplate { get; set; }
    }
}
