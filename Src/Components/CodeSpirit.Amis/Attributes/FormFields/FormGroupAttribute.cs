using System.ComponentModel;

namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 表单项组特性，用于将多个表单字段组织成一个组
    /// <para>支持在DTO类型上使用，可以将相关的字段分组显示</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class FormGroupAttribute : Attribute
    {
        /// <summary>
        /// 组标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 组描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 组名称，用于标识组
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 包含的字段名称列表，用逗号分隔
        /// </summary>
        public string Fields { get; set; }

        /// <summary>
        /// 组的显示模式
        /// </summary>
        public FormGroupMode Mode { get; set; } = FormGroupMode.Normal;

        /// <summary>
        /// 组的间距大小
        /// </summary>
        public FormGroupGap Gap { get; set; } = FormGroupGap.Normal;

        /// <summary>
        /// 组的方向
        /// </summary>
        public FormGroupDirection Direction { get; set; } = FormGroupDirection.Vertical;

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool ShowBorder { get; set; } = false;

        /// <summary>
        /// 自定义CSS类名
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 显示条件表达式
        /// </summary>
        public string VisibleOn { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool Hidden { get; set; } = false;

        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// 禁用条件表达式
        /// </summary>
        public string DisabledOn { get; set; }

        /// <summary>
        /// 组的排序权重，数值越小越靠前
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 自定义配置，以JSON字符串形式提供
        /// </summary>
        public string AdditionalConfig { get; set; }

        /// <summary>
        /// 初始化表单项组特性
        /// </summary>
        public FormGroupAttribute()
        {
        }

        /// <summary>
        /// 初始化表单项组特性，并设置组名称和标题
        /// </summary>
        /// <param name="name">组名称</param>
        /// <param name="title">组标题</param>
        public FormGroupAttribute(string name, string title = null)
        {
            Name = name;
            Title = title ?? name;
        }

        /// <summary>
        /// 初始化表单项组特性，并设置组名称、标题和包含的字段
        /// </summary>
        /// <param name="name">组名称</param>
        /// <param name="title">组标题</param>
        /// <param name="fields">包含的字段名称，用逗号分隔</param>
        public FormGroupAttribute(string name, string title, string fields) : this(name, title)
        {
            Fields = fields;
        }
    }

    /// <summary>
    /// 表单项组显示模式
    /// </summary>
    public enum FormGroupMode
    {
        /// <summary>
        /// 普通模式
        /// </summary>
        [Description("普通模式")]
        Normal,

        /// <summary>
        /// 内联模式
        /// </summary>
        [Description("内联模式")]
        Inline,

        /// <summary>
        /// 水平模式
        /// </summary>
        [Description("水平模式")]
        Horizontal
    }

    /// <summary>
    /// 表单项组间距大小
    /// </summary>
    public enum FormGroupGap
    {
        /// <summary>
        /// 无间距
        /// </summary>
        [Description("无间距")]
        None,

        /// <summary>
        /// 小间距
        /// </summary>
        [Description("小间距")]
        Small,

        /// <summary>
        /// 普通间距
        /// </summary>
        [Description("普通间距")]
        Normal,

        /// <summary>
        /// 大间距
        /// </summary>
        [Description("大间距")]
        Large
    }

    /// <summary>
    /// 表单项组方向
    /// </summary>
    public enum FormGroupDirection
    {
        /// <summary>
        /// 垂直方向
        /// </summary>
        [Description("垂直方向")]
        Vertical,

        /// <summary>
        /// 水平方向
        /// </summary>
        [Description("水平方向")]
        Horizontal
    }
}
