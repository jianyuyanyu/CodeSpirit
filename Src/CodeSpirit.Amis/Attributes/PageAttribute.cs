namespace CodeSpirit.Amis.Attributes
{
    /// <summary>
    /// 自定义属性，用于标注控制器或操作方法的页面元数据。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PageAttribute : Attribute
    {
        /// <summary>
        /// 页面标签
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 页面 URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 重定向 URL
        /// </summary>
        public string Redirect { get; set; }

        /// <summary>
        /// 父级页面标签
        /// </summary>
        public string ParentLabel { get; set; }

        /// <summary>
        /// Schema API 的 URL
        /// </summary>
        public string SchemaApi { get; set; }

        /// <summary>
        /// Schema 内容（JSON 字符串）
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// 菜单图标，比如：fa fa-file
        /// </summary>
        public string Icon { get; set; }

        public PageAttribute() { }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="label">页面标签</param>
        public PageAttribute(string label)
        {
            Label = label;
        }

        public PageAttribute(string label, string ParentLabel, string Url, string SchemaApi, string icon) : this(label)
        {
            this.ParentLabel = ParentLabel;
            this.Url = Url;
            this.SchemaApi = SchemaApi;
            Schema = Schema;
            Icon = icon;
        }
    }
}