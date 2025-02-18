namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于标注需要生成 AMIS InputTree 字段的属性或参数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisInputExcelFieldAttribute : AmisFormFieldAttribute
    {
        public bool CreateInputTable { get; set; }

        /// <summary>
        /// 初始化 <see cref="AmisInputTreeFieldAttribute"/> 的新实例。
        /// </summary>
        public AmisInputExcelFieldAttribute()
        {
            Type = "input-excel";
        }

    }
}

