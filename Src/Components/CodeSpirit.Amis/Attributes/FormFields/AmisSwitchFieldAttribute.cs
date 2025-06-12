namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的开关字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisSwitchFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 开关打开时的文本
        /// </summary>
        public string OnText { get; set; }

        /// <summary>
        /// 开关关闭时的文本
        /// </summary>
        public string OffText { get; set; }

        /// <summary>
        /// 开关打开时的值
        /// </summary>
        public object TrueValue { get; set; } = true;

        /// <summary>
        /// 开关关闭时的值
        /// </summary>
        public object FalseValue { get; set; } = false;

        /// <summary>
        /// 开关的尺寸，可选值：sm、md、lg
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 初始化 AmisSwitchFieldAttribute 实例。
        /// </summary>
        public AmisSwitchFieldAttribute()
        {
            Type = "switch";
        }

        /// <summary>
        /// 使用标签初始化 AmisSwitchFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisSwitchFieldAttribute(string label) : this()
        {
            Label = label;
        }

        /// <summary>
        /// 使用默认值初始化 AmisSwitchFieldAttribute 实例。
        /// </summary>
        /// <param name="defaultValue">默认值</param>
        public AmisSwitchFieldAttribute(bool defaultValue) : this()
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// 使用标签和默认值初始化 AmisSwitchFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        /// <param name="defaultValue">默认值</param>
        public AmisSwitchFieldAttribute(string label, bool defaultValue) : this()
        {
            Label = label;
            DefaultValue = defaultValue;
        }
    }
} 