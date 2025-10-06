using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM.Processors;

/// <summary>
/// 默认LLM JSON响应处理器实现
/// </summary>
public class DefaultLLMJsonProcessor : ILLMJsonProcessor
{
    private readonly ILogger<DefaultLLMJsonProcessor> _logger;

    /// <summary>
    /// 初始化默认LLM JSON处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultLLMJsonProcessor(ILogger<DefaultLLMJsonProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 解析结构化响应
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="aiResponse">AI响应内容</param>
    /// <returns>解析结果</returns>
    public Task<JsonProcessingResult<T>> ParseStructuredResponseAsync<T>(string aiResponse) where T : class
    {
        try
        {
            _logger.LogDebug("开始解析AI响应，原始响应长度: {Length}", aiResponse?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return Task.FromResult(JsonProcessingResult<T>.Failure("AI响应内容为空", aiResponse ?? ""));
            }

            // 1. 提取JSON内容
            var cleanedJson = ExtractJsonFromResponse(aiResponse);
            _logger.LogDebug("清理后的JSON长度: {Length}", cleanedJson.Length);

            var wasRepaired = false;

            // 2. 验证JSON格式
            if (!ValidateJsonStructure(cleanedJson))
            {
                _logger.LogWarning("AI返回的JSON格式无效，尝试修复");
                var originalLength = cleanedJson.Length;
                cleanedJson = RepairJson(cleanedJson);
                wasRepaired = true;

                _logger.LogDebug("JSON修复尝试完成，原长度: {OriginalLength}，修复后长度: {FixedLength}",
                    originalLength, cleanedJson.Length);

                if (!ValidateJsonStructure(cleanedJson))
                {
                    _logger.LogError("AI返回的JSON格式无效且无法修复");
                    return Task.FromResult(JsonProcessingResult<T>.Failure("AI返回的JSON格式无效且无法修复", aiResponse));
                }

                _logger.LogInformation("JSON修复成功，继续解析");
            }

            // 3. 反序列化为目标类型
            var result = JsonSerializer.Deserialize<T>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (result == null)
            {
                return Task.FromResult(JsonProcessingResult<T>.Failure("JSON反序列化结果为null", aiResponse));
            }

            _logger.LogDebug("JSON解析成功，类型: {Type}", typeof(T).Name);
            return Task.FromResult(JsonProcessingResult<T>.Success(result, aiResponse, cleanedJson, wasRepaired));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON解析失败: {Message}", ex.Message);
            return Task.FromResult(JsonProcessingResult<T>.Failure($"JSON解析失败: {ex.Message}", aiResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析AI响应时发生未知错误: {Message}", ex.Message);
            return Task.FromResult(JsonProcessingResult<T>.Failure($"解析失败: {ex.Message}", aiResponse));
        }
    }

    /// <summary>
    /// 从AI响应中提取JSON内容
    /// </summary>
    /// <param name="aiResponse">AI响应内容</param>
    /// <returns>清理后的JSON字符串</returns>
    public string ExtractJsonFromResponse(string aiResponse)
    {
        if (string.IsNullOrEmpty(aiResponse))
        {
            throw new ArgumentException("AI响应内容为空", nameof(aiResponse));
        }

        _logger.LogDebug("开始提取JSON，原始响应长度: {Length}", aiResponse.Length);

        // 移除可能的Markdown代码块标记
        var cleaned = aiResponse.Trim();

        // 如果以```json开头，移除代码块标记
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7); // 移除```json
            var codeBlockEndIndex = cleaned.LastIndexOf("```");
            if (codeBlockEndIndex > 0)
            {
                cleaned = cleaned.Substring(0, codeBlockEndIndex);
            }
            _logger.LogDebug("移除```json代码块标记");
        }
        // 如果以```开头，移除代码块标记
        else if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > 0)
            {
                cleaned = cleaned.Substring(firstNewline + 1);
            }
            var codeBlockEndIndex = cleaned.LastIndexOf("```");
            if (codeBlockEndIndex > 0)
            {
                cleaned = cleaned.Substring(0, codeBlockEndIndex);
            }
            _logger.LogDebug("移除```代码块标记");
        }

        // 查找第一个{和最后一个}，提取JSON部分
        var startIndex = cleaned.IndexOf('{');
        var endIndex = cleaned.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
            _logger.LogDebug("提取JSON片段，起始位置: {Start}，结束位置: {End}", startIndex, endIndex);
        }
        else
        {
            _logger.LogWarning("未找到有效的JSON边界，起始位置: {Start}，结束位置: {End}", startIndex, endIndex);
        }

        var result = cleaned.Trim();
        _logger.LogDebug("JSON提取完成，最终长度: {Length}", result.Length);

        return result;
    }

    /// <summary>
    /// 修复损坏的JSON
    /// </summary>
    /// <param name="brokenJson">损坏的JSON字符串</param>
    /// <returns>修复后的JSON字符串</returns>
    public string RepairJson(string brokenJson)
    {
        if (string.IsNullOrWhiteSpace(brokenJson))
        {
            return brokenJson;
        }

        try
        {
            _logger.LogDebug("开始JSON修复，原始长度: {Length}", brokenJson.Length);

            var fixedJson = brokenJson.Trim();

            // 1. 处理截断的JSON
            fixedJson = HandleTruncatedJson(fixedJson);

            // 2. 移除可能的非JSON前缀文本
            var jsonStart = fixedJson.IndexOf('{');
            if (jsonStart > 0)
            {
                fixedJson = fixedJson.Substring(jsonStart);
                _logger.LogDebug("移除JSON前缀文本，新起始位置: {Start}", jsonStart);
            }

            // 3. 查找最后一个完整的对象或数组结束位置
            var lastValidEnd = FindLastValidJsonEnd(fixedJson);
            if (lastValidEnd > 0 && lastValidEnd < fixedJson.Length - 1)
            {
                fixedJson = fixedJson.Substring(0, lastValidEnd + 1);
                _logger.LogDebug("截取到最后有效JSON位置: {End}", lastValidEnd);
            }

            // 4. 检查并修复括号平衡
            fixedJson = BalanceBrackets(fixedJson);

            // 5. 移除可能的尾随逗号
            var beforeCommaRemoval = fixedJson;
            fixedJson = Regex.Replace(fixedJson, @",(\s*[}\]])", "$1");
            if (fixedJson != beforeCommaRemoval)
            {
                _logger.LogDebug("移除了尾随逗号");
            }

            // 6. 修复可能的字符串引号问题
            fixedJson = FixStringQuotes(fixedJson);

            // 7. 如果仍然无效，尝试提取部分有效的JSON
            if (!ValidateJsonStructure(fixedJson))
            {
                fixedJson = ExtractPartialValidJson(fixedJson);
            }

            _logger.LogDebug("JSON修复完成，原长度: {OriginalLength}，修复后长度: {FixedLength}",
                brokenJson.Length, fixedJson.Length);

            return fixedJson;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JSON修复失败");
            return brokenJson; // 返回原始JSON
        }
    }

    /// <summary>
    /// 验证JSON格式是否有效
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>是否有效</returns>
    public bool ValidateJsonStructure(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 处理截断的JSON
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>处理后的JSON字符串</returns>
    private string HandleTruncatedJson(string json)
    {
        try
        {
            var trimmed = json.TrimEnd();
            _logger.LogDebug("检查JSON截断，原始长度: {Length}", trimmed.Length);

            // 常见的截断模式
            var truncationPatterns = new[]
            {
                "...",           // 省略号
                "，存在明显错别字",  // 从用户提供的例子看到的截断
                "存在明显错别字",
                "\"errors\": [",  // 数组开始但未完成
                "\"corrections\":", // 字段开始但未完成
                "],",            // 数组结束后有多余逗号
                "},",            // 对象结束后有多余逗号
                ",\"",           // 字段分隔符后截断
                "\"",            // 单独的引号
            };

            foreach (var pattern in truncationPatterns)
            {
                if (trimmed.EndsWith(pattern))
                {
                    _logger.LogDebug("检测到截断模式: {Pattern}", pattern);

                    // 根据截断位置尝试修复
                    if (pattern == "..." || pattern.Contains("错别字"))
                    {
                        // 如果是在字符串值中截断，尝试闭合字符串
                        var lastQuoteIndex = trimmed.LastIndexOf('"');
                        var secondLastQuoteIndex = trimmed.LastIndexOf('"', lastQuoteIndex - 1);

                        if (lastQuoteIndex > secondLastQuoteIndex && secondLastQuoteIndex >= 0)
                        {
                            // 在字符串中截断，移除截断内容并添加闭合引号
                            var beforeTruncation = trimmed.Substring(0, lastQuoteIndex);
                            trimmed = beforeTruncation + "\"";
                            _logger.LogDebug("修复字符串截断，移除截断内容");
                        }
                    }
                    else if (pattern.Contains("["))
                    {
                        // 数组开始但未完成，添加空数组结束
                        trimmed += "]";
                        _logger.LogDebug("修复数组截断");
                    }
                    else if (pattern.Contains(":"))
                    {
                        // 字段开始但未完成，添加空值
                        trimmed += "null";
                        _logger.LogDebug("修复字段截断");
                    }
                    else if (pattern == "]," || pattern == "},")
                    {
                        // 数组或对象结束后有多余逗号，移除逗号
                        trimmed = trimmed.Substring(0, trimmed.Length - 1); // 移除最后的逗号
                        _logger.LogDebug("移除多余的逗号: {Pattern}", pattern);
                    }
                    else if (pattern == ",\"")
                    {
                        // 字段分隔符后截断，移除不完整的字段
                        var lastCommaIndex = trimmed.LastIndexOf(",\"");
                        if (lastCommaIndex >= 0)
                        {
                            trimmed = trimmed.Substring(0, lastCommaIndex);
                            _logger.LogDebug("移除不完整的字段");
                        }
                    }

                    break;
                }
            }

            // 检查是否在字符串中间截断（没有配对的引号）
            var quoteCount = trimmed.Count(c => c == '"');
            if (quoteCount % 2 != 0)
            {
                _logger.LogDebug("检测到奇数个引号，可能在字符串中截断");

                // 奇数个引号，可能在字符串中截断
                var lastQuoteIndex = trimmed.LastIndexOf('"');
                if (lastQuoteIndex >= 0)
                {
                    // 查找这个引号是否是字段名还是字符串值
                    var colonAfterQuote = trimmed.IndexOf(':', lastQuoteIndex);

                    if (colonAfterQuote < 0) // 没有冒号，可能是字符串值截断
                    {
                        // 查找前一个完整的字段结束位置
                        var beforeQuote = trimmed.Substring(0, lastQuoteIndex);
                        var lastCompleteField = Math.Max(beforeQuote.LastIndexOf(','), beforeQuote.LastIndexOf('{'));

                        if (lastCompleteField >= 0)
                        {
                            // 截断到最后一个完整字段
                            trimmed = trimmed.Substring(0, lastCompleteField);
                            _logger.LogDebug("截断到最后完整字段位置: {Position}", lastCompleteField);
                        }
                        else
                        {
                            // 添加闭合引号
                            trimmed += "\"";
                            _logger.LogDebug("添加闭合引号");
                        }
                    }
                }
            }

            return trimmed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "处理截断JSON失败");
            return json;
        }
    }

    /// <summary>
    /// 查找最后一个有效的JSON结束位置
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>最后有效位置索引</returns>
    private int FindLastValidJsonEnd(string json)
    {
        var braceCount = 0;
        var bracketCount = 0;
        var inString = false;
        var escapeNext = false;

        for (int i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            switch (c)
            {
                case '{':
                    braceCount++;
                    break;
                case '}':
                    braceCount--;
                    if (braceCount == 0 && bracketCount == 0)
                    {
                        return i; // 找到完整的JSON对象结束位置
                    }
                    break;
                case '[':
                    bracketCount++;
                    break;
                case ']':
                    bracketCount--;
                    break;
            }
        }

        return -1; // 没有找到完整的结束位置
    }

    /// <summary>
    /// 平衡括号
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>平衡后的JSON字符串</returns>
    private string BalanceBrackets(string json)
    {
        var openBraces = json.Count(c => c == '{');
        var closeBraces = json.Count(c => c == '}');
        var openBrackets = json.Count(c => c == '[');
        var closeBrackets = json.Count(c => c == ']');

        var result = json;

        // 补充缺少的结尾大括号
        while (openBraces > closeBraces)
        {
            result += "}";
            closeBraces++;
        }

        // 补充缺少的结尾方括号
        while (openBrackets > closeBrackets)
        {
            result += "]";
            closeBrackets++;
        }

        return result;
    }

    /// <summary>
    /// 修复字符串引号问题
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>修复后的JSON字符串</returns>
    private string FixStringQuotes(string json)
    {
        try
        {
            var result = json;

            // 1. 修复未闭合的字符串引号
            result = FixUnclosedQuotes(result);

            // 2. 修复字段名缺少引号的问题
            result = FixUnquotedFieldNames(result);

            _logger.LogDebug("字符串引号修复完成");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "字符串引号修复失败");
            return json;
        }
    }

    /// <summary>
    /// 修复未闭合的字符串引号
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>修复后的JSON字符串</returns>
    private string FixUnclosedQuotes(string json)
    {
        var result = new System.Text.StringBuilder(json);
        var inString = false;
        var escapeNext = false;
        var lastQuoteIndex = -1;

        for (int i = 0; i < result.Length; i++)
        {
            var c = result[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                if (inString)
                {
                    inString = false;
                }
                else
                {
                    inString = true;
                    lastQuoteIndex = i;
                }
            }
        }

        // 如果字符串没有闭合，在适当位置添加闭合引号
        if (inString && lastQuoteIndex >= 0)
        {
            // 查找下一个逗号、大括号或方括号作为字符串结束位置
            for (int i = lastQuoteIndex + 1; i < result.Length; i++)
            {
                var c = result[i];
                if (c == ',' || c == '}' || c == ']' || c == '\n' || c == '\r')
                {
                    result.Insert(i, '"');
                    _logger.LogDebug("在位置 {Position} 添加闭合引号", i);
                    break;
                }
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 修复字段名缺少引号的问题
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>修复后的JSON字符串</returns>
    private string FixUnquotedFieldNames(string json)
    {
        // 使用正则表达式查找并修复未加引号的字段名
        var pattern = @"(\s*)([a-zA-Z_][a-zA-Z0-9_]*)\s*:";
        var replacement = "$1\"$2\":";

        var result = Regex.Replace(json, pattern, replacement);

        if (result != json)
        {
            _logger.LogDebug("修复了未加引号的字段名");
        }

        return result;
    }

    /// <summary>
    /// 提取部分有效的JSON
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>部分有效的JSON字符串</returns>
    private string ExtractPartialValidJson(string json)
    {
        try
        {
            _logger.LogDebug("尝试提取部分有效JSON，原始长度: {Length}", json.Length);

            // 策略1: 尝试提取完整的results数组
            var partialJson = TryExtractResultsArray(json);
            if (!string.IsNullOrEmpty(partialJson) && ValidateJsonStructure(partialJson))
            {
                _logger.LogDebug("成功提取完整的results数组");
                return partialJson;
            }

            // 策略2: 尝试提取单个有效的result对象
            partialJson = TryExtractSingleResults(json);
            if (!string.IsNullOrEmpty(partialJson) && ValidateJsonStructure(partialJson))
            {
                _logger.LogDebug("成功提取单个result对象");
                return partialJson;
            }

            // 最后的降级策略：返回空结构
            _logger.LogDebug("无法提取有效JSON，返回空结构");
            return "{\"results\":[]}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取部分JSON失败");
            return "{\"results\":[]}";
        }
    }

    /// <summary>
    /// 尝试提取results数组
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>提取的JSON字符串</returns>
    private string? TryExtractResultsArray(string json)
    {
        try
        {
            var resultsStart = json.IndexOf("\"results\"");
            if (resultsStart < 0) return null;

            var colonIndex = json.IndexOf(':', resultsStart);
            if (colonIndex < 0) return null;

            var arrayStart = json.IndexOf('[', colonIndex);
            if (arrayStart < 0) return null;

            // 查找匹配的数组结束位置
            var bracketCount = 0;
            var inString = false;
            var escapeNext = false;
            var arrayEnd = -1;

            for (int i = arrayStart; i < json.Length; i++)
            {
                var c = json[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == '[')
                {
                    bracketCount++;
                }
                else if (c == ']')
                {
                    bracketCount--;
                    if (bracketCount == 0)
                    {
                        arrayEnd = i;
                        break;
                    }
                }
            }

            if (arrayEnd > arrayStart)
            {
                var arrayContent = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                return "{\"results\":" + arrayContent + "}";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 尝试提取单个result对象
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>提取的JSON字符串</returns>
    private string? TryExtractSingleResults(string json)
    {
        try
        {
            var results = new List<string>();
            var questionIndexPattern = @"""questionIndex""\s*:\s*(\d+)";
            var matches = Regex.Matches(json, questionIndexPattern);

            _logger.LogDebug("在JSON中找到 {Count} 个questionIndex匹配项", matches.Count);

            foreach (Match match in matches)
            {
                var startPos = match.Index;
                var questionIndex = match.Groups[1].Value;

                // 向前查找对象开始
                var objStart = json.LastIndexOf('{', startPos);
                if (objStart < 0)
                {
                    _logger.LogDebug("questionIndex {Index} 找不到对象开始位置", questionIndex);
                    continue;
                }

                // 向后查找对象结束
                var braceCount = 0;
                var inString = false;
                var escapeNext = false;
                var objEnd = -1;

                for (int i = objStart; i < json.Length; i++)
                {
                    var c = json[i];

                    if (escapeNext)
                    {
                        escapeNext = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escapeNext = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = !inString;
                        continue;
                    }

                    if (inString) continue;

                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            objEnd = i;
                            break;
                        }
                    }
                }

                if (objEnd > objStart)
                {
                    var objContent = json.Substring(objStart, objEnd - objStart + 1);
                    results.Add(objContent);
                    _logger.LogDebug("成功提取questionIndex {Index} 的对象，长度: {Length}", questionIndex, objContent.Length);
                }
                else
                {
                    _logger.LogDebug("questionIndex {Index} 找不到对象结束位置", questionIndex);
                }
            }

            if (results.Count > 0)
            {
                var finalJson = "{\"results\":[" + string.Join(",", results) + "]}";
                _logger.LogDebug("成功提取 {Count} 个result对象，总长度: {Length}", results.Count, finalJson.Length);
                return finalJson;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取单个result对象失败");
            return null;
        }
    }
}
