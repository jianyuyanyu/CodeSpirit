namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的文本域字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisTextareaFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 最小行数
        /// </summary>
        public int MinRows { get; set; }

        /// <summary>
        /// 最大行数
        /// </summary>
        public int MaxRows { get; set; }

        /// <summary>
        /// 是否清除首尾空格
        /// </summary>
        public bool Trim { get; set; }

        /// <summary>
        /// 是否显示计数器
        /// </summary>
        public bool ShowCounter { get; set; }

        /// <summary>
        /// 最大字符数
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// 是否可以调整大小
        /// </summary>
        public bool Resizable { get; set; }

        /// <summary>
        /// 初始化 AmisTextareaFieldAttribute 实例。
        /// </summary>
        public AmisTextareaFieldAttribute()
        {
            Type = "textarea";
        }

        /// <summary>
        /// 使用标签初始化 AmisTextareaFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisTextareaFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 