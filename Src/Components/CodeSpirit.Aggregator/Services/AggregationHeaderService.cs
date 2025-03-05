using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace CodeSpirit.Aggregator.Services
{
    public class AggregationHeaderService : IAggregationHeaderService
    {
        private readonly ILogger<AggregationHeaderService> _logger;

        public AggregationHeaderService(ILogger<AggregationHeaderService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 从模型类型生成聚合规则头信息
        /// </summary>
        public string GenerateAggregationHeader(Type modelType)
        {
            var rules = new List<string>();
            CollectAggregationRules(modelType, string.Empty, rules);

            var headerValue = string.Join(",", rules);
            
            // 检查是否包含非ASCII字符
            if (headerValue.Any(c => c > 127))
            {
                // 使用Base64编码非ASCII字符
                var bytes = Encoding.UTF8.GetBytes(headerValue);
                headerValue = Convert.ToBase64String(bytes);
                _logger.LogInformation("聚合规则包含非ASCII字符，已进行Base64编码: {EncodedHeader}", headerValue);
            }

            return headerValue;
        }

        /// <summary>
        /// 递归收集聚合规则
        /// </summary>
        private void CollectAggregationRules(Type type, string parentPath, List<string> rules)
        {
            var properties = type.GetProperties()
                .Where(p => p.GetCustomAttribute<AggregateFieldAttribute>() != null);

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<AggregateFieldAttribute>();
                var fieldPath = string.IsNullOrEmpty(parentPath) 
                    ? property.Name.ToCamelCase() 
                    : $"{parentPath}.{property.Name.ToCamelCase()}";

                // 如果是复杂类型且没有数据源，则递归处理
                if (IsComplexType(property.PropertyType) && string.IsNullOrEmpty(attribute.DataSource))
                {
                    CollectAggregationRules(property.PropertyType, fieldPath, rules);
                }
                else
                {
                    var rule = attribute.GetRuleString(fieldPath);
                    rules.Add(rule);
                    _logger.LogInformation("发现聚合字段: {FieldPath}, 规则: {Rule}", fieldPath, rule);
                }
            }
        }

        /// <summary>
        /// 判断是否为复杂类型（非基本类型）
        /// </summary>
        private bool IsComplexType(Type type)
        {
            return !type.IsPrimitive 
                && type != typeof(string) 
                && type != typeof(decimal) 
                && type != typeof(DateTime)
                && type != typeof(Guid);
        }
    }
} 