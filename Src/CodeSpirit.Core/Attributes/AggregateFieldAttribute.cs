using System;

namespace CodeSpirit.Core.Attributes
{
    /// <summary>
    /// 用于标记需要聚合的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AggregateFieldAttribute : Attribute
    {
        /// <summary>
        /// 数据源路径，格式：/path/{value}.响应字段
        /// </summary>
        public string DataSource { get; }

        /// <summary>
        /// 输出模板，格式：模板文本，可使用 {value} 和 {field} 占位符
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataSource">数据源路径</param>
        /// <param name="template">输出模板</param>
        public AggregateFieldAttribute(string dataSource = null, string template = null)
        {
            DataSource = dataSource;
            Template = template;
        }

        /// <summary>
        /// 获取聚合规则字符串
        /// </summary>
        public string GetRuleString(string fieldPath)
        {
            var rule = fieldPath;
            
            if (!string.IsNullOrEmpty(DataSource))
            {
                rule += "=" + DataSource;
            }
            
            if (!string.IsNullOrEmpty(Template))
            {
                rule += "#" + Template;
            }
            
            return rule;
        }
    }
} 