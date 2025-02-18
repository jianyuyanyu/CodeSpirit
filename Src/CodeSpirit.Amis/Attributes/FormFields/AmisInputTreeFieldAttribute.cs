namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于标注需要生成 AMIS InputTree 字段的属性或参数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisInputTreeFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 数据源接口，用于加载树形数据。
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// 节点显示字段。
        /// </summary>
        public string LabelField { get; set; }

        /// <summary>
        /// 节点值字段。
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// 是否允许多选。
        /// </summary>
        public bool Multiple { get; set; }
        public bool JoinValues { get; set; }
        public bool ExtractValue { get; set; }

        /// <summary>
        /// 初始化 <see cref="AmisInputTreeFieldAttribute"/> 的新实例。
        /// </summary>
        public AmisInputTreeFieldAttribute()
        {
            // 默认类型为 'input-tree'
            Type = "input-tree";
        }
    }
}

