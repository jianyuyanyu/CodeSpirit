using System;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Amis.Attributes.FormFields
{
    /// <summary>
    /// 树形选择器字段特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AmisTreeSelectFieldAttribute : AmisFormFieldAttribute
    {
        /// <summary>
        /// 数据源
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// 选项标签字段
        /// </summary>
        public string LabelField { get; set; } = "label";

        /// <summary>
        /// 选项值字段
        /// </summary>
        public string ValueField { get; set; } = "value";

        /// <summary>
        /// 是否多选
        /// </summary>
        public bool Multiple { get; set; } = false;

        /// <summary>
        /// 拼接值
        /// </summary>
        public bool JoinValues { get; set; } = true;

        /// <summary>
        /// 提取值
        /// </summary>
        public bool ExtractValue { get; set; } = false;

        /// <summary>
        /// 级联选择
        /// </summary>
        public bool Cascade { get; set; } = false;

        /// <summary>
        /// 是否可搜索
        /// </summary>
        public bool Searchable { get; set; } = false;

        /// <summary>
        /// 子节点字段名
        /// </summary>
        public string DeferField { get; set; }

        /// <summary>
        /// 是否显示删除图标
        /// </summary>
        public bool Clearable { get; set; } = false;

        /// <summary>
        /// 是否显示展开图标
        /// </summary>
        public bool ShowIcon { get; set; } = true;

        /// <summary>
        /// 是否显示分支连接线
        /// </summary>
        public bool ShowOutline { get; set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="label">标签</param>
        /// <param name="dataSource">数据源</param>
        public AmisTreeSelectFieldAttribute() : base("tree-select")
        {
        }
    }
} 