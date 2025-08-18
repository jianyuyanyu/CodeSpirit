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

        /// <summary>
        /// 是否将选中的值用分隔符拼接起来，仅在多选时有效。
        /// </summary>
        public bool JoinValues { get; set; }

        /// <summary>
        /// 是否提取选中的值，仅在多选时有效。
        /// </summary>
        public bool ExtractValue { get; set; }

        /// <summary>
        /// 懒加载接口，返回配置接口用来实现树懒加载。
        /// </summary>
        public string DeferApi { get; set; }

        /// <summary>
        /// 是否显示图标。
        /// </summary>
        public bool ShowIcon { get; set; }

        /// <summary>
        /// 展开指定层级的树节点，默认为0，即不自动展开。
        /// </summary>
        public int Expand { get; set; }

        /// <summary>
        /// 是否级联选择。选中父节点会自动选中子节点，默认为false。
        /// </summary>
        public bool Cascade { get; set; } = false;

        /// <summary>
        /// 当选中父节点时，是否自动选择子节点，默认为true。
        /// </summary>
        public bool AutoCheckChildren { get; set; } = true;

        /// <summary>
        /// 是否可搜索，默认为false。
        /// </summary>
        public bool Searchable { get; set; } = false;

        /// <summary>
        /// 是否显示树层级连接线，默认为false。
        /// </summary>
        public bool ShowOutline { get; set; } = false;

        /// <summary>
        /// 是否显示清除按钮，默认为false。
        /// </summary>
        public bool Clearable { get; set; } = false;

        /// <summary>
        /// 子节点字段名，默认为"children"。
        /// </summary>
        public string ChildrenField { get; set; } = "children";

        /// <summary>
        /// 懒加载子节点字段名。
        /// </summary>
        public string DeferField { get; set; }

        /// <summary>
        /// 是否只允许选择叶子节点，默认false
        /// </summary>
        public bool OnlyLeaf { get; set; } = false;

        /// <summary>
        /// 是否自动调整高度，默认为false
        /// </summary>
        public bool HeightAuto { get; set; } = false;

        /// <summary>
        /// 是否默认选择第一个选项，默认为false
        /// </summary>
        public bool SelectFirst { get; set; } = false;

        /// <summary>
        /// 是否只显示输入框不显示边框等样式，默认为false
        /// </summary>
        public bool InputOnly { get; set; } = false;

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

