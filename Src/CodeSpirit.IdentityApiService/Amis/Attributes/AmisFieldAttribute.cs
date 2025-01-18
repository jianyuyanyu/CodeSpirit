namespace CodeSpirit.IdentityApi.Amis.Attributes
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单字段的通用属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AmisFieldAttribute : Attribute
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

        /// <summary>
        /// 自定义表单字段的其他配置，以 JSON 字符串形式提供。
        /// </summary>
        public string AdditionalConfig { get; set; }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisFieldAttribute"/> 实例。
        /// </summary>
        public AmisFieldAttribute()
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="AmisFieldAttribute"/> 实例，并设置字段类型。
        /// </summary>
        /// <param name="type">表单字段的类型。</param>
        public AmisFieldAttribute(string type)
        {
            Type = type;
        }
    }
}

