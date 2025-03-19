namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的数组输入字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisArrayFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 数组元素的配置
        /// </summary>
        public string Items { get; set; }

        /// <summary>
        /// 是否可新增
        /// </summary>
        public bool Addable { get; set; } = true;

        /// <summary>
        /// 是否可删除
        /// </summary>
        public bool Removable { get; set; } = true;

        /// <summary>
        /// 是否可拖拽排序
        /// </summary>
        public bool Draggable { get; set; }

        /// <summary>
        /// 最少个数
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// 最多个数
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// 新增按钮文字
        /// </summary>
        public string AddButtonText { get; set; } = "新增";

        /// <summary>
        /// 删除按钮文字
        /// </summary>
        public string DeleteButtonText { get; set; } = "删除";

        /// <summary>
        /// 是否显示序号
        /// </summary>
        public bool ShowIndex { get; set; }

        /// <summary>
        /// 序号列说明
        /// </summary>
        public string IndexLabel { get; set; } = "序号";

        /// <summary>
        /// 是否可复制并新增
        /// </summary>
        public bool Copyable { get; set; }

        /// <summary>
        /// 复制按钮文字
        /// </summary>
        public string CopyButtonText { get; set; } = "复制并新增";

        /// <summary>
        /// 是否可以访问父级数据，正常情况下，数组内的表单项值变化后会让外层的表单项更新（具体表现为从顶层到该表单项的 path 中间的所有 数组 和 对象 的值都会更新）。
        /// </summary>
        public bool SyncFields { get; set; }

        /// <summary>
        /// 是否开启条件分支
        /// </summary>
        public bool Conditions { get; set; }

        /// <summary>
        /// 数组输入框的尺寸，支持 xs、sm、md、lg、full
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 初始化 AmisArrayFieldAttribute 实例。
        /// </summary>
        public AmisArrayFieldAttribute()
        {
            Type = "input-array";
        }

        /// <summary>
        /// 使用标签初始化 AmisArrayFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisArrayFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
}