using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace CodeSpirit.Aggregator.Services
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
            
            // 尝试解码Base64编码的头部值
            try
            {
                if (IsBase64String(aggregateKeys))
                {
                    var bytes = Convert.FromBase64String(aggregateKeys);
                    aggregateKeys = Encoding.UTF8.GetString(bytes);
                    _logger.LogInformation("解码Base64编码的聚合规则: {DecodedKeys}", aggregateKeys);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解码聚合规则失败，将使用原始值");
            }

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
                        var firstSeparatorIndex = equalsIndex > 0 && hashIndex > 0 
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

        /// <summary>
        /// 检查字符串是否为Base64编码
        /// </summary>
        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return false;
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
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
            if (obj.TryGetValue(fieldName, out JToken value))
            {
                string sourceValue = value.ToString();
                if (!string.IsNullOrEmpty(sourceValue))
                {
                    string aggregatedValue = await ApplyAggregationRule(sourceValue, ruleSpec, context);
                    if (!string.IsNullOrEmpty(aggregatedValue))
                    {
                        obj[fieldName] = aggregatedValue;
                        _logger.LogInformation("字段 {FieldName} 聚合结果: {SourceValue} => {AggregatedValue}",
                            fieldName, sourceValue, aggregatedValue);
                    }
                    else
                    {
                        _logger.LogWarning("字段 {FieldName} 聚合结果为空，保持原值: {SourceValue}",
                            fieldName, sourceValue);
                    }
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
            try
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
                
                _logger.LogInformation("应用聚合规则: 数据源={DataSource}, 模板={Template}", dataSource, template);
                
                // 根据规则类型进行处理
                if (string.IsNullOrEmpty(dataSource) && !string.IsNullOrEmpty(template))
                {
                    // 静态替换: 字段#模板
                    var result = template.Replace("{value}", sourceValue);
                    _logger.LogInformation("静态替换结果: {Result}", result);
                    return result;
                }
                else if (!string.IsNullOrEmpty(dataSource))
                {
                    // 动态替换或补充: 字段=/path/{value}.响应字段[#模板]
                    var dataSourceResult = await FetchFromDataSource(dataSource, sourceValue, context);
                    _logger.LogInformation("数据源结果: {Result}", dataSourceResult);
                    
                    if (string.IsNullOrEmpty(template))
                    {
                        // 动态替换: 字段=/path/{value}.响应字段
                        if (string.IsNullOrEmpty(dataSourceResult))
                        {
                            _logger.LogWarning("数据源返回空值，使用原始值: {Value}", sourceValue);
                            return sourceValue;
                        }
                        _logger.LogInformation("动态替换结果: {Result}", dataSourceResult);
                        return dataSourceResult;
                    }
                    else
                    {
                        // 动态补充: 字段=/path/{value}.字段#模板
                        var result = template
                            .Replace("{value}", sourceValue)
                            .Replace("{field}", dataSourceResult ?? string.Empty);
                        _logger.LogInformation("动态补充结果: {Result}", result);
                        return result;
                    }
                }
                
                // 默认返回原值
                _logger.LogInformation("使用默认值: {Value}", sourceValue);
                return sourceValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用聚合规则失败: {Rule}", ruleSpec);
                return sourceValue;
            }
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
                var fieldPath = dataSource.Substring(pathEndIndex + 1);
                
                _logger.LogInformation("从数据源获取数据: 路径={Path}, 字段路径={FieldPath}", path, fieldPath);
                
                // 从 HttpContext 中获取 HttpClient
                var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("AggregationClient");
                
                // 发起请求
                var response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("从数据源获取的响应: {Content}", content);
                    
                    var jsonObj = JObject.Parse(content);
                    
                    // 提取指定字段，支持嵌套属性路径
                    var fieldParts = fieldPath.Split('.');
                    JToken currentToken = jsonObj;
                    
                    _logger.LogInformation("开始处理嵌套字段路径: {FieldPath}, 字段部分: {FieldParts}", 
                        fieldPath, string.Join(", ", fieldParts));
                    
                    foreach (var part in fieldParts)
                    {
                        if (currentToken is JObject obj)
                        {
                            _logger.LogInformation("当前处理字段部分: {Part}, 当前Token类型: {TokenType}, 当前Token内容: {TokenContent}", 
                                part, currentToken.Type, currentToken.ToString());
                            
                            if (obj.TryGetValue(part, out JToken value))
                            {
                                currentToken = value;
                                _logger.LogInformation("成功获取字段值: {Part} = {Value}, 类型: {TokenType}", 
                                    part, value.ToString(), value.Type);
                            }
                            else
                            {
                                _logger.LogError("数据源响应中未找到字段 {FieldPath}: {Content}", fieldPath, content);
                                return null;
                            }
                        }
                        else
                        {
                            _logger.LogError("无法访问嵌套字段 {FieldPath}: 父级不是对象类型, 当前Token类型: {TokenType}, 内容: {TokenContent}", 
                                fieldPath, currentToken.Type, currentToken.ToString());
                            return null;
                        }
                    }
                    
                    if (currentToken.Type != JTokenType.String)
                    {
                        _logger.LogError("最终字段值不是字符串类型: {FieldPath}, 类型: {TokenType}, 内容: {TokenContent}", 
                            fieldPath, currentToken.Type, currentToken.ToString());
                        return null;
                    }
                    
                    var result = currentToken.ToString();
                    _logger.LogInformation("从数据源提取的字段值: {FieldPath} = {Value}", fieldPath, result);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("从数据源获取数据失败: {Status} {Path}, 响应内容: {Content}", 
                        response.StatusCode, path, errorContent);
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