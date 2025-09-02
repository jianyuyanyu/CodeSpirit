using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 图标列特性，用于将字段显示为图标
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IconColumnAttribute : Attribute
    {
        /// <summary>
        /// 图标厂商，默认为空字符串表示自定义图标
        /// </summary>
        public string Vendor { get; set; } = "";

        /// <summary>
        /// 图标大小，可选值：xs, sm, md, lg, xl, 2xl, 3xl, 4xl（对应Tailwind CSS的text-*类）
        /// </summary>
        public string Size { get; set; } = "lg";

        /// <summary>
        /// 图标颜色，可选值：primary, secondary, success, danger, warning, info, light, dark, muted（对应Bootstrap的text-*类）
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 是否支持旋转动画
        /// </summary>
        public bool Spin { get; set; } = false;

        /// <summary>
        /// 自定义 CSS 类名
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 当图标值为空时显示的默认图标
        /// </summary>
        public string DefaultIcon { get; set; }

        /// <summary>
        /// 是否在图标前显示文本
        /// </summary>
        public bool ShowText { get; set; } = false;

        /// <summary>
        /// 文本与图标的位置关系，可选值：left, right, top, bottom
        /// </summary>
        public string TextPosition { get; set; } = "right";

        /// <summary>
        /// 初始化一个新的 <see cref="IconColumnAttribute"/> 实例
        /// </summary>
        public IconColumnAttribute()
        {
        }

        /// <summary>
        /// 初始化一个新的 <see cref="IconColumnAttribute"/> 实例，并设置图标厂商
        /// </summary>
        /// <param name="vendor">图标厂商</param>
        public IconColumnAttribute(string vendor)
        {
            Vendor = vendor;
        }

        /// <summary>
        /// 初始化一个新的 <see cref="IconColumnAttribute"/> 实例，并设置图标厂商和大小
        /// </summary>
        /// <param name="vendor">图标厂商</param>
        /// <param name="size">图标大小</param>
        public IconColumnAttribute(string vendor, string size)
        {
            Vendor = vendor;
            Size = size;
        }
    }
}
