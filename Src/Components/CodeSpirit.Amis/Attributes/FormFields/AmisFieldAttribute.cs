namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单字段的通用属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisFormFieldAttribute : Attribute
    {
        /// <summary>
        /// 表单字段的类型，例如 "input-text", "switch", "datetime", "image", "avatar" 等。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 表单字段的标签文本。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 是否为必填字段。
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// 表单字段的占位符文本。
        /// </summary>
        public string Placeholder { get; set; }

        public string VisibleOn { get; set; }

        public bool Hidden { get; set; }

        /// <summary>
        /// 字段的值（已弃用，请使用 DefaultValue 或 DefaultValueExpression）
        /// </summary>
        [Obsolete("请使用 DefaultValue 或 DefaultValueExpression 属性替代")]
        public string Value { get; set; }

        /// <summary>
        /// 字段的默认值，支持多种数据类型
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// 字段的默认值表达式，支持 ${xxx} 语法
        /// </summary>
        public string DefaultValueExpression { get; set; }

        /// <summary>
        /// 默认值的类型
        /// </summary>
        public DefaultValueType ValueType { get; set; } = DefaultValueType.Static;

        /// <summary>
        /// 自定义表单字段的其他配置，以 JSON 字符串形式提供。
        /// </summary>
        public string AdditionalConfig { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// 是否为静态模式
        /// </summary>
        public bool Static { get; set; }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisFormFieldAttribute"/> 实例。
        /// </summary>
        public AmisFormFieldAttribute()
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisFormFieldAttribute"/> 实例，并设置字段类型。
        /// </summary>
        /// <param name="type">表单字段的类型。</param>
        public AmisFormFieldAttribute(string type)
        {
            Type = type;
        }
    }
}
