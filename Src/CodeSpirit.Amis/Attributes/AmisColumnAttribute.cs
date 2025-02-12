namespace CodeSpirit.Amis.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AmisColumnAttribute : Attribute
    {
        public string Label { get; set; } // 列标题
        public bool Sortable { get; set; } // 是否支持排序
        public string Type { get; set; } // 数据类型，例如：字符串、日期、数字等
        public bool QuickEdit { get; set; } // 是否可编辑

        /// <summary>
        /// 通过名称关联数据
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 是否可复制
        /// </summary>
        public bool Copyable { get; set; }

        /// <summary>
        /// 是否固定当前列（left | right | none）
        /// </summary>
        public string Fixed { get; set; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// 背景色阶最小值
        /// </summary>
        public double BackgroundScaleMin { get; set; }

        /// <summary>
        /// 背景色阶最大值
        /// </summary>
        public double BackgroundScaleMax { get; set; }

        /// <summary>
        /// 背景色阶颜色数组（至少包含两个颜色值）
        /// </summary>
        public string[] BackgroundScaleColors { get; set; }
    }
}
