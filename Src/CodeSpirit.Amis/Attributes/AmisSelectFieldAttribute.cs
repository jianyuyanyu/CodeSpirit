namespace CodeSpirit.Amis.Attributes
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的 select 类型字段的详细属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AmisSelectFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 数据源 URL，用于动态加载选项，仅适用于 "select" 类型字段。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 选项对象中的值字段名称，仅适用于 "select" 类型字段。
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// 选项对象中的标签字段名称，仅适用于 "select" 类型字段。
        /// </summary>
        public string LabelField { get; set; }

        /// <summary>
        /// 是否允许多选，仅适用于 "select" 类型字段。
        /// </summary>
        public bool Multiple { get; set; } = false;

        /// <summary>
        /// 是否将多个选中的值连接成一个字符串，仅适用于 "select" 类型字段。
        /// </summary>
        public bool JoinValues { get; set; } = true;

        /// <summary>
        /// 是否提取选中的值，仅适用于 "select" 类型字段。
        /// </summary>
        public bool ExtractValue { get; set; } = false;

        /// <summary>
        /// 是否启用搜索功能，仅适用于 "select" 类型字段。
        /// </summary>
        public bool Searchable { get; set; } = false;

        /// <summary>
        /// 是否允许清除选项，仅适用于 "select" 类型字段。
        /// </summary>
        public bool Clearable { get; set; } = false;

        /// <summary>
        /// 初始化一个新的 <see cref="AmisSelectFieldAttribute"/> 实例。
        /// </summary>
        public AmisSelectFieldAttribute() : base("select")
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisSelectFieldAttribute"/> 实例，并设置 select 类型字段的属性。
        /// </summary>
        /// <param name="source">数据源 URL。</param>
        /// <param name="valueField">选项对象中的值字段名称。</param>
        /// <param name="labelField">选项对象中的标签字段名称。</param>
        public AmisSelectFieldAttribute(string source, string valueField, string labelField)
            : base("select")
        {
            Source = source;
            ValueField = valueField;
            LabelField = labelField;
        }
    }
}

