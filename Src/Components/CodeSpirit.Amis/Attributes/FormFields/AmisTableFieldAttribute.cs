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

        /// <summary>
        /// 是否显示序号列
        /// </summary>
        public bool ShowIndex { get; set; } = false;

        /// <summary>
        /// 是否启用新增模式
        /// </summary>
        public bool EditOnAdd { get; set; } = false;

        /// <summary>
        /// 是否启用确认模式
        /// </summary>
        public bool ConfirmMode { get; set; } = false;

        /// <summary>
        /// 每页显示多少条数据
        /// </summary>
        public int Perpage { get; set; }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool Editable { get; set; } = true;

    /// <summary>
    /// 是否可复制
    /// </summary>
    public bool Copyable { get; set; } = false;

    /// <summary>
    /// 条件显示表达式，支持表达式语法，如 "this.type === 1"
    /// </summary>
    public string VisibleOn { get; set; }

    /// <summary>
    /// 是否启用快速编辑功能
    /// </summary>
    public bool QuickEdit { get; set; } = false;
    }
} 