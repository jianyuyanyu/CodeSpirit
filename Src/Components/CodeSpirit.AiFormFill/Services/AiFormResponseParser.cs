
using System.Text.RegularExpressions;

namespace CodeSpirit.AiFormFill.Services;

/// <summary>
/// AI表单响应解析器
/// </summary>
public class AiFormResponseParser : IScopedDependency
{
    private readonly ILogger<AiFormResponseParser> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AiFormResponseParser(ILogger<AiFormResponseParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 解析LLM响应
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="llmResponse">LLM响应</param>
    /// <param name="existingData">现有数据</param>
    /// <returns>解析后的对象</returns>
    public async Task<T> ParseResponseAsync<T>(string llmResponse, T? existingData = null) where T : class, new()
    {
        try
        {
            // 提取JSON部分
            var jsonContent = ExtractJsonContent(llmResponse);
            
            _logger.LogDebug("提取的JSON内容：{JsonContent}", jsonContent);
            
            // 解析JSON
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonContent);
            if (jsonObject == null)
            {
                throw new ArgumentException("无法解析JSON内容");
            }

            // 创建结果对象
            var result = existingData ?? new T();
            var dtoType = typeof(T);
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
            
            // 获取可填充的属性
            var properties = GetFillableProperties<T>(aiFormFillAttr?.IgnoreFields ?? Array.Empty<string>());

            // 填充属性值
            foreach (var property in properties)
            {
                var jsonPropertyName = GetJsonPropertyName(property);
                
                if (jsonObject.TryGetValue(jsonPropertyName, out var jsonValue) ||
                    jsonObject.TryGetValue(property.Name, out jsonValue)) // 尝试使用属性名作为备选
                {
                    try
                    {
                        var convertedValue = ConvertJsonValue(jsonValue, property.PropertyType);
                        if (convertedValue != null && property.CanWrite)
                        {
                            property.SetValue(result, convertedValue);
                            _logger.LogDebug("成功设置属性 {PropertyName} = {Value}", property.Name, convertedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "转换属性值失败：{PropertyName}, 值：{Value}", property.Name, jsonValue);
                    }
                }
                else
                {
                    _logger.LogDebug("JSON中未找到属性：{PropertyName}（JSON属性名：{JsonPropertyName}）", property.Name, jsonPropertyName);
                }
            }

            // 验证结果
            await ValidateResultAsync(result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI响应失败：{Response}", llmResponse);
            throw new BusinessException($"解析AI响应失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 提取JSON内容
    /// </summary>
    /// <param name="response">LLM响应</param>
    /// <returns>JSON内容</returns>
    private string ExtractJsonContent(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentException("响应内容为空");
        }

        // 尝试多种JSON提取方式
        
        // 1. 使用正则表达式提取JSON部分（支持多行）
        var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
        if (jsonMatch.Success)
        {
            return jsonMatch.Value;
        }

        // 2. 查找代码块中的JSON
        var codeBlockMatch = Regex.Match(response, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups[1].Value;
        }

        // 3. 如果整个响应看起来像JSON，直接使用
        var trimmedResponse = response.Trim();
        if (trimmedResponse.StartsWith("{") && trimmedResponse.EndsWith("}"))
        {
            return trimmedResponse;
        }

        throw new ArgumentException("响应中未找到有效的JSON格式");
    }

    /// <summary>
    /// 转换JSON值到目标类型
    /// </summary>
    /// <param name="jsonValue">JSON值</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值</returns>
    private object? ConvertJsonValue(JToken jsonValue, Type targetType)
    {
        if (jsonValue.Type == JTokenType.Null)
            return null;

        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            return underlyingType.Name switch
            {
                nameof(String) => jsonValue.ToString(),
                nameof(Int32) => jsonValue.ToObject<int>(),
                nameof(Int64) => jsonValue.ToObject<long>(),
                nameof(Double) => jsonValue.ToObject<double>(),
                nameof(Decimal) => jsonValue.ToObject<decimal>(),
                nameof(Boolean) => jsonValue.ToObject<bool>(),
                nameof(DateTime) => jsonValue.ToObject<DateTime>(),
                _ => jsonValue.ToObject(underlyingType)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "类型转换失败，尝试字符串转换：{JsonValue} -> {TargetType}", jsonValue, targetType.Name);
            
            // 如果直接转换失败，尝试转换为字符串
            if (underlyingType == typeof(string))
            {
                return jsonValue.ToString();
            }
            
            throw;
        }
    }

    /// <summary>
    /// 获取可填充的属性列表
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="ignoreFields">忽略的字段列表</param>
    /// <returns>可填充的属性列表</returns>
    private List<PropertyInfo> GetFillableProperties<T>(string[] ignoreFields) where T : class
    {
        var dtoType = typeof(T);
        var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
        
        return dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && 
                       // 全局模式不排除触发字段，传统模式排除触发字段
                       (aiFormFillAttr?.IsGlobalMode == true || p.Name != aiFormFillAttr?.TriggerField) && 
                       !ignoreFields.Contains(p.Name) && // 排除忽略字段
                       IsAiFillEnabled(p)) // 检查字段是否启用AI填充
            .ToList();
    }

    /// <summary>
    /// 检查字段是否启用AI填充
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>是否启用</returns>
    private bool IsAiFillEnabled(PropertyInfo property)
    {
        var aiFieldAttr = property.GetCustomAttribute<AiFieldFillAttribute>();
        return aiFieldAttr?.Enabled != false; // 默认启用，除非明确设置为false
    }

    /// <summary>
    /// 获取JSON属性名称
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>JSON属性名称</returns>
    private string GetJsonPropertyName(PropertyInfo property)
    {
        var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyAttribute>();
        return jsonPropertyAttr?.PropertyName ?? property.Name;
    }

    /// <summary>
    /// 验证解析结果
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="result">解析结果</param>
    /// <returns>验证任务</returns>
    private async Task ValidateResultAsync<T>(T result) where T : class
    {
        // 使用 DataAnnotations 进行验证
        var validationContext = new ValidationContext(result);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(result, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            _logger.LogWarning("AI填充结果验证失败：{Errors}", errors);
            // 注意：这里不抛出异常，因为AI生成的内容可能不完全符合验证规则
            // 可以根据业务需求决定是否抛出异常
        }
        
        await Task.CompletedTask;
    }
}
