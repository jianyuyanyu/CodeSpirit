using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace CodeSpirit.Aggregator.Services
{
    public class AggregationHeaderService
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
            
            // 获取所有带有 AggregateFieldAttribute 的属性
            var properties = modelType.GetProperties()
                .Where(p => p.GetCustomAttribute<AggregateFieldAttribute>() != null);

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<AggregateFieldAttribute>();
                var fieldPath = GetFieldPath(property);
                var rule = attribute.GetRuleString(fieldPath);
                rules.Add(rule);
                
                _logger.LogInformation("发现聚合字段: {FieldPath}, 规则: {Rule}", fieldPath, rule);
            }

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
        /// 获取字段的完整路径（包括嵌套属性）
        /// </summary>
        private string GetFieldPath(PropertyInfo property)
        {
            var path = new List<string> { property.Name.ToCamelCase() };
            var currentType = property.DeclaringType;

            // 处理嵌套属性
            while (currentType != null)
            {
                var parentProperty = currentType.GetProperties()
                    .FirstOrDefault(p => p.PropertyType == currentType);
                
                if (parentProperty == null)
                    break;

                path.Insert(0, parentProperty.Name.ToCamelCase());
                currentType = parentProperty.DeclaringType;
            }

            return string.Join(".", path);
        }
    }
} 