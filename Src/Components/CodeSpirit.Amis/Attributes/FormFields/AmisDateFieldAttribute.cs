namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的日期选择字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisDateFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 日期显示格式
        /// </summary>
        public string DisplayFormat { get; set; } = "YYYY-MM-DD";

        /// <summary>
        /// 日期选择器格式
        /// </summary>
        public string PickerFormat { get; set; }

        /// <summary>
        /// 最小日期值，ISO 8601格式
        /// </summary>
        public string Min { get; set; }

        /// <summary>
        /// 最大日期值，ISO 8601格式
        /// </summary>
        public string Max { get; set; }

        /// <summary>
        /// 占位符文本
        /// </summary>
        public string InputPlaceholder { get; set; } = "请选择日期";

        /// <summary>
        /// 是否可清除
        /// </summary>
        public bool Clearable { get; set; } = true;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 是否使用当前日期作为默认值
        /// </summary>
        public bool UseCurrentDate { get; set; } = false;

        /// <summary>
        /// 日期分隔符
        /// </summary>
        public string DateSeparator { get; set; } = "-";

        /// <summary>
        /// 是否显示清除按钮
        /// </summary>
        public bool ShowClearBtn { get; set; } = true;

        /// <summary>
        /// 是否显示日历图标
        /// </summary>
        public bool ShowIcon { get; set; } = true;
        
        /// <summary>
        /// 是否使用UTC时间
        /// </summary>
        /// <remarks>
        /// 设置为true时，表单提交值为UTC时间。
        /// UTC时间格式为 ISO 8601 格式的 UTC 字符串，如 2022-02-10
        /// </remarks>
        public bool Utc { get; set; } = false;
        
        /// <summary>
        /// 时区偏移，如 '+0800'。不设置时会根据用户浏览器自动检测
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// 初始化 AmisDateFieldAttribute 实例。
        /// </summary>
        public AmisDateFieldAttribute()
        {
            Type = "input-date";
        }

        /// <summary>
        /// 使用标签初始化 AmisDateFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisDateFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 