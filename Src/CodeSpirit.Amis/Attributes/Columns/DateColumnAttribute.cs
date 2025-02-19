namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 日期列配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateColumnAttribute : Attribute
    {
        /// <summary>
        /// 展示格式，参考 moment 中的格式说明。如：YYYY-MM-DD HH:mm:ss
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 指定日期数据的格式，默认为 ISO 8601 格式。如：YYYY-MM-DD
        /// </summary>
        public string InputFormat { get; set; }

        /// <summary>
        /// 占位符
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// 是否显示相对当前的时间，比如：11 小时前、3 天前、1 年前等
        /// </summary>
        public bool FromNow { get; set; }
    }
}