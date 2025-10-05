namespace CodeSpirit.Amis.Attributes.Columns
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

        /// <summary>
        /// 默认是否显示
        /// </summary>
        public bool Toggled { get; set; } = true;
        public bool Disabled { get; set; }

        /// <summary>
        /// 状态映射配置（用于 status 类型列）
        /// 支持预定义的状态映射或自定义映射
        /// </summary>
        public StatusMapping StatusMapping { get; set; } = StatusMapping.None;

        /// <summary>
        /// 自定义状态映射配置（JSON格式）
        /// 例如：{"1": "success", "0": "fail", "pending": "info"}
        /// </summary>
        public string CustomStatusMap { get; set; }

        /// <summary>
        /// 状态标签文本映射（JSON格式）
        /// 例如：{"success": "成功", "fail": "失败", "info": "处理中"}
        /// </summary>
        public string StatusLabelMap { get; set; }

        /// <summary>
        /// 是否显示状态图标
        /// </summary>
        public bool ShowStatusIcon { get; set; } = true;

        /// <summary>
        /// 状态列的占位符文本
        /// </summary>
        public string StatusPlaceholder { get; set; } = "-";
    }

    /// <summary>
    /// 预定义的状态映射类型
    /// </summary>
    public enum StatusMapping
    {
        /// <summary>
        /// 无映射
        /// </summary>
        None,

        /// <summary>
        /// HTTP状态码映射
        /// 2xx -> success (绿色)
        /// 3xx -> info (蓝色) 
        /// 4xx -> warning (橙色)
        /// 5xx -> danger (红色)
        /// </summary>
        HttpStatusCode,

        /// <summary>
        /// 布尔值映射
        /// true -> success (成功)
        /// false -> fail (失败)
        /// </summary>
        Boolean,

        /// <summary>
        /// 审计操作类型映射
        /// Create -> success (创建)
        /// Update -> info (更新)
        /// Delete -> danger (删除)
        /// Query -> default (查询)
        /// </summary>
        AuditOperationType,

        /// <summary>
        /// 通用状态映射
        /// active/enabled/success -> success
        /// inactive/disabled/fail -> fail
        /// pending/processing -> info
        /// warning -> warning
        /// error/danger -> danger
        /// </summary>
        CommonStatus,

        /// <summary>
        /// HTTP请求方法映射
        /// GET -> info (查询)
        /// POST -> success (创建)
        /// PUT -> warning (更新)
        /// DELETE -> danger (删除)
        /// PATCH -> warning (部分更新)
        /// HEAD/OPTIONS -> default (其他)
        /// </summary>
        HttpMethod,

        /// <summary>
        /// 数字状态映射
        /// 1 -> success
        /// 0 -> fail
        /// -1 -> warning
        /// </summary>
        NumericStatus
    }
}
