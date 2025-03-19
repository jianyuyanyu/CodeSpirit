using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 标签列特性，用于配置标签列的显示
    /// 基于each列封装，支持标签颜色、最大显示数量、超出数量显示模板等
    /// </summary>
    /// <example>
    /// 使用示例:
    /// <code>
    /// [DisplayName("标签")]
    /// [TagsColumn(Color = "info")]
    /// public List<string>? Tags { get; set; }
    /// 
    /// // 自定义配置
    /// [DisplayName("技能")]
    /// [TagsColumn(Color = "success", MaxTags = 3, OverflowTemplate = "+{overflow}更多")]
    /// public List<string>? Skills { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property)]
    public class TagsColumnAttribute : AmisColumnAttribute
    {
        /// <summary>
        /// 标签颜色，默认为info
        /// </summary>
        /// <remarks>
        /// 可选值: primary, success, info, warning, danger
        /// </remarks>
        public string Color { get; set; } = "info";

        /// <summary>
        /// 标签的CSS类，默认为label
        /// </summary>
        public string CssClass { get; set; } = "label";

        /// <summary>
        /// 最大显示标签数量，0表示不限制
        /// </summary>
        public int MaxTags { get; set; } = 0;

        /// <summary>
        /// 额外的CSS类，用于添加边距等
        /// </summary>
        public string ExtraClass { get; set; } = "mr-1";

        /// <summary>
        /// 超出最大数量时的文本模板，如"+{overflow}更多"
        /// </summary>
        public string OverflowTemplate { get; set; } = "+{overflow}";

        /// <summary>
        /// 是否显示空列表的占位符
        /// </summary>
        public bool ShowPlaceholder { get; set; } = true;

        /// <summary>
        /// 空列表的占位符文本
        /// </summary>
        public string Placeholder { get; set; } = "-";
    }
} 