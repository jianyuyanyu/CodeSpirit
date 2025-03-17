namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 自定义特性，用于配置 AMIS 表单中的穿梭框字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AmisTransferFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 数据源 URL
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 选项值字段名
        /// </summary>
        public string ValueField { get; set; } = "value";

        /// <summary>
        /// 选项标签字段名
        /// </summary>
        public string LabelField { get; set; } = "label";

        /// <summary>
        /// 是否可搜索
        /// </summary>
        public bool Searchable { get; set; }

        /// <summary>
        /// 是否多选
        /// </summary>
        public bool Multiple { get; set; } = true;

        /// <summary>
        /// 是否将值用分隔符连接
        /// </summary>
        public bool JoinValues { get; set; } = true;

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// 是否显示统计
        /// </summary>
        public bool ShowStats { get; set; } = true;

        /// <summary>
        /// 左侧标题
        /// </summary>
        public string SourceLabel { get; set; } = "待选项";

        /// <summary>
        /// 右侧标题
        /// </summary>
        public string TargetLabel { get; set; } = "已选项";

        /// <summary>
        /// 可排序
        /// </summary>
        public bool Sortable { get; set; }

        /// <summary>
        /// 左侧搜索框提示
        /// </summary>
        public string SearchPlaceholder { get; set; } = "请搜索";

        /// <summary>
        /// 右侧搜索框提示
        /// </summary>
        public string ResultSearchPlaceholder { get; set; } = "请搜索";

        /// <summary>
        /// 左侧列表为空时显示的文本
        /// </summary>
        public string NoDataText { get; set; } = "暂无数据";

        /// <summary>
        /// 右侧列表为空时显示的文本
        /// </summary>
        public string ResultNoDataText { get; set; } = "暂无数据";

        /// <summary>
        /// 初始化 AmisTransferFieldAttribute 实例。
        /// </summary>
        public AmisTransferFieldAttribute()
        {
            Type = "transfer";
        }

        /// <summary>
        /// 使用标签初始化 AmisTransferFieldAttribute 实例。
        /// </summary>
        /// <param name="label">字段标签</param>
        public AmisTransferFieldAttribute(string label) : this()
        {
            Label = label;
        }
    }
} 