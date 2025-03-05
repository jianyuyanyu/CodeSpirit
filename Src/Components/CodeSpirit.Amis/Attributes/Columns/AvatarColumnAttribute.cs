using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AvatarColumnAttribute : Attribute
    {
        /// <summary>
        /// 文字
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 图片拉伸方式，可选 'contain' | 'cover' | 'fill' | 'none' | 'scale-down'
        /// </summary>
        public string Fit { get; set; }

        /// <summary>
        /// 形状，可选 'circle' | 'square' | 'rounded'
        /// </summary>
        public string Shape { get; set; }

        /// <summary>
        /// 大小，可选 'default' | 'normal' | 'small' 或数字
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 字符类型距离左右两侧边界单位像素
        /// </summary>
        public int? Gap { get; set; }

        /// <summary>
        /// 图片加载失败后，通过 onError 控制是否进行 text、icon 置换
        /// </summary>
        public string OnError { get; set; } = "return true;";
    }
}