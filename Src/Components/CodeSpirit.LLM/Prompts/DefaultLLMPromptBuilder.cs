using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM.Prompts;

/// <summary>
/// 默认LLM提示词构建器实现
/// </summary>
public class DefaultLLMPromptBuilder : ILLMPromptBuilder
{
    private readonly ILLMPromptTemplateManager _templateManager;
    private readonly ILogger<DefaultLLMPromptBuilder> _logger;

    private string? _systemPrompt;
    private string? _templateContent;
    private readonly List<string> _validationRules = new();
    private readonly List<object> _examples = new();
    private string? _outputFormat;
    private readonly List<string> _instructions = new();
    private string? _context;

    /// <summary>
    /// 初始化默认LLM提示词构建器
    /// </summary>
    /// <param name="templateManager">模板管理器</param>
    /// <param name="logger">日志记录器</param>
    public DefaultLLMPromptBuilder(
        ILLMPromptTemplateManager templateManager,
        ILogger<DefaultLLMPromptBuilder> logger)
    {
        _templateManager = templateManager;
        _logger = logger;
    }

    /// <summary>
    /// 设置系统提示词
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithSystemPrompt(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        return this;
    }

    /// <summary>
    /// 使用模板构建提示词
    /// </summary>
    /// <param name="templateName">模板名称</param>
    /// <param name="parameters">模板参数</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithTemplate(string templateName, object parameters)
    {
        try
        {
            _templateContent = _templateManager.RenderTemplate(templateName, parameters);
            _logger.LogDebug("使用模板 {TemplateName} 构建提示词", templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用模板 {TemplateName} 构建提示词失败", templateName);
            throw;
        }
        return this;
    }

    /// <summary>
    /// 添加验证规则
    /// </summary>
    /// <param name="rules">验证规则列表</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithValidationRules(params string[] rules)
    {
        _validationRules.AddRange(rules);
        return this;
    }

    /// <summary>
    /// 添加示例
    /// </summary>
    /// <param name="examples">示例列表</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithExamples(params object[] examples)
    {
        _examples.AddRange(examples);
        return this;
    }

    /// <summary>
    /// 指定输出格式
    /// </summary>
    /// <typeparam name="T">输出类型</typeparam>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithOutputFormat<T>()
    {
        _outputFormat = GenerateJsonSchema<T>();
        return this;
    }

    /// <summary>
    /// 添加自定义指令
    /// </summary>
    /// <param name="instruction">指令内容</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithInstruction(string instruction)
    {
        _instructions.Add(instruction);
        return this;
    }

    /// <summary>
    /// 添加上下文信息
    /// </summary>
    /// <param name="context">上下文内容</param>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder WithContext(string context)
    {
        _context = context;
        return this;
    }

    /// <summary>
    /// 构建最终的提示词
    /// </summary>
    /// <returns>构建的提示词</returns>
    public string Build()
    {
        var prompt = new StringBuilder();

        // 1. 系统提示词
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            prompt.AppendLine(_systemPrompt);
            prompt.AppendLine();
        }

        // 2. 上下文信息
        if (!string.IsNullOrEmpty(_context))
        {
            prompt.AppendLine("## 上下文信息");
            prompt.AppendLine(_context);
            prompt.AppendLine();
        }

        // 3. 模板内容
        if (!string.IsNullOrEmpty(_templateContent))
        {
            prompt.AppendLine(_templateContent);
            prompt.AppendLine();
        }

        // 4. 验证规则
        if (_validationRules.Count > 0)
        {
            prompt.AppendLine("## 验证规则");
            for (int i = 0; i < _validationRules.Count; i++)
            {
                prompt.AppendLine($"{i + 1}. {_validationRules[i]}");
            }
            prompt.AppendLine();
        }

        // 5. 示例
        if (_examples.Count > 0)
        {
            prompt.AppendLine("## 示例");
            for (int i = 0; i < _examples.Count; i++)
            {
                prompt.AppendLine($"### 示例 {i + 1}");
                prompt.AppendLine(FormatExample(_examples[i]));
                prompt.AppendLine();
            }
        }

        // 6. 自定义指令
        if (_instructions.Count > 0)
        {
            prompt.AppendLine("## 指令");
            foreach (var instruction in _instructions)
            {
                prompt.AppendLine($"- {instruction}");
            }
            prompt.AppendLine();
        }

        // 7. 输出格式
        if (!string.IsNullOrEmpty(_outputFormat))
        {
            prompt.AppendLine("## 输出格式");
            prompt.AppendLine("请以JSON格式返回结果，格式如下：");
            prompt.AppendLine("```json");
            prompt.AppendLine(_outputFormat);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }

        var result = prompt.ToString().Trim();
        _logger.LogDebug("构建提示词完成，长度: {Length}", result.Length);
        
        return result;
    }

    /// <summary>
    /// 重置构建器状态
    /// </summary>
    /// <returns>构建器实例</returns>
    public ILLMPromptBuilder Reset()
    {
        _systemPrompt = null;
        _templateContent = null;
        _validationRules.Clear();
        _examples.Clear();
        _outputFormat = null;
        _instructions.Clear();
        _context = null;
        return this;
    }

    /// <summary>
    /// 生成JSON Schema
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>JSON Schema字符串</returns>
    private string GenerateJsonSchema<T>()
    {
        try
        {
            var type = typeof(T);
            var schema = new StringBuilder();
            
            schema.AppendLine("{");
            GenerateObjectSchema(type, schema, 1);
            schema.AppendLine("}");
            
            return schema.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "生成JSON Schema失败，类型: {Type}", typeof(T).Name);
            return "{}";
        }
    }

    /// <summary>
    /// 生成对象Schema
    /// </summary>
    /// <param name="type">对象类型</param>
    /// <param name="schema">Schema构建器</param>
    /// <param name="indent">缩进级别</param>
    private void GenerateObjectSchema(Type type, StringBuilder schema, int indent)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var indentStr = new string(' ', indent * 2);
        
        for (int i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var propertyName = GetJsonPropertyName(property);
            var propertyType = GetJsonType(property.PropertyType);
            
            schema.Append($"{indentStr}\"{propertyName}\": ");
            
            if (propertyType == "object")
            {
                schema.AppendLine("{");
                GenerateObjectSchema(property.PropertyType, schema, indent + 1);
                schema.Append($"{indentStr}}}");
            }
            else if (propertyType == "array")
            {
                var elementType = property.PropertyType.GetElementType() ?? 
                                 property.PropertyType.GetGenericArguments().FirstOrDefault();
                if (elementType != null)
                {
                    schema.Append("[");
                    if (GetJsonType(elementType) == "object")
                    {
                        schema.AppendLine("{");
                        GenerateObjectSchema(elementType, schema, indent + 1);
                        schema.Append($"{indentStr}  }}");
                    }
                    else
                    {
                        schema.Append($"\"{GetJsonType(elementType)} value\"");
                    }
                    schema.Append("]");
                }
                else
                {
                    schema.Append("[]");
                }
            }
            else
            {
                schema.Append($"\"{propertyType} value\"");
            }
            
            if (i < properties.Length - 1)
            {
                schema.AppendLine(",");
            }
            else
            {
                schema.AppendLine();
            }
        }
    }

    /// <summary>
    /// 获取JSON属性名
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <returns>JSON属性名</returns>
    private string GetJsonPropertyName(PropertyInfo property)
    {
        // 可以在这里处理JsonPropertyName特性等
        return char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
    }

    /// <summary>
    /// 获取JSON类型名
    /// </summary>
    /// <param name="type">C#类型</param>
    /// <returns>JSON类型名</returns>
    private string GetJsonType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || 
            type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || 
            type == typeof(ushort) || type == typeof(sbyte))
            return "number";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return "string";
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            return "array";
        if (type.IsEnum)
            return "string";
        if (Nullable.GetUnderlyingType(type) != null)
            return GetJsonType(Nullable.GetUnderlyingType(type)!);
        
        return "object";
    }

    /// <summary>
    /// 格式化示例
    /// </summary>
    /// <param name="example">示例对象</param>
    /// <returns>格式化后的示例字符串</returns>
    private string FormatExample(object example)
    {
        try
        {
            if (example is string str)
            {
                return str;
            }

            return JsonSerializer.Serialize(example, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "格式化示例失败");
            return example.ToString() ?? "";
        }
    }
}
