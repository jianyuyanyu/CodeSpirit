using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
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
            // 处理 Task<T> 和 ActionResult<T> 类型
            modelType = ExtractInnerMostType(modelType);

            try
            {
                var rules = new List<string>();
                CollectAggregationRules(modelType, string.Empty, rules);

                if (!rules.Any())
                {
                    _logger.LogInformation("未找到任何聚合规则: {ModelType}", modelType.FullName);
                    return string.Empty;
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

                _logger.LogInformation("生成聚合规则: {ModelType} => {Rules}", modelType.FullName, headerValue);
                return headerValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成聚合规则时发生错误: {ModelType}", modelType.FullName);
                throw;
            }
        }

        private Type ExtractInnerMostType(Type type)
        {
            while (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(Task<>) || 
                    genericDef == typeof(ActionResult<>))
                {
                    type = type.GetGenericArguments()[0];
                }
                else
                {
                    break;
                }
            }
            return type;
        }

        /// <summary>
        /// 递归收集聚合规则
        /// </summary>
        private void CollectAggregationRules(Type type, string parentPath, List<string> rules)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                
                // 处理 ApiResponse<T> 类型
                if (genericDef == typeof(ApiResponse<>))
                {
                    type = type.GetGenericArguments()[0];
                    parentPath = string.IsNullOrEmpty(parentPath) ? "data" : $"{parentPath}.data";
                    CollectAggregationRules(type, parentPath, rules);
                    return;
                }
                
                // 处理集合类型
                if (genericDef == typeof(PageList<>) ||
                    genericDef == typeof(List<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    type = type.GetGenericArguments()[0];
                    parentPath = string.IsNullOrEmpty(parentPath) ? "items" : $"{parentPath}.items";
                    CollectAggregationRules(type, parentPath, rules);
                    return;
                }
            }

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
                    _logger.LogDebug("处理复杂类型属性: {PropertyName} => {PropertyType}", property.Name, property.PropertyType.Name);
                    CollectAggregationRules(property.PropertyType, fieldPath, rules);
                }
                else
                {
                    var rule = attribute.GetRuleString(fieldPath);
                    rules.Add(rule);
                    _logger.LogDebug("添加聚合规则: {FieldPath} => {Rule}", fieldPath, rule);
                }
            }
        }

        /// <summary>
        /// 判断是否为复杂类型（非基本类型）
        /// </summary>
        private bool IsComplexType(Type type)
        {
            if (type.IsEnum) return false;
            
            return !type.IsPrimitive 
                && type != typeof(string) 
                && type != typeof(decimal) 
                && type != typeof(DateTime)
                && type != typeof(DateTimeOffset)
                && type != typeof(TimeSpan)
                && type != typeof(Guid);
        }
    }
} 