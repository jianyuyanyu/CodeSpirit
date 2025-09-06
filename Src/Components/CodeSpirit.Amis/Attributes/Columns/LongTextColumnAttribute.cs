using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    /// <summary>
    /// 长文本列特性，用于配置长文本字段的显示和弹窗行为
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LongTextColumnAttribute : Attribute
    {
        /// <summary>
        /// 是否启用点击弹窗（默认为悬停弹窗）
        /// </summary>
        public bool EnableClickPopOver { get; set; } = false;

        /// <summary>
        /// 弹窗触发方式：hover(悬停) 或 click(点击)
        /// </summary>
        public string Trigger { get; set; } = "hover";

        /// <summary>
        /// 是否显示点击图标（仅在EnableClickPopOver为true时有效）
        /// </summary>
        public bool ShowClickIcon { get; set; } = true;

        /// <summary>
        /// 点击图标的CSS类名
        /// </summary>
        public string ClickIconClass { get; set; } = "fa fa-info-circle text-info";

        /// <summary>
        /// 自定义显示长度（覆盖自动计算的长度）
        /// </summary>
        public int? CustomDisplayLength { get; set; }

        /// <summary>
        /// 自定义列宽（覆盖自动计算的宽度）
        /// </summary>
        public int? CustomWidth { get; set; }

        /// <summary>
        /// 弹窗模式：popOver 或 dialog
        /// </summary>
        public string PopOverMode { get; set; } = "popOver";

        /// <summary>
        /// 弹窗大小（仅在dialog模式下有效）
        /// </summary>
        public string DialogSize { get; set; } = "md";

        /// <summary>
        /// 初始化长文本列特性
        /// </summary>
        public LongTextColumnAttribute()
        {
        }

        /// <summary>
        /// 初始化长文本列特性并启用点击弹窗
        /// </summary>
        /// <param name="enableClickPopOver">是否启用点击弹窗</param>
        public LongTextColumnAttribute(bool enableClickPopOver)
        {
            EnableClickPopOver = enableClickPopOver;
            Trigger = enableClickPopOver ? "click" : "hover";
        }

        /// <summary>
        /// 初始化长文本列特性并指定触发方式
        /// </summary>
        /// <param name="trigger">触发方式：hover 或 click</param>
        public LongTextColumnAttribute(string trigger)
        {
            Trigger = trigger;
            EnableClickPopOver = trigger == "click";
        }
    }
}
