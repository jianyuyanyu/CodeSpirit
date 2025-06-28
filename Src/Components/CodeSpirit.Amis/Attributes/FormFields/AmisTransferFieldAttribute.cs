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
        /// 懒加载接口，返回配置接口用来实现懒加载
        /// </summary>
        public string DeferApi { get; set; }

        /// <summary>
        /// 选择模式，支持：list（列表模式）、table（表格模式）、tree（树形模式）、chained（级联模式）、associated（关联模式）
        /// </summary>
        public string SelectMode { get; set; } = "list";

        /// <summary>
        /// 结果列表的模式是否跟随选择模式，当设置为 true 时，结果区域会使用与左侧选择区域相同的展示模式
        /// </summary>
        public bool ResultListModeFollowSelect { get; set; } = false;

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
        /// 是否提取选中的值。当为 false 时，选中的是整个选项对象；当为 true 时，选中的是选项对象的值
        /// </summary>
        public bool ExtractValue { get; set; } = false;

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
        /// 是否可清除
        /// </summary>
        public bool Clearable { get; set; } = true;

        /// <summary>
        /// 是否显示删除按钮
        /// </summary>
        public bool ShowDeleteBtn { get; set; } = true;

        /// <summary>
        /// 是否显示全选按钮
        /// </summary>
        public bool ShowSelectAll { get; set; } = true;

        /// <summary>
        /// 每页显示多少条数据，默认不分页
        /// </summary>
        public int PerPage { get; set; } = 0;

        /// <summary>
        /// 当前页数
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 是否显示分页
        /// </summary>
        public bool ShowPagination { get; set; } = false;

        /// <summary>
        /// 自定义搜索函数，当开启搜索的时候，用来自定义搜索函数
        /// </summary>
        public string SearchApi { get; set; }

        /// <summary>
        /// 是否开启结果区域的搜索
        /// </summary>
        public bool ResultSearchable { get; set; } = false;

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