using System;

namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// AMIS input-table 表格编辑组件特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AmisTableFieldAttribute : Attribute
    {
        /// <summary>
        /// 是否可新增
        /// </summary>
        public bool Addable { get; set; } = true;

        /// <summary>
        /// 是否可删除
        /// </summary>
        public bool Removable { get; set; } = true;

        /// <summary>
        /// 是否可拖拽排序
        /// </summary>
        public bool Draggable { get; set; } = true;

        /// <summary>
        /// 显示新增按钮文字
        /// </summary>
        public string AddButtonText { get; set; } = "新增";
    }
} 