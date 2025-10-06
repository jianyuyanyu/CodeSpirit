using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodeSpirit.LLM.Prompts;

/// <summary>
/// 默认LLM提示词模板管理器实现
/// </summary>
public class DefaultLLMPromptTemplateManager : ILLMPromptTemplateManager
{
    private readonly ConcurrentDictionary<string, string> _templates = new();
    private readonly ILogger<DefaultLLMPromptTemplateManager> _logger;

    /// <summary>
    /// 初始化默认LLM提示词模板管理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultLLMPromptTemplateManager(ILogger<DefaultLLMPromptTemplateManager> logger)
    {
        _logger = logger;
        RegisterBuiltInTemplates();
    }

    /// <summary>
    /// 注册模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <param name="template">模板内容</param>
    public void RegisterTemplate(string name, string template)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("模板名称不能为空", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("模板内容不能为空", nameof(template));
        }

        _templates.AddOrUpdate(name, template, (key, oldValue) => template);
        _logger.LogDebug("注册模板: {TemplateName}", name);
    }

    /// <summary>
    /// 获取模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <returns>模板内容</returns>
    public string GetTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("模板名称不能为空", nameof(name));
        }

        if (_templates.TryGetValue(name, out var template))
        {
            return template;
        }

        throw new ArgumentException($"模板 '{name}' 不存在", nameof(name));
    }

    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <param name="parameters">模板参数</param>
    /// <returns>渲染后的内容</returns>
    public string RenderTemplate(string name, object parameters)
    {
        var template = GetTemplate(name);
        return RenderTemplateContent(template, parameters);
    }

    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <returns>是否存在</returns>
    public bool HasTemplate(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && _templates.ContainsKey(name);
    }

    /// <summary>
    /// 获取所有模板名称
    /// </summary>
    /// <returns>模板名称列表</returns>
    public IEnumerable<string> GetTemplateNames()
    {
        return _templates.Keys.ToList();
    }

    /// <summary>
    /// 渲染模板内容
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="parameters">参数对象</param>
    /// <returns>渲染后的内容</returns>
    private string RenderTemplateContent(string template, object parameters)
    {
        try
        {
            if (parameters == null)
            {
                return template;
            }

            var result = template;

            // 将参数对象转换为字典
            var paramDict = ConvertToParameterDictionary(parameters);

            // 使用正则表达式替换模板变量 {{variableName}}
            var pattern = @"\{\{(\w+)\}\}";
            result = Regex.Replace(result, pattern, match =>
            {
                var variableName = match.Groups[1].Value;
                if (paramDict.TryGetValue(variableName, out var value))
                {
                    return FormatParameterValue(value);
                }

                _logger.LogWarning("模板变量 '{VariableName}' 未找到对应的参数值", variableName);
                return match.Value; // 保持原样
            });

            // 处理条件语句 {{#if condition}}...{{/if}}
            result = ProcessConditionalBlocks(result, paramDict);

            // 处理循环语句 {{#each items}}...{{/each}}
            result = ProcessLoopBlocks(result, paramDict);

            _logger.LogDebug("模板渲染完成，原长度: {OriginalLength}，渲染后长度: {RenderedLength}",
                template.Length, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染模板失败");
            throw new InvalidOperationException("渲染模板失败", ex);
        }
    }

    /// <summary>
    /// 将参数对象转换为字典
    /// </summary>
    /// <param name="parameters">参数对象</param>
    /// <returns>参数字典</returns>
    private Dictionary<string, object?> ConvertToParameterDictionary(object parameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (parameters is Dictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            // 使用反射获取对象属性
            var properties = parameters.GetType().GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(parameters);
                    result[property.Name] = value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取属性 {PropertyName} 的值失败", property.Name);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 格式化参数值
    /// </summary>
    /// <param name="value">参数值</param>
    /// <returns>格式化后的字符串</returns>
    private string FormatParameterValue(object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is string str)
        {
            return str;
        }

        if (value is bool boolean)
        {
            return boolean.ToString().ToLowerInvariant();
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (value is IEnumerable<object> enumerable && !(value is string))
        {
            try
            {
                var items = enumerable.ToList();
                if (items.Count == 0)
                {
                    return string.Empty;
                }

                // 如果是简单类型的集合，用逗号分隔
                if (items.All(item => item is string || item.GetType().IsPrimitive))
                {
                    return string.Join(", ", items);
                }

                // 复杂对象集合，序列化为JSON
                return JsonConvert.SerializeObject(items, Formatting.Indented);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "格式化集合参数失败");
                return value.ToString() ?? string.Empty;
            }
        }

        // 复杂对象，序列化为JSON
        if (value.GetType().IsClass && value.GetType() != typeof(string))
        {
            try
            {
                return JsonConvert.SerializeObject(value, Formatting.Indented);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "序列化对象参数失败");
                return value.ToString() ?? string.Empty;
            }
        }

        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 处理条件语句块
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>处理后的模板</returns>
    private string ProcessConditionalBlocks(string template, Dictionary<string, object?> parameters)
    {
        var pattern = @"\{\{#if\s+(\w+)\}\}(.*?)\{\{/if\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var conditionName = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            if (parameters.TryGetValue(conditionName, out var value))
            {
                var isTrue = value switch
                {
                    bool b => b,
                    string s => !string.IsNullOrWhiteSpace(s),
                    int i => i != 0,
                    null => false,
                    _ => true
                };

                return isTrue ? content : string.Empty;
            }

            return string.Empty;
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// 处理循环语句块
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>处理后的模板</returns>
    private string ProcessLoopBlocks(string template, Dictionary<string, object?> parameters)
    {
        var pattern = @"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var collectionName = match.Groups[1].Value;
            var itemTemplate = match.Groups[2].Value;

            if (parameters.TryGetValue(collectionName, out var value) && 
                value is IEnumerable<object> collection)
            {
                var result = new List<string>();
                var index = 0;

                foreach (var item in collection)
                {
                    var itemContent = itemTemplate;
                    
                    // 替换 {{this}} 为当前项
                    itemContent = itemContent.Replace("{{this}}", FormatParameterValue(item));
                    
                    // 替换 {{@index}} 为当前索引
                    itemContent = itemContent.Replace("{{@index}}", index.ToString());

                    // 如果项是复杂对象，处理其属性
                    if (item != null && item.GetType().IsClass && item.GetType() != typeof(string))
                    {
                        var itemParams = ConvertToParameterDictionary(item);
                        foreach (var kvp in itemParams)
                        {
                            var placeholder = $"{{{{{kvp.Key}}}}}";
                            itemContent = itemContent.Replace(placeholder, FormatParameterValue(kvp.Value));
                        }
                    }

                    result.Add(itemContent);
                    index++;
                }

                return string.Join(Environment.NewLine, result);
            }

            return string.Empty;
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// 注册内置模板
    /// </summary>
    private void RegisterBuiltInTemplates()
    {
        // 题目批量审核模板
        RegisterTemplate("question_batch_audit", @"请批量审核以下 {{questions.Count}} 道题目的格式和内容，检查是否存在错误：

审核标准：
1. 题目内容不应包含序号、分值、选项、答案，应完整、清晰
2. 选项格式不应包含序号、ABCD等标记，多个选项应使用逗号分隔，选项不应重复
3. 正确答案是否与选项匹配，是否合理；如果正确答案使用序号或ABCD等标记，应修正为选项文本
4. 是否存在错别字或标点符号错误
5. 解析是否合理

{{#if autoCorrect}}如果发现错误，请自动修正并说明修正内容。

【严格修正规则 - 必须遵守】：
1. 【题目内容修正】：
   - 只能删除：明显的序号（如""1.""、""（1）""、""第1题""等）、分值标记（如""(5分)""）
   - 只能修正：明显的错别字和标点符号错误
   - 绝对禁止：删除题目的引导语（如""下列说法正确的是""、""以下哪项是""等）
   - 绝对禁止：修改题目的核心语义和表达方式
   - 绝对禁止：补充任何原题目中没有的内容

2. 【选项修正】：
   - 只能删除：选项前的序号标记（如""A.""、""①""等）
   - 只能修正：明显的错别字
   - 绝对禁止：修改选项的事实内容和语义
   - 绝对禁止：调整选项的逻辑关系

3. 【答案修正】：
   - 只能修正：将序号答案（如""A""、""①""）转换为对应的选项文本
   - 绝对禁止：修改答案的实际内容

【修正示例】：
✅ 正确修正：""1. 下列说法正确的是"" → ""下列说法正确的是""（删除序号）
❌ 错误修正：""下列说法正确的是"" → ""关于计算机的说法，正确的是""（修改语义）
✅ 正确修正：""A. 数据总线决定速度"" → ""数据总线决定速度""（删除选项标记）
❌ 错误修正：""PII/300比PII/350时钟频率高"" → ""PII/300的时钟频率低于PII/350""（修改事实）{{/if}}{{#if !autoCorrect}}如果发现错误，请指出错误但不要修正。{{/if}}

题目列表：
{{#each questions}}
题目 {{@index}}:
- 内容：{{Content}}
- 类型：{{Type}}
- 选项：{{Options}}
- 正确答案：{{CorrectAnswer}}
- 难度：{{Difficulty}}
- 解析：{{Analysis}}
- 标签：{{Tags}}
{{/each}}");

        // 单个题目审核模板
        RegisterTemplate("question_single_audit", @"请审核以下题目的格式和内容，检查是否存在错误：

题目内容：{{Content}}
题目类型：{{Type}}
选项：{{Options}}
正确答案：{{CorrectAnswer}}
难度：{{Difficulty}}
解析：{{Analysis}}
标签：{{Tags}}

请检查：
1. 题目内容不应包含序号、分值、选项、答案，应完整、清晰
2. 选项格式不应包含序号、ABCD等标记，多个选项应使用逗号分隔，选项不应重复
3. 正确答案是否与选项匹配，是否合理；如果正确答案使用序号或ABCD等标记，应修正为选项文本
4. 是否存在错别字或标点符号错误
5. 解析是否合理

{{#if autoCorrect}}如果发现错误，请自动修正并说明修正内容。{{/if}}{{#if !autoCorrect}}如果发现错误，请指出错误但不要修正。{{/if}}");

        _logger.LogInformation("已注册 {Count} 个内置模板", _templates.Count);
    }
}
