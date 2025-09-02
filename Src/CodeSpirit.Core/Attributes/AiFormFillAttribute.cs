using System;

namespace CodeSpirit.Core.Attributes
{
    /// <summary>
    /// AI表单填充特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AiFormFillAttribute : Attribute
    {
        /// <summary>
        /// 触发AI填充的字段名称
        /// 如果为空，则启用全局AI填充模式（在表单顶部显示AI优化组件）
        /// </summary>
        public string TriggerField { get; set; } = string.Empty;

        /// <summary>
        /// 需要忽略的字段列表
        /// </summary>
        public string[] IgnoreFields { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 自定义提示词模板
        /// </summary>
        public string CustomPromptTemplate { get; set; }

        /// <summary>
        /// API端点路径（相对路径）
        /// </summary>
        public string ApiEndpoint { get; set; }

        /// <summary>
        /// 最大Token数量
        /// </summary>
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 全局AI填充模式的提示文本
        /// 当TriggerField为空时，在表单顶部显示的AI优化文本框为空时的提示词
        /// </summary>
        public string GlobalFillPrompt { get; set; } = "使用AI智能优化表单";

        /// <summary>
        /// 是否为全局AI填充模式
        /// 当TriggerField为空时，返回true
        /// </summary>
        public bool IsGlobalMode => string.IsNullOrEmpty(TriggerField);
    }

    /// <summary>
    /// AI字段填充特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AiFieldFillAttribute : Attribute
    {
        /// <summary>
        /// 是否参与AI填充
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 字段权重（影响提示词中的重要性）
        /// </summary>
        public int Weight { get; set; } = 1;

        /// <summary>
        /// 字段填充优先级
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 自定义字段描述（用于提示词生成）
        /// 如果未设置，将自动从属性的Description特性获取
        /// </summary>
        public string CustomDescription { get; set; }
    }
}
