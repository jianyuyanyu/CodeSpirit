using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Web.Services
{
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

        /// <summary>
        /// 获取聚合规则
        /// </summary>
        public Dictionary<string, string> GetAggregationRules(HttpResponseMessage response)
        {
            var aggregationRules = new Dictionary<string, string>();

            if (!NeedsAggregation(response))
                return aggregationRules;

            string aggregateKeys = response.Headers.GetValues("X-Aggregate-Keys").FirstOrDefault() ?? string.Empty;
            _logger.LogInformation("需要聚合的字段: {AggregateKeys}", aggregateKeys);

            // 解析每个字段的聚合规则
            foreach (var rule in aggregateKeys.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedRule = rule.Trim();
                if (!string.IsNullOrEmpty(trimmedRule))
                {
                    // 规则的第一部分是字段路径，可能包含点号分隔的路径
                    var fieldPath = trimmedRule;
                    
                    // 检查是否有 = 或 # 
                    var equalsIndex = trimmedRule.IndexOf('=');
                    var hashIndex = trimmedRule.IndexOf('#');
                    
                    if (equalsIndex > 0 || hashIndex > 0)
                    {
                        var firstSeparatorIndex = (equalsIndex > 0 && hashIndex > 0) 
                            ? Math.Min(equalsIndex, hashIndex) 
                            : Math.Max(equalsIndex, hashIndex);
                        
                        fieldPath = trimmedRule.Substring(0, firstSeparatorIndex).Trim();
                    }
                    
                    aggregationRules[fieldPath] = trimmedRule;
                    _logger.LogInformation("字段 {FieldPath} 的聚合规则: {Rule}", fieldPath, trimmedRule);
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

        /// <summary>
        /// 处理JSON对象中的聚合规则
        /// </summary>
        private async Task ProcessJsonObject(JObject obj, Dictionary<string, string> aggregationRules, HttpContext context)
        {
            // 处理每个聚合规则
            foreach (var rulePair in aggregationRules)
            {
                string fieldPath = rulePair.Key;
                string ruleSpec = rulePair.Value;

                try
                {
                    // 解析字段路径，处理嵌套路径情况
                    var pathParts = fieldPath.Split('.');
                    if (pathParts.Length == 1)
                    {
                        // 简单字段
                        await ProcessSimpleField(obj, fieldPath, ruleSpec, context);
                    }
                    else
                    {
                        // 处理嵌套字段
                        await ProcessNestedField(obj, pathParts, ruleSpec, context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理字段 {FieldPath} 的聚合规则失败: {Rule}", fieldPath, ruleSpec);
                }
            }
        }

        /// <summary>
        /// 处理简单字段（非嵌套）的聚合
        /// </summary>
        private async Task ProcessSimpleField(JObject obj, string fieldName, string ruleSpec, HttpContext context)
        {
            if (obj.TryGetValue(fieldName, out JToken value) && value.Type == JTokenType.String)
            {
                string sourceValue = value.ToString();
                if (!string.IsNullOrEmpty(sourceValue))
                {
                    string aggregatedValue = await ApplyAggregationRule(sourceValue, ruleSpec, context);
                    obj[fieldName] = aggregatedValue;
                    
                    _logger.LogInformation("字段 {FieldName} 聚合结果: {SourceValue} => {AggregatedValue}",
                        fieldName, sourceValue, aggregatedValue);
                }
            }
        }

        /// <summary>
        /// 处理嵌套字段的聚合
        /// </summary>
        private async Task ProcessNestedField(JObject root, string[] pathParts, string ruleSpec, HttpContext context)
        {
            // 递归查找并处理嵌套字段
            await ProcessNestedFieldRecursive(root, pathParts, 0, ruleSpec, context);
        }

        /// <summary>
        /// 递归处理嵌套字段
        /// </summary>
        private async Task ProcessNestedFieldRecursive(JToken current, string[] pathParts, int depth, string ruleSpec, HttpContext context)
        {
            if (depth >= pathParts.Length)
                return;

            var currentPart = pathParts[depth];
            var isLastPart = depth == pathParts.Length - 1;

            if (current is JObject obj)
            {
                if (obj.TryGetValue(currentPart, out JToken childToken))
                {
                    if (isLastPart)
                    {
                        // 到达最终字段
                        if (childToken.Type == JTokenType.String)
                        {
                            string sourceValue = childToken.ToString();
                            if (!string.IsNullOrEmpty(sourceValue))
                            {
                                string aggregatedValue = await ApplyAggregationRule(sourceValue, ruleSpec, context);
                                obj[currentPart] = aggregatedValue;
                                
                                _logger.LogInformation("字段 {FieldPath} 聚合结果: {SourceValue} => {AggregatedValue}",
                                    string.Join(".", pathParts), sourceValue, aggregatedValue);
                            }
                        }
                    }
                    else
                    {
                        // 继续遍历下一级
                        await ProcessNestedFieldRecursive(childToken, pathParts, depth + 1, ruleSpec, context);
                    }
                }
            }
            else if (current is JArray array)
            {
                // 对数组中的每个对象应用递归处理
                foreach (var item in array)
                {
                    await ProcessNestedFieldRecursive(item, pathParts, depth, ruleSpec, context);
                }
            }
        }

        /// <summary>
        /// 应用聚合规则到字段值
        /// </summary>
        private async Task<string> ApplyAggregationRule(string sourceValue, string ruleSpec, HttpContext context)
        {
            // 解析规则结构: 字段路径[=数据源][#模板]
            var dataSource = string.Empty;
            var template = string.Empty;
            
            var equalsIndex = ruleSpec.IndexOf('=');
            var hashIndex = ruleSpec.IndexOf('#');
            
            // 提取数据源部分
            if (equalsIndex > 0)
            {
                var endIndex = hashIndex > equalsIndex ? hashIndex : ruleSpec.Length;
                dataSource = ruleSpec.Substring(equalsIndex + 1, endIndex - equalsIndex - 1).Trim();
            }
            
            // 提取模板部分
            if (hashIndex > 0)
            {
                template = ruleSpec.Substring(hashIndex + 1).Trim();
            }
            
            // 根据规则类型进行处理
            if (string.IsNullOrEmpty(dataSource) && !string.IsNullOrEmpty(template))
            {
                // 静态替换: 字段#模板
                return template.Replace("{value}", sourceValue);
            }
            else if (!string.IsNullOrEmpty(dataSource))
            {
                // 动态替换或补充: 字段=/path/{value}.响应字段[#模板]
                var dataSourceResult = await FetchFromDataSource(dataSource, sourceValue, context);
                
                if (string.IsNullOrEmpty(template))
                {
                    // 动态替换: 字段=/path/{value}.响应字段
                    return dataSourceResult ?? sourceValue;
                }
                else
                {
                    // 动态补充: 字段=/path/{value}.字段#模板
                    return template
                        .Replace("{value}", sourceValue)
                        .Replace("{field}", dataSourceResult ?? string.Empty);
                }
            }
            
            // 默认返回原值
            return sourceValue;
        }

        /// <summary>
        /// 从数据源获取数据
        /// </summary>
        private async Task<string> FetchFromDataSource(string dataSource, string sourceValue, HttpContext context)
        {
            try
            {
                // 解析数据源: /path/{value}.响应字段
                var pathEndIndex = dataSource.LastIndexOf('.');
                if (pathEndIndex <= 0)
                {
                    _logger.LogError("无效的数据源格式: {DataSource}", dataSource);
                    return null;
                }
                
                var path = dataSource.Substring(0, pathEndIndex).Replace("{value}", sourceValue);
                var fieldName = dataSource.Substring(pathEndIndex + 1);
                
                // 从 HttpContext 中获取 HttpClient
                var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("AggregationClient");
                
                // 发起请求
                var response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonObj = JObject.Parse(content);
                    
                    // 提取指定字段
                    if (jsonObj.TryGetValue(fieldName, out JToken value))
                    {
                        return value.ToString();
                    }
                }
                else
                {
                    _logger.LogError("从数据源获取数据失败: {Status} {Path}", response.StatusCode, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从数据源 {DataSource} 获取数据失败", dataSource);
            }
            
            return null;
        }
    }
}