namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的时间选择字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisTimeFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 时间显示格式
        /// </summary>
        public string DisplayFormat { get; set; } = "HH:mm:ss";

        /// <summary>
        /// 最小时间值，格式如 "00:00:00"
        /// </summary>
        public string Min { get; set; }

        /// <summary>
        /// 最大时间值，格式如 "23:59:59"
        /// </summary>
        public string Max { get; set; }

        /// <summary>
        /// 占位符文本
        /// </summary>
        public string InputPlaceholder { get; set; } = "请选择时间";

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
        /// 时间分隔符
        /// </summary>
        public string TimeSeparator { get; set; } = ":";

        /// <summary>
        /// 是否显示秒选择
        /// </summary>
        public bool ShowSeconds { get; set; } = true;

        /// <summary>
        /// 步长（分钟）
        /// </summary>
        public int Step { get; set; } = 1;

        /// <summary>
        /// 是否显示清除按钮
        /// </summary>
        public bool ShowClearBtn { get; set; } = true;

        /// <summary>
        /// 是否显示图标
        /// </summary>
        public bool ShowIcon { get; set; } = true;
        
        /// <summary>
        /// 是否使用UTC时间
        /// </summary>
        /// <remarks>
        /// 设置为true时，表单提交值为UTC时间。
        /// 注：时间组件通常不涉及日期，UTC主要影响跨日期时区转换
        /// </remarks>
        public bool Utc { get; set; } = false;
        
        /// <summary>
        /// 时区偏移，如 '+0800'。不设置时会根据用户浏览器自动检测
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// 初始化 AmisTimeFieldAttribute 实例。
        /// </summary>
        public AmisTimeFieldAttribute()
        {
            Type = "input-time";
        }

        /// <summary>
        /// 使用标签初始化 AmisTimeFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisTimeFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 