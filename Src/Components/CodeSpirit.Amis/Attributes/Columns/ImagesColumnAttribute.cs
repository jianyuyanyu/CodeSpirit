using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// AMIS 图片集列特性，用于展示多张图片轮播
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ImagesColumnAttribute : Attribute
    {
        /// <summary>
        /// 是否可放大显示
        /// </summary>
        public bool EnlargeAble { get; set; } = true;

        /// <summary>
        /// 是否显示图片工具栏
        /// </summary>
        public bool ShowToolbar { get; set; } = true;

        /// <summary>
        /// 缩略图模式，可选 'w-full' | 'h-full' | 'contain' | 'cover'
        /// </summary>
        public string ThumbMode { get; set; } = "contain";

        /// <summary>
        /// 缩略图比例，可选 '1:1' | '4:3' | '16:9'
        /// </summary>
        public string ThumbRatio { get; set; } = "1:1";

        /// <summary>
        /// 原图地址字段名，支持模板语法如 ${xxx}
        /// </summary>
        public string OriginalSrc { get; set; }

        /// <summary>
        /// 图片地址字段名，支持模板语法如 ${xxx}
        /// </summary>
        public string Src { get; set; }

        /// <summary>
        /// 分隔符，用于分割字符串类型的图片列表
        /// </summary>
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// 默认显示的图片数量
        /// </summary>
        public int ShowCount { get; set; } = 0;

        /// <summary>
        /// 自定义CSS类名
        /// </summary>
        public string ClassName { get; set; }
    }
}

