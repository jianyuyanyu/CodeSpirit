using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateColumnAttribute : Attribute
    {
        public string Format { get; set; }
        public string InputFormat { get; set; }
        public string Placeholder { get; set; }
        public int TimeZone { get; set; }

        /// <summary>
        /// 是否显示相对当前的时间描述，比如: 11 小时前、3 天前、1 年前等，fromNow 为 true 时，format 不生效。
        /// </summary>
        public bool FromNow { get; set; }
    }
}