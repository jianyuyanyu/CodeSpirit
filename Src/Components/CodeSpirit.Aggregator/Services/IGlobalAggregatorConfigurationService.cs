using System.Collections.Generic;

namespace CodeSpirit.Aggregator.Services
{
    /// <summary>
    /// 全局聚合器配置服务接口
    /// </summary>
    public interface IGlobalAggregatorConfigurationService
    {
        /// <summary>
        /// 注册全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称（如：CreatedBy、UpdatedBy等）</param>
        /// <param name="dataSource">数据源路径，格式：/path/{value}.响应字段</param>
        /// <param name="template">输出模板，可使用 {value} 和 {field} 占位符</param>
        void RegisterGlobalRule(string fieldName, string dataSource, string template = null);

        /// <summary>
        /// 获取指定字段的全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>聚合规则，如果不存在则返回null</returns>
        GlobalAggregationRule GetGlobalRule(string fieldName);

        /// <summary>
        /// 获取所有全局聚合规则
        /// </summary>
        /// <returns>全局聚合规则字典</returns>
        IReadOnlyDictionary<string, GlobalAggregationRule> GetAllGlobalRules();

        /// <summary>
        /// 移除全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>是否成功移除</returns>
        bool RemoveGlobalRule(string fieldName);

        /// <summary>
        /// 清空所有全局聚合规则
        /// </summary>
        void ClearAllGlobalRules();
    }

    /// <summary>
    /// 全局聚合规则
    /// </summary>
    public class GlobalAggregationRule
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 数据源路径，格式：/path/{value}.响应字段
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// 输出模板，可使用 {value} 和 {field} 占位符
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="dataSource">数据源路径</param>
        /// <param name="template">输出模板</param>
        public GlobalAggregationRule(string fieldName, string dataSource, string template = null)
        {
            FieldName = fieldName;
            DataSource = dataSource;
            Template = template;
        }

        /// <summary>
        /// 获取聚合规则字符串
        /// </summary>
        /// <param name="fieldPath">字段路径</param>
        /// <returns>聚合规则字符串</returns>
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
