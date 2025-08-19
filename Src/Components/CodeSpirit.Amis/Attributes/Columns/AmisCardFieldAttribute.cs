namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 卡片字段配置特性，用于配置属性在卡片模式中的显示方式
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AmisCardFieldAttribute : Attribute
    {
        /// <summary>
        /// 卡片字段类型
        /// </summary>
        public CardFieldType FieldType { get; set; } = CardFieldType.Body;

        /// <summary>
        /// 字段在卡片中的显示顺序
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 是否为头像字段（当FieldType为Avatar时使用）
        /// </summary>
        public bool IsAvatar { get; set; } = false;

        /// <summary>
        /// 是否为高亮字段（当FieldType为Highlight时使用）
        /// </summary>
        public bool IsHighlight { get; set; } = false;

        /// <summary>
        /// 模板内容（当FieldType为Body时可以使用自定义模板）
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// CSS类名
        /// </summary>
        public string ClassName { get; set; }
    }

    /// <summary>
    /// 卡片字段类型枚举
    /// </summary>
    public enum CardFieldType
    {
        /// <summary>
        /// 标题
        /// </summary>
        Title,

        /// <summary>
        /// 子标题
        /// </summary>
        SubTitle,

        /// <summary>
        /// 描述
        /// </summary>
        Description,

        /// <summary>
        /// 头像
        /// </summary>
        Avatar,

        /// <summary>
        /// 高亮
        /// </summary>
        Highlight,

        /// <summary>
        /// 主体内容
        /// </summary>
        Body
    }
}
