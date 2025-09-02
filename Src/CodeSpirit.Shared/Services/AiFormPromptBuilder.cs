using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace CodeSpirit.Shared.Services;

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
    /// <returns>构建的提示词</returns>
    public string BuildPrompt<T>(string triggerValue, string? customTemplate = null) where T : class
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
            return BuildCustomPrompt<T>(triggerValue, customTemplate);
        }

        return BuildDefaultPrompt<T>(triggerValue, aiFormFillAttr);
    }

    /// <summary>
    /// 构建自定义提示词
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发值</param>
    /// <param name="customTemplate">自定义模板</param>
    /// <returns>构建的提示词</returns>
    private string BuildCustomPrompt<T>(string triggerValue, string customTemplate) where T : class
    {
        try
        {
            return string.Format(customTemplate, triggerValue);
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

        return promptBuilder.ToString();
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
}
