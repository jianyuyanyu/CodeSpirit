using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeSpirit.Aggregator.Services
{
    /// <summary>
    /// 全局聚合器配置服务实现
    /// </summary>
    public class GlobalAggregatorConfigurationService : IGlobalAggregatorConfigurationService
    {
        private readonly ILogger<GlobalAggregatorConfigurationService> _logger;
        private readonly ConcurrentDictionary<string, GlobalAggregationRule> _globalRules;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public GlobalAggregatorConfigurationService(ILogger<GlobalAggregatorConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _globalRules = new ConcurrentDictionary<string, GlobalAggregationRule>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 注册全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称（如：CreatedBy、UpdatedBy等）</param>
        /// <param name="dataSource">数据源路径，格式：/path/{value}.响应字段</param>
        /// <param name="template">输出模板，可使用 {value} 和 {field} 占位符</param>
        public void RegisterGlobalRule(string fieldName, string dataSource, string template = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("字段名称不能为空", nameof(fieldName));
            }

            if (string.IsNullOrWhiteSpace(dataSource) && string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("数据源和模板不能同时为空", nameof(dataSource));
            }

            var rule = new GlobalAggregationRule(fieldName, dataSource, template);
            _globalRules.AddOrUpdate(fieldName, rule, (key, oldValue) => rule);

            _logger.LogInformation("注册全局聚合规则: 字段={FieldName}, 数据源={DataSource}, 模板={Template}", 
                fieldName, dataSource, template);
        }

        /// <summary>
        /// 获取指定字段的全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>聚合规则，如果不存在则返回null</returns>
        public GlobalAggregationRule GetGlobalRule(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            _globalRules.TryGetValue(fieldName, out var rule);
            return rule;
        }

        /// <summary>
        /// 获取所有全局聚合规则
        /// </summary>
        /// <returns>全局聚合规则字典</returns>
        public IReadOnlyDictionary<string, GlobalAggregationRule> GetAllGlobalRules()
        {
            return new Dictionary<string, GlobalAggregationRule>(_globalRules);
        }

        /// <summary>
        /// 移除全局聚合规则
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGlobalRule(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            var removed = _globalRules.TryRemove(fieldName, out var removedRule);
            if (removed)
            {
                _logger.LogInformation("移除全局聚合规则: 字段={FieldName}", fieldName);
            }

            return removed;
        }

        /// <summary>
        /// 清空所有全局聚合规则
        /// </summary>
        public void ClearAllGlobalRules()
        {
            var count = _globalRules.Count;
            _globalRules.Clear();
            _logger.LogInformation("清空所有全局聚合规则，共移除 {Count} 条规则", count);
        }
    }
}
