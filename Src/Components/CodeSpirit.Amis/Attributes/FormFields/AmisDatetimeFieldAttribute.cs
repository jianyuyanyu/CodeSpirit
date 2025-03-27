namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的日期时间选择字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisDatetimeFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 日期时间显示格式
        /// </summary>
        public string DisplayFormat { get; set; } = "YYYY-MM-DD HH:mm:ss";

        /// <summary>
        /// 日期时间选择器格式
        /// </summary>
        public string PickerFormat { get; set; }

        /// <summary>
        /// 最小日期时间值，ISO 8601格式
        /// </summary>
        public string Min { get; set; }

        /// <summary>
        /// 最大日期时间值，ISO 8601格式
        /// </summary>
        public string Max { get; set; }

        /// <summary>
        /// 占位符文本
        /// </summary>
        public string InputPlaceholder { get; set; } = "请选择日期时间";

        /// <summary>
        /// 是否可清除
        /// </summary>
        public bool Clearable { get; set; } = true;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 是否使用当前时间作为默认值
        /// </summary>
        public bool UseCurrentTime { get; set; } = false;

        /// <summary>
        /// 日期分隔符
        /// </summary>
        public string DateSeparator { get; set; } = "-";

        /// <summary>
        /// 时间分隔符
        /// </summary>
        public string TimeSeparator { get; set; } = ":";

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
        /// UTC时间格式为 ISO 8601 格式的 UTC 字符串，如 2022-02-10T09:00:59Z
        /// </remarks>
        public bool Utc { get; set; } = false;
        
        /// <summary>
        /// 时区偏移，如 '+0800'。不设置时会根据用户浏览器自动检测
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// 初始化 AmisDatetimeFieldAttribute 实例。
        /// </summary>
        public AmisDatetimeFieldAttribute()
        {
            Type = "input-datetime";
        }

        /// <summary>
        /// 使用标签初始化 AmisDatetimeFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisDatetimeFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 