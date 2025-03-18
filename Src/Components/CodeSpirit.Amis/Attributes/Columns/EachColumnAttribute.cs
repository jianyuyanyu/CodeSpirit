using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// AMIS each 列特性，用于循环渲染数组数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EachColumnAttribute : AmisColumnAttribute
    {
        /// <summary>
        /// 循环项变量名，默认为 item
        /// </summary>
        public string ItemVariable { get; set; } = "item";

        /// <summary>
        /// 索引变量名，默认为 index
        /// </summary>
        public string IndexVariable { get; set; } = "index";

        /// <summary>
        /// 列表项模板，用于渲染每一项
        /// </summary>
        public string ItemTemplate { get; set; }

        /// <summary>
        /// 获取数据的表达式，如果为空，则使用当前值
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 初始化 <see cref="EachColumnAttribute"/> 的新实例
        /// </summary>
        public EachColumnAttribute()
        {
            Type = "each";
        }
    }
} 