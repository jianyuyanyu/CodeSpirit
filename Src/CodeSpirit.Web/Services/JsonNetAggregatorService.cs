using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Web.Services
{
    /// <summary>
    /// ## 使用示例
    /// ### 后端返回
    //// {
    ////  "id": 123,
    ////  "title": "测试文档",
    ////  "createdBy": "10001",
    ////  "updatedBy": "10002",
    ////  "items": [
    ////    {
    ////      "itemId": 1,
    ////      "createdBy": "10003"
    ////    }
    ////  ]
    ////}
    /// 
    //// X-Aggregate-Keys: createdBy,updatedBy
    //// X-Aggregate-Field-createdBy: userId=userName
    //// X-Aggregate-Field-updatedBy: userId=userName
    /// 
    /// ### 处理后返回
    //// {
    ////  "id": 123,
    ////  "title": "测试文档",
    ////  "createdBy": "User-10001",
    ////  "updatedBy": "User-10002",
    ////  "items": [
    ////    {
    ////      "itemId": 1,
    ////      "createdBy": "User-10003"
    ////    }
    ////  ]
    //// }
    /// 
    /// 
    /// 
    /// </summary>
    public class JsonNetAggregatorService : IAggregatorService
    {
        private readonly ILogger<JsonNetAggregatorService> _logger;

        public JsonNetAggregatorService(ILogger<JsonNetAggregatorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool NeedsAggregation(HttpResponseMessage response)
        {
            return response.Headers.Contains("X-Aggregate-Keys");
        }

        public Dictionary<string, string> GetAggregationRules(HttpResponseMessage response)
        {
            var aggregationRules = new Dictionary<string, string>();

            if (!NeedsAggregation(response))
                return aggregationRules;

            string aggregateKeys = response.Headers.GetValues("X-Aggregate-Keys").FirstOrDefault() ?? string.Empty;
            _logger.LogInformation("需要聚合的字段: {AggregateKeys}", aggregateKeys);

            // 解析每个字段的聚合规则
            foreach (var key in aggregateKeys.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string headerName = $"X-Aggregate-Field-{key.Trim()}";
                if (response.Headers.Contains(headerName))
                {
                    string rule = response.Headers.GetValues(headerName).FirstOrDefault() ?? string.Empty;
                    aggregationRules[key.Trim()] = rule;
                    _logger.LogInformation("字段 {Key} 的聚合规则: {Rule}", key, rule);
                }
            }

            return aggregationRules;
        }

        public async Task<string> AggregateJsonContent(string jsonContent, Dictionary<string, string> aggregationRules, HttpContext context)
        {
            try
            {
                // 解析JSON
                JToken root = JToken.Parse(jsonContent);

                if (root is JArray array)
                {
                    // 处理JSON数组
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (array[i] is JObject obj)
                        {
                            await ProcessJsonObject(obj, aggregationRules, context);
                        }
                    }
                }
                else if (root is JObject obj)
                {
                    // 处理单个JSON对象
                    await ProcessJsonObject(obj, aggregationRules, context);
                }

                // 将处理后的JSON转换回字符串
                return root.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理JSON内容失败");
                return jsonContent; // 发生错误时返回原始内容
            }
        }

        private async Task ProcessJsonObject(JObject obj, Dictionary<string, string> aggregationRules, HttpContext context)
        {
            // 存储需要替换的字段值
            Dictionary<string, string> replacementValues = new Dictionary<string, string>();

            // 首先收集所有需要聚合的字段的值
            foreach (var fieldName in aggregationRules.Keys)
            {
                if (obj.TryGetValue(fieldName, out JToken value) && value.Type == JTokenType.String)
                {
                    string sourceValue = value.ToString();
                    replacementValues[fieldName] = sourceValue;
                }
            }

            // 然后处理聚合规则
            foreach (var rulePair in aggregationRules)
            {
                string fieldName = rulePair.Key;
                string rule = rulePair.Value;

                if (replacementValues.TryGetValue(fieldName, out string sourceValue) && !string.IsNullOrEmpty(sourceValue))
                {
                    try
                    {
                        // 规则格式：sourceField=targetField
                        var ruleParts = rule.Split('=', 2);
                        if (ruleParts.Length == 2)
                        {
                            string sourceField = ruleParts[0].Trim();
                            string targetField = ruleParts[1].Trim();

                            // 执行聚合逻辑
                            string aggregatedValue = await GetAggregatedValue(sourceField, sourceValue, targetField, context);

                            // 替换原始值
                            obj[fieldName] = aggregatedValue;

                            _logger.LogInformation("字段 {FieldName} 聚合结果: {SourceValue} => {AggregatedValue}",
                                fieldName, sourceValue, aggregatedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理聚合规则失败: {Rule}", rule);
                    }
                }
            }

            // 递归处理嵌套的JSON对象
            foreach (var property in obj.Properties().ToList())
            {
                if (property.Value is JObject nestedObj)
                {
                    await ProcessJsonObject(nestedObj, aggregationRules, context);
                }
                else if (property.Value is JArray nestedArray)
                {
                    foreach (var item in nestedArray)
                    {
                        if (item is JObject nestedArrayObj)
                        {
                            await ProcessJsonObject(nestedArrayObj, aggregationRules, context);
                        }
                    }
                }
            }
        }

        private async Task<string> GetAggregatedValue(string sourceField, string sourceValue, string targetField,
            HttpContext context)
        {
            // 这里实现您的聚合逻辑
            // 示例：如果规则是 userId=userName，可以根据userId查询用户名

            // 简单示例实现
            if (sourceField.Equals("userId", StringComparison.OrdinalIgnoreCase) &&
                targetField.Equals("userName", StringComparison.OrdinalIgnoreCase))
            {
                // 这里可以从数据库或用户服务获取用户名
                // 示例代码仅作演示：
                try
                {
                    // 假设有一个用户服务
                    // var userService = context.RequestServices.GetRequiredService<IUserService>();
                    // var userName = await userService.GetUserNameById(sourceValue);
                    // return userName ?? sourceValue;

                    // 简单模拟返回
                    return $"User-{sourceValue}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "聚合用户信息失败: {UserId}", sourceValue);
                    return sourceValue; // 失败时返回原值
                }
            }

            // 处理其他聚合规则...

            // 默认返回原值
            return sourceValue;
        }
    }
}