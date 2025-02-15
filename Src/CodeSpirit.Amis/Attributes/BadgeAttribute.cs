namespace CodeSpirit.Amis.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class BadgeAttribute : Attribute
    {
        /// <summary>
        /// 角标类型，可选 'dot' | 'text' | 'ribbon'
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// 角标文案
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 角标大小
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// 角标级别，可选 'info' | 'success' | 'warning' | 'danger'
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 封顶的数字值
        /// </summary>
        public int OverflowCount { get; set; }

        /// <summary>
        /// 角标位置，可选 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left'
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 角标位置偏移X
        /// </summary>
        public int OffsetX { get; set; }

        /// <summary>
        /// 角标位置偏移Y
        /// </summary>
        public int OffsetY { get; set; }

        /// <summary>
        /// 是否显示动画
        /// </summary>
        public bool Animation { get; set; }

        /// <summary>
        /// 控制角标显示的表达式
        /// </summary>
        public string VisibleOn { get; set; }

        /// <summary>
        /// 外层 dom 的类名
        /// </summary>
        public string ClassName {  get; set; }
    }
} 