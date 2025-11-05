
using System.Text;

namespace CodeSpirit.AiFormFill.Services;

/// <summary>
/// AI表单提示词构建器
/// </summary>
public class AiFormPromptBuilder : IScopedDependency
{
    private readonly ILogger<AiFormPromptBuilder> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AiFormPromptBuilder(ILogger<AiFormPromptBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 构建表单填充提示词
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发字段的值</param>
    /// <param name="customTemplate">自定义模板</param>
    /// <param name="existingData">现有数据（用于替换自定义模板中的占位符）</param>
    /// <returns>构建的提示词</returns>
    public string BuildPrompt<T>(string triggerValue, string? customTemplate = null, object? existingData = null) where T : class
    {
        var dtoType = typeof(T);
        var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
        
        if (aiFormFillAttr == null)
        {
            throw new InvalidOperationException($"类型 {dtoType.Name} 未标记 AiFormFillAttribute 特性");
        }

        // 使用自定义模板或生成默认模板
        if (!string.IsNullOrEmpty(customTemplate))
        {
            return BuildCustomPrompt<T>(triggerValue, customTemplate, existingData);
        }

        return BuildDefaultPrompt<T>(triggerValue, aiFormFillAttr);
    }

    /// <summary>
    /// 构建自定义提示词
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发值</param>
    /// <param name="customTemplate">自定义模板</param>
    /// <param name="existingData">现有数据（用于替换占位符）</param>
    /// <returns>构建的提示词</returns>
    private string BuildCustomPrompt<T>(string triggerValue, string customTemplate, object? existingData = null) where T : class
    {
        try
        {
            var dtoType = typeof(T);
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>()!;
            
            // 1. 替换自定义模板中的命名占位符
            var processedPrompt = ReplaceNamedPlaceholders(customTemplate, existingData);
            
            // 2. 追加JSON返回结构说明
            var promptWithJsonStructure = AppendJsonStructure<T>(processedPrompt, aiFormFillAttr);
            
            // 3. 如果DTO包含DateTime字段，自动添加当前时间上下文
            return AppendCurrentTimeIfNeeded<T>(promptWithJsonStructure);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "自定义提示词模板格式化失败，使用默认模板");
            var aiFormFillAttr = typeof(T).GetCustomAttribute<AiFormFillAttribute>()!;
            return BuildDefaultPrompt<T>(triggerValue, aiFormFillAttr);
        }
    }

    /// <summary>
    /// 构建默认提示词
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发值</param>
    /// <param name="attr">AI填充特性</param>
    /// <returns>构建的提示词</returns>
    private string BuildDefaultPrompt<T>(string triggerValue, AiFormFillAttribute attr) where T : class
    {
        var dtoType = typeof(T);
        var properties = GetFillableProperties<T>(attr.IgnoreFields);
        
        var promptBuilder = new StringBuilder();
        
        // 添加基础上下文
        if (attr.IsGlobalMode)
        {
            // 全局模式提示词
            var dtoDisplayName = dtoType.Name.EndsWith("Dto") 
                ? dtoType.Name.Substring(0, dtoType.Name.Length - 3) 
                : dtoType.Name;
            promptBuilder.AppendLine($"请为以下{dtoDisplayName}表单的所有字段智能生成合适的内容：");
            promptBuilder.AppendLine();
        }
        else
        {
            // 传统模式提示词
            var triggerFieldDisplayName = GetTriggerFieldDisplayName<T>(attr.TriggerField);
            promptBuilder.AppendLine($"基于输入的{triggerFieldDisplayName}：\"{triggerValue}\"，请为以下表单字段生成合适的内容：");
            promptBuilder.AppendLine();
        }

        // 添加字段描述
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var displayName = GetDisplayName(prop);
            var description = GetFieldDescription(prop);
            var validationInfo = GetValidationInfo(prop);
            
            promptBuilder.AppendLine($"{i + 1}. {displayName}：{description}");
            
            // 添加验证约束信息
            if (!string.IsNullOrEmpty(validationInfo))
            {
                promptBuilder.AppendLine($"   约束条件：{validationInfo}");
            }
        }

        // 添加输出格式要求
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("请以JSON格式回复，格式如下：");
        promptBuilder.AppendLine("{");
        
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var jsonPropertyName = GetJsonPropertyName(prop);
            var comma = i < properties.Count - 1 ? "," : "";
            promptBuilder.AppendLine($"  \"{jsonPropertyName}\": \"字段值\"{comma}");
        }
        
        promptBuilder.AppendLine("}");
        
        // 添加要求和约束
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("要求：");
        promptBuilder.AppendLine("- 内容要与输入信息高度相关");
        promptBuilder.AppendLine("- 生成的内容要符合实际业务场景");
        promptBuilder.AppendLine("- 确保JSON格式正确");
        promptBuilder.AppendLine("- 字段值要简洁明了");
        promptBuilder.AppendLine("- 严格按照约束条件生成内容");

        // 如果DTO包含DateTime字段，自动添加当前时间上下文
        return AppendCurrentTimeIfNeeded<T>(promptBuilder.ToString());
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
            .OrderBy(p => GetFieldPriority(p))
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
    /// 获取字段优先级
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>优先级</returns>
    private int GetFieldPriority(PropertyInfo property)
    {
        var aiFieldAttr = property.GetCustomAttribute<AiFieldFillAttribute>();
        return aiFieldAttr?.Priority ?? 0;
    }

    /// <summary>
    /// 获取触发字段的显示名称
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerFieldName">触发字段名称</param>
    /// <returns>显示名称</returns>
    private string GetTriggerFieldDisplayName<T>(string triggerFieldName) where T : class
    {
        var triggerProperty = typeof(T).GetProperty(triggerFieldName);
        return triggerProperty != null ? GetDisplayName(triggerProperty) : triggerFieldName;
    }

    /// <summary>
    /// 获取属性显示名称
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>显示名称</returns>
    private string GetDisplayName(PropertyInfo property)
    {
        var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
        return displayNameAttr?.DisplayName ?? property.Name;
    }

    /// <summary>
    /// 智能获取字段描述
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>字段描述</returns>
    private string GetFieldDescription(PropertyInfo property)
    {
        var aiFieldAttr = property.GetCustomAttribute<AiFieldFillAttribute>();
        
        // 优先使用自定义描述
        if (!string.IsNullOrEmpty(aiFieldAttr?.CustomDescription))
        {
            return aiFieldAttr.CustomDescription;
        }
        
        // 其次使用Description特性
        var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
        if (!string.IsNullOrEmpty(descriptionAttr?.Description))
        {
            return descriptionAttr.Description;
        }
        
        // 最后使用DisplayName
        var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
        return displayNameAttr?.DisplayName ?? property.Name;
    }

    /// <summary>
    /// 自动获取验证信息
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>验证信息</returns>
    private string GetValidationInfo(PropertyInfo property)
    {
        var validationRules = new List<string>();
        
        // Required特性
        if (property.GetCustomAttribute<RequiredAttribute>() != null)
        {
            validationRules.Add("必填");
        }
        
        // StringLength特性
        var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
        if (stringLengthAttr != null)
        {
            if (stringLengthAttr.MinimumLength > 0)
            {
                validationRules.Add($"长度{stringLengthAttr.MinimumLength}-{stringLengthAttr.MaximumLength}字符");
            }
            else
            {
                validationRules.Add($"最大{stringLengthAttr.MaximumLength}字符");
            }
        }
        
        // Range特性
        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr != null)
        {
            validationRules.Add($"范围{rangeAttr.Minimum}-{rangeAttr.Maximum}");
        }
        
        // MinLength特性
        var minLengthAttr = property.GetCustomAttribute<MinLengthAttribute>();
        if (minLengthAttr != null)
        {
            validationRules.Add($"最少{minLengthAttr.Length}字符");
        }
        
        // MaxLength特性
        var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLengthAttr != null)
        {
            validationRules.Add($"最多{maxLengthAttr.Length}字符");
        }
        
        return string.Join("，", validationRules);
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
    /// 如果DTO包含DateTime字段，自动添加当前时间上下文
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="basePrompt">基础提示词</param>
    /// <returns>增强后的提示词</returns>
    private string AppendCurrentTimeIfNeeded<T>(string basePrompt) where T : class
    {
        var dtoType = typeof(T);
        var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
        if (aiFormFillAttr == null)
        {
            return basePrompt;
        }

        // 获取所有DateTime类型的可填充字段
        var dateTimeProperties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && 
                       !aiFormFillAttr.IgnoreFields.Contains(p.Name) &&
                       IsAiFillEnabled(p) &&
                       IsDateTimeType(p.PropertyType))
            .ToList();

        // 如果没有DateTime字段，直接返回原提示词
        if (dateTimeProperties.Count == 0)
        {
            return basePrompt;
        }

        // 构建时间上下文信息
        var currentTime = DateTime.Now;
        var timeContextBuilder = new StringBuilder();
        timeContextBuilder.AppendLine();
        timeContextBuilder.AppendLine("**重要时间上下文：**");
        timeContextBuilder.AppendLine($"当前日期时间：{currentTime:yyyy年MM月dd日 HH:mm:ss}（{currentTime.DayOfWeek}）");
        timeContextBuilder.AppendLine($"- 今天是：{currentTime:yyyy-MM-dd}");
        timeContextBuilder.AppendLine($"- 当前时刻：{currentTime:HH:mm:ss}");
        timeContextBuilder.AppendLine($"- ISO 8601格式：{currentTime:yyyy-MM-ddTHH:mm:ssZ}");
        timeContextBuilder.AppendLine();
        timeContextBuilder.AppendLine("**注意事项：**");
        timeContextBuilder.AppendLine("- 所有日期必须基于当前时间推算，不能返回过去的日期");
        timeContextBuilder.AppendLine("- 目标日期应该是未来的时间点");
        timeContextBuilder.AppendLine("- 日期格式请使用ISO 8601标准格式（如：2025-12-31T00:00:00Z）");
        
        _logger.LogDebug("检测到{Count}个DateTime字段，已添加当前时间上下文", dateTimeProperties.Count);

        return basePrompt + timeContextBuilder.ToString();
    }

    /// <summary>
    /// 检查类型是否为DateTime类型（包括可空类型）
    /// </summary>
    /// <param name="propertyType">属性类型</param>
    /// <returns>是否为DateTime类型</returns>
    private bool IsDateTimeType(Type propertyType)
    {
        // 检查直接的DateTime类型
        if (propertyType == typeof(DateTime))
        {
            return true;
        }

        // 检查可空的DateTime类型
        if (propertyType == typeof(DateTime?))
        {
            return true;
        }

        // 检查Nullable<DateTime>
        var underlyingType = Nullable.GetUnderlyingType(propertyType);
        if (underlyingType != null && underlyingType == typeof(DateTime))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检测提示词中是否已包含JSON结构说明
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>是否包含JSON结构说明</returns>
    private bool HasJsonStructureDescription(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            return false;
        }

        // 检测常见的JSON结构说明关键词
        var jsonStructureKeywords = new[]
        {
            "```json",           // Markdown JSON代码块
            "```JSON",           // Markdown JSON代码块（大写）
            "JSON结构说明",       // 中文说明
            "返回JSON结构",       // 中文说明
            "JSON格式说明",       // 中文说明
            "以JSON格式回复",     // 中文说明
            "JSON Schema",       // 英文说明
            "return JSON structure", // 英文说明
            "response format",   // 英文说明
            "output format"      // 英文说明
        };

        return jsonStructureKeywords.Any(keyword => 
            prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 替换自定义模板中的命名占位符
    /// </summary>
    /// <param name="template">模板字符串</param>
    /// <param name="existingData">数据源对象</param>
    /// <returns>替换后的字符串</returns>
    private string ReplaceNamedPlaceholders(string template, object? existingData)
    {
        if (existingData == null)
        {
            // 如果没有数据，将所有占位符替换为空字符串
            return System.Text.RegularExpressions.Regex.Replace(template, @"\{(\w+)\}", string.Empty);
        }

        var dataType = existingData.GetType();
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 构建属性值字典（不区分大小写）
        var propertyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(existingData);
                if (value != null)
                {
                    // 格式化不同类型的值
                    string formattedValue;
                    if (value is DateTime dateTime)
                    {
                        formattedValue = dateTime.ToString("yyyy年MM月dd日");
                    }
                    else if (value is DateTimeOffset dateTimeOffset)
                    {
                        formattedValue = dateTimeOffset.DateTime.ToString("yyyy年MM月dd日");
                    }
                    else if (value is System.Collections.IEnumerable enumerable && !(value is string))
                    {
                        // 对于集合类型，显示元素数量
                        var count = 0;
                        foreach (var _ in enumerable)
                        {
                            count++;
                        }
                        formattedValue = $"{count}项";
                    }
                    else
                    {
                        formattedValue = value.ToString() ?? string.Empty;
                    }
                    
                    propertyValues[prop.Name] = formattedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取属性 {PropertyName} 的值时出错", prop.Name);
            }
        }

        // 使用正则表达式替换所有占位符
        var result = System.Text.RegularExpressions.Regex.Replace(
            template,
            @"\{(\w+)\}",
            match =>
            {
                var propertyName = match.Groups[1].Value;
                if (propertyValues.TryGetValue(propertyName, out var value))
                {
                    return value;
                }
                // 如果找不到对应属性，替换为空字符串
                return string.Empty;
            });

        return result;
    }

    /// <summary>
    /// 追加JSON返回结构说明
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="basePrompt">基础提示词</param>
    /// <param name="attr">AI填充特性</param>
    /// <returns>追加后的提示词</returns>
    private string AppendJsonStructure<T>(string basePrompt, AiFormFillAttribute attr) where T : class
    {
        // 检测自定义模板中是否已包含JSON结构说明
        if (HasJsonStructureDescription(basePrompt))
        {
            _logger.LogDebug("自定义模板中已包含JSON结构说明，跳过追加");
            return basePrompt;
        }
        
        var properties = GetFillableProperties<T>(attr.IgnoreFields);
        
        if (properties.Count == 0)
        {
            _logger.LogWarning("类型 {Type} 没有可填充的字段", typeof(T).Name);
            return basePrompt;
        }

        var promptBuilder = new StringBuilder(basePrompt);
        
        // 确保基础提示词后有适当的换行
        if (!basePrompt.EndsWith("\n\n"))
        {
            if (basePrompt.EndsWith("\n"))
            {
                promptBuilder.AppendLine();
            }
            else
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine();
            }
        }

        // 添加JSON格式说明
        promptBuilder.AppendLine("请以JSON格式回复，格式如下：");
        promptBuilder.AppendLine("{");
        
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var jsonPropertyName = GetJsonPropertyName(prop);
            var comma = i < properties.Count - 1 ? "," : "";
            
            // 获取字段描述和验证信息用于注释
            var displayName = GetDisplayName(prop);
            var description = GetFieldDescription(prop);
            var validationInfo = GetValidationInfo(prop);
            
            // 构建字段注释
            var comment = new List<string>();
            // 优先使用CustomDescription
            if (!string.IsNullOrEmpty(description) && description != displayName)
            {
                comment.Add(description);
            }
            else if (!string.IsNullOrEmpty(displayName))
            {
                comment.Add(displayName);
            }
            if (!string.IsNullOrEmpty(validationInfo))
            {
                comment.Add(validationInfo);
            }
            var commentStr = comment.Count > 0 ? $" // {string.Join(", ", comment)}" : "";
            
            // 为复杂类型添加示例结构
            var propertyType = prop.PropertyType;
            if (IsCollectionType(propertyType))
            {
                // 获取集合的元素类型
                var elementType = GetCollectionElementType(propertyType);
                if (elementType != null && elementType.IsClass && elementType != typeof(string))
                {
                    // 展开集合元素的结构
                    promptBuilder.AppendLine($"  \"{jsonPropertyName}\": [{commentStr}");
                    var elementStructure = BuildObjectStructure(elementType, "    ");
                    promptBuilder.Append(elementStructure);
                    promptBuilder.AppendLine($"  ]{comma}");
                }
                else
                {
                    promptBuilder.AppendLine($"  \"{jsonPropertyName}\": []{comma}{commentStr}");
                }
            }
            else if (propertyType.IsClass && propertyType != typeof(string))
            {
                promptBuilder.AppendLine($"  \"{jsonPropertyName}\": {{}}{comma}{commentStr}");
            }
            else
            {
                promptBuilder.AppendLine($"  \"{jsonPropertyName}\": \"字段值\"{comma}{commentStr}");
            }
        }
        
        promptBuilder.AppendLine("}");
        
        // 添加要求和约束
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("要求：");
        promptBuilder.AppendLine("- 内容要与输入信息高度相关");
        promptBuilder.AppendLine("- 生成的内容要符合实际业务场景");
        promptBuilder.AppendLine("- 确保JSON格式正确");
        promptBuilder.AppendLine("- 字段值要简洁明了");
        promptBuilder.AppendLine("- 严格按照约束条件生成内容");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 检查类型是否为集合类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否为集合类型</returns>
    private bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// 获取集合的元素类型
    /// </summary>
    /// <param name="collectionType">集合类型</param>
    /// <returns>元素类型，如果无法确定则返回null</returns>
    private Type? GetCollectionElementType(Type collectionType)
    {
        // 处理数组
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // 处理泛型集合（如 List<T>, IEnumerable<T>）
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        // 查找实现的泛型接口
        var interfaces = collectionType.GetInterfaces();
        foreach (var @interface in interfaces)
        {
            if (@interface.IsGenericType && 
                @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return @interface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// 构建对象的JSON结构示例
    /// </summary>
    /// <param name="type">对象类型</param>
    /// <param name="indent">缩进</param>
    /// <returns>JSON结构字符串</returns>
    private string BuildObjectStructure(Type type, string indent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}{{");

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var jsonPropertyName = GetJsonPropertyName(prop);
            var comma = i < properties.Count - 1 ? "," : "";
            var displayName = GetDisplayName(prop);
            var description = GetFieldDescription(prop);
            var validationInfo = GetValidationInfo(prop);

            // 构建字段注释（优先使用CustomDescription，因为它是专门为AI设计的）
            var comment = new List<string>();
            
            // 如果description与displayName不同，说明存在更详细的描述（CustomDescription或Description特性）
            if (!string.IsNullOrEmpty(description) && description != displayName)
            {
                comment.Add(description);
            }
            else if (!string.IsNullOrEmpty(displayName))
            {
                comment.Add(displayName);
            }
            
            if (!string.IsNullOrEmpty(validationInfo))
            {
                comment.Add(validationInfo);
            }
            var commentStr = comment.Count > 0 ? $" // {string.Join(", ", comment)}" : "";

            // 根据属性类型生成示例值
            var propertyType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": \"string\"{comma}{commentStr}");
            }
            else if (underlyingType == typeof(int) || underlyingType == typeof(long) || 
                     underlyingType == typeof(short) || underlyingType == typeof(byte))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": 0{comma}{commentStr}");
            }
            else if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || 
                     underlyingType == typeof(float))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": 0.0{comma}{commentStr}");
            }
            else if (underlyingType == typeof(bool))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": false{comma}{commentStr}");
            }
            else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": \"2025-01-01T00:00:00Z\"{comma}{commentStr}");
            }
            else if (underlyingType == typeof(Guid))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": \"00000000-0000-0000-0000-000000000000\"{comma}{commentStr}");
            }
            else if (IsCollectionType(propertyType))
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": []{comma}{commentStr}");
            }
            else if (underlyingType.IsClass)
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": {{}}{comma}{commentStr}");
            }
            else
            {
                sb.AppendLine($"{indent}  \"{jsonPropertyName}\": null{comma}{commentStr}");
            }
        }

        sb.Append($"{indent}}}");
        return sb.ToString();
    }
}
