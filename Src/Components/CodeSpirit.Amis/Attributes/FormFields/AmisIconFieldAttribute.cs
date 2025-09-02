namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的图标选择器字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AmisIconFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 图标库类型，支持 "fontawesome", "iconfont" 等
        /// </summary>
        public string IconType { get; set; } = "fontawesome";

        /// <summary>
        /// 是否允许搜索图标
        /// </summary>
        public bool Searchable { get; set; } = true;

        /// <summary>
        /// 是否允许清除图标
        /// </summary>
        public bool Clearable { get; set; } = true;

        /// <summary>
        /// 图标选择器的占位符文本
        /// </summary>
        public new string Placeholder { get; set; } = "请选择图标";

        /// <summary>
        /// 图标预览大小，支持 "sm", "md", "lg"
        /// </summary>
        public string PreviewSize { get; set; } = "md";

        /// <summary>
        /// 自定义图标列表数据源 URL
        /// 如果不设置，则使用默认的FontAwesome图标库
        /// </summary>
        public string IconSource { get; set; }

        /// <summary>
        /// 是否显示图标预览
        /// </summary>
        public bool ShowPreview { get; set; } = true;

        /// <summary>
        /// 初始化一个新的 <see cref="AmisIconFieldAttribute"/> 实例。
        /// </summary>
        public AmisIconFieldAttribute() : base("input-text")
        {
            // 图标选择器的具体实现由AmisIconFieldFactory处理
            // 这里只设置基础类型，不使用AdditionalConfig避免与工厂冲突
        }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisIconFieldAttribute"/> 实例，并设置图标类型。
        /// </summary>
        /// <param name="iconType">图标库类型</param>
        public AmisIconFieldAttribute(string iconType) : this()
        {
            IconType = iconType;
        }
    }
}
