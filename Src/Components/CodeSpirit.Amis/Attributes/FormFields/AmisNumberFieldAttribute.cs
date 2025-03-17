namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的数字输入字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisNumberFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// 步长
        /// </summary>
        public double Step { get; set; } = 1;

        /// <summary>
        /// 精度（小数位数）
        /// </summary>
        public int Precision { get; set; } = 0;

        /// <summary>
        /// 是否显示步进器
        /// </summary>
        public bool ShowSteps { get; set; } = true;

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 是否大数字显示，开启后会把大数字转成万、亿等单位
        /// </summary>
        public bool BigNumber { get; set; }

        /// <summary>
        /// 是否显示千分分隔符
        /// </summary>
        public bool Kilobitwise { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 后缀
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 是否是金额
        /// </summary>
        public bool IsCurrency { get; set; }

        /// <summary>
        /// 键盘行为
        /// </summary>
        public string KeyboardMode { get; set; } = "default";

        /// <summary>
        /// 初始化 AmisNumberFieldAttribute 实例。
        /// </summary>
        public AmisNumberFieldAttribute()
        {
            Type = "input-number";
        }

        /// <summary>
        /// 使用标签初始化 AmisNumberFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisNumberFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 