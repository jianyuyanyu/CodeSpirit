using CodeSpirit.ExamApi.Dtos.Question;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodeSpirit.ExamApi.Services.Helpers;

/// <summary>
/// 默认题目解析器实现
/// </summary>
public class DefaultQuestionParser : IQuestionParser
{
    private readonly ILogger<DefaultQuestionParser> _logger;

    /// <summary>
    /// 初始化默认题目解析器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultQuestionParser(ILogger<DefaultQuestionParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public List<CreateQuestionDto> ParseQuestions(string content, AIGenerateQuestionDto request)
    {
        _logger.LogDebug("开始解析生成的题目，输入长度: {Length} 字符", content.Length);
        List<CreateQuestionDto> result = new();

        try
        {
            // 清理特殊字符
            content = CleanJsonString(content);

            // 尝试提取JSON部分
            content = ExtractJsonFromText(content);

            // 解析JSON
            _logger.LogDebug("尝试解析JSON");
            JsonElement jsonElement;
            try
            {
                jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                _logger.LogDebug("JSON解析成功");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON解析失败，尝试提取有效JSON部分: {Json}", content);

                // 尝试提取有效的JSON部分
                var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}");
                if (jsonMatch.Success)
                {
                    string extractedJson = jsonMatch.Value;
                    _logger.LogInformation("提取到JSON文本: {Json}",
                        extractedJson.Length > 100 ? extractedJson.Substring(0, 100) + "..." : extractedJson);
                    try
                    {
                        jsonElement = JsonSerializer.Deserialize<JsonElement>(extractedJson);
                        _logger.LogDebug("从提取的文本中成功解析JSON");
                    }
                    catch (JsonException innerEx)
                    {
                        _logger.LogError(innerEx, "二次JSON解析失败，需要在下一轮请求中向LLM提供更明确的格式指导");
                        throw new FormatException("无法解析生成的内容为有效JSON", innerEx);
                    }
                }
                else
                {
                    _logger.LogError("无法提取有效的JSON，需要向LLM请求完全重新生成并严格遵循JSON格式");
                    throw new FormatException("响应内容不包含有效的JSON", ex);
                }
            }

            // 尝试从不同路径查找questions数组
            JsonElement questionsArray;
            bool foundQuestionsArray = false;

            if (jsonElement.TryGetProperty("questions", out questionsArray))
            {
                _logger.LogDebug("在根路径找到questions数组");
                foundQuestionsArray = true;
            }
            else if (jsonElement.TryGetProperty("data", out var data) &&
                     data.TryGetProperty("questions", out questionsArray))
            {
                _logger.LogDebug("在data路径找到questions数组");
                foundQuestionsArray = true;
            }
            else if (jsonElement.TryGetProperty("result", out var resultData) &&
                     resultData.ValueKind == JsonValueKind.Object &&
                     resultData.TryGetProperty("questions", out questionsArray))
            {
                _logger.LogDebug("在result路径找到questions数组");
                foundQuestionsArray = true;
            }
            else if (jsonElement.TryGetProperty("output", out var output) &&
                     output.TryGetProperty("questions", out questionsArray))
            {
                _logger.LogDebug("在output路径找到questions数组");
                foundQuestionsArray = true;
            }

            // 如果找不到questions数组，检查是否为单个题目对象
            if (!foundQuestionsArray)
            {
                _logger.LogWarning("未找到questions数组，尝试检查是否为单个题目对象");

                // 检查是否有基本题目属性
                bool isSingleQuestion = jsonElement.TryGetProperty("content", out _) &&
                                       (jsonElement.TryGetProperty("options", out _) ||
                                        request.Type == Data.Models.Enums.QuestionType.TrueFalse) &&
                                       jsonElement.TryGetProperty("correctAnswer", out _);

                if (isSingleQuestion)
                {
                    _logger.LogInformation("检测到单个题目对象，创建虚拟questions数组");
                    var tempArray = new[] { jsonElement };
                    result.AddRange(ProcessQuestionObjects(tempArray, request));
                    return result;
                }
                else
                {
                    // 尝试按照整个响应是数组的情况处理
                    _logger.LogWarning("未找到标准题目结构，尝试将整个JSON作为题目数组处理");
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("根元素是数组，尝试直接作为题目数组处理");
                        result.AddRange(ProcessQuestionObjects(jsonElement.EnumerateArray(), request));
                        return result;
                    }
                    else
                    {
                        _logger.LogError("无法识别题目格式: {Json}", content);
                        throw new FormatException("无法找到题目数据");
                    }
                }
            }

            _logger.LogDebug("处理questions数组中的题目");
            result.AddRange(ProcessQuestionObjects(questionsArray.EnumerateArray(), request));

            _logger.LogInformation("成功解析 {Count} 道题目", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析生成的题目时发生错误: {ErrorMessage}", ex.Message);

            // 尝试直接解析原始JSON文本
            try
            {
                // 尝试提取JSON部分
                var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}");
                if (jsonMatch.Success)
                {
                    var extractedJson = jsonMatch.Value;
                    _logger.LogInformation("提取到JSON文本: {ExtractedJson}",
                        extractedJson.Length > 100 ? extractedJson.Substring(0, 100) + "..." : extractedJson);

                    // 再次尝试解析
                    return ParseQuestions(extractedJson, request);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "二次解析生成的题目时发生错误");
            }

            throw new AppServiceException(400, "解析生成的题目时发生错误");
        }
    }

    /// <summary>
    /// 处理题目对象集合
    /// </summary>
    private List<CreateQuestionDto> ProcessQuestionObjects(IEnumerable<JsonElement> questions, AIGenerateQuestionDto request)
    {
        List<CreateQuestionDto> results = new List<CreateQuestionDto>();
        int questionIndex = 0;

        foreach (var question in questions)
        {
            questionIndex++;
            _logger.LogDebug("处理第 {Index} 个题目", questionIndex);
            try
            {
                var dto = new CreateQuestionDto
                {
                    Type = request.Type,
                    Difficulty = request.Difficulty,
                    CategoryId = request.CategoryId,
                    DefaultScore = 10 // 默认分值
                };

                // 设置题目内容
                if (question.TryGetProperty("content", out JsonElement content))
                {
                    dto.Content = content.GetString() ?? string.Empty;
                    _logger.LogDebug("题目内容: {Content}",
                        dto.Content.Length > 50 ? dto.Content.Substring(0, 50) + "..." : dto.Content);
                }
                else
                {
                    _logger.LogWarning("题目缺少content字段");
                    continue; // 跳过没有内容的题目
                }

                // 设置选项
                dto.Options = new List<string>();
                if (request.Type != Data.Models.Enums.QuestionType.TrueFalse &&
                    question.TryGetProperty("options", out JsonElement options))
                {
                    if (options.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var option in options.EnumerateArray())
                        {
                            dto.Options.Add(option.GetString() ?? string.Empty);
                        }
                        _logger.LogDebug("题目选项数量: {Count}", dto.Options.Count);

                        // 验证单选题必须有4个选项
                        if (request.Type == Data.Models.Enums.QuestionType.SingleChoice && dto.Options.Count != 4)
                        {
                            _logger.LogWarning("解析出的单选题选项数量不为4个: {Count}", dto.Options.Count);

                            // 只有当选项已存在且数量小于4个时才补充
                            if (dto.Options.Count > 0 && dto.Options.Count < 4)
                            {
                                _logger.LogWarning("选项数量不足4个，进行补充");
                                while (dto.Options.Count < 4)
                                {
                                    dto.Options.Add($"选项{(char)('A' + dto.Options.Count)}（自动补充）");
                                }
                            }
                            // 如果选项多于4个，截取前4个
                            else if (dto.Options.Count > 4)
                            {
                                _logger.LogWarning("单选题选项超过4个，截取前4个选项");
                                dto.Options = dto.Options.Take(4).ToList();
                            }
                        }
                    }
                    else if (options.ValueKind == JsonValueKind.String)
                    {
                        // 有时模型可能输出选项字符串而不是数组
                        string optionsText = options.GetString() ?? string.Empty;
                        var optionsList = ParseOptionsFromText(optionsText);
                        dto.Options.AddRange(optionsList);
                        _logger.LogDebug("从文本解析选项: {Count} 个", optionsList.Count);

                        // 验证单选题必须有4个选项
                        if (request.Type == Data.Models.Enums.QuestionType.SingleChoice && dto.Options.Count != 4)
                        {
                            if (dto.Options.Count > 0 && dto.Options.Count < 4)
                            {
                                _logger.LogWarning("选项数量不足4个，进行补充");
                                while (dto.Options.Count < 4)
                                {
                                    dto.Options.Add($"选项{(char)('A' + dto.Options.Count)}（自动补充）");
                                }
                            }
                            else if (dto.Options.Count > 4)
                            {
                                _logger.LogWarning("单选题选项超过4个，截取前4个选项");
                                dto.Options = dto.Options.Take(4).ToList();
                            }
                        }
                    }
                }

                // 设置正确答案
                if (question.TryGetProperty("correctAnswer", out JsonElement correctAnswer))
                {
                    string answer = correctAnswer.GetString() ?? string.Empty;
                    _logger.LogDebug("原始正确答案: {Answer}", answer);

                    // 处理不同类型题目的答案格式
                    switch (request.Type)
                    {
                        case Data.Models.Enums.QuestionType.SingleChoice:
                            // 如果返回的是完整选项文本，转换为选项字母
                            if (answer.Length > 1 && dto.Options.Any() &&
                               dto.Options.Any(opt => opt.Contains(answer) || answer.Contains(opt)))
                            {
                                // 查找匹配的选项索引
                                for (int i = 0; i < dto.Options.Count; i++)
                                {
                                    if (dto.Options[i].Contains(answer) || answer.Contains(dto.Options[i]))
                                    {
                                        answer = ((char)('A' + i)).ToString();
                                        _logger.LogDebug("单选题答案转换为: {Answer}", answer);
                                        break;
                                    }
                                }
                            }
                            break;

                        case Data.Models.Enums.QuestionType.MultipleChoice:
                            // 多选题，如果返回的是完整选项文本数组，转换为选项字母列表
                            if (answer.Length > 3 && !answer.All(c => c == ',' || c == ' ' || (c >= 'A' && c <= 'Z')))
                            {
                                var selectedOptions = answer.Split(',', '、', '；', ';').Select(opt => opt.Trim());
                                var letterAnswers = new List<string>();

                                foreach (var selectedOption in selectedOptions)
                                {
                                    for (int i = 0; i < dto.Options.Count; i++)
                                    {
                                        if (dto.Options[i].Contains(selectedOption) || selectedOption.Contains(dto.Options[i]))
                                        {
                                            letterAnswers.Add(((char)('A' + i)).ToString());
                                            break;
                                        }
                                    }
                                }

                                if (letterAnswers.Any())
                                {
                                    answer = string.Join(",", letterAnswers);
                                    _logger.LogDebug("多选题答案转换为: {Answer}", answer);
                                }
                            }
                            break;

                        case Data.Models.Enums.QuestionType.TrueFalse:
                            // 确保判断题答案格式为True/False
                            if (!string.Equals(answer, "True", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(answer, "False", StringComparison.OrdinalIgnoreCase))
                            {
                                bool isTrue = answer.ToLower().Contains("正确") ||
                                             answer.ToLower().Contains("true") ||
                                             answer == "√" ||
                                             answer == "对" ||
                                             answer == "是" ||
                                             answer == "1" ||
                                             answer == "T";

                                answer = isTrue ? "True" : "False";
                                _logger.LogDebug("判断题答案标准化为: {Answer}", answer);
                            }
                            break;
                    }

                    dto.CorrectAnswer = answer;
                }
                else
                {
                    _logger.LogWarning("题目缺少correctAnswer字段");
                }

                // 设置解析
                if (question.TryGetProperty("analysis", out JsonElement analysis))
                {
                    dto.Analysis = analysis.GetString();
                    _logger.LogDebug("题目解析: {Analysis}",
                        dto.Analysis != null && dto.Analysis.Length > 50
                            ? dto.Analysis.Substring(0, 50) + "..."
                            : dto.Analysis);
                }

                // 设置知识点
                if (question.TryGetProperty("knowledgePoints", out JsonElement knowledgePoints))
                {
                    dto.KnowledgePoints = knowledgePoints.GetString();
                    _logger.LogDebug("题目知识点: {KnowledgePoints}", dto.KnowledgePoints);
                }

                // 设置标签
                dto.Tags = new List<string> { request.Topic };

                // 添加到结果列表中
                results.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理第 {Index} 个题目时出错: {ErrorMessage}", questionIndex, ex.Message);
                // 跳过有问题的题目，继续处理其他题目
            }
        }

        return results;
    }

    /// <summary>
    /// 清理JSON字符串，移除特殊字符
    /// </summary>
    private string CleanJsonString(string json)
    {
        try
        {
            // 移除Markdown代码块标记
            if (json.StartsWith("```json") || json.StartsWith("```"))
            {
                _logger.LogDebug("检测到Markdown代码块，移除代码块标记");
                var parts = json.Split(new[] { "```" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    // 取最长的部分作为JSON
                    json = parts.OrderByDescending(p => p.Length).First().Trim();

                    // 如果开头有"json"，移除它
                    if (json.StartsWith("json"))
                    {
                        json = json.Substring(4).Trim();
                    }
                }
            }

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理JSON字符串时发生错误");
            return json;
        }
    }

    /// <summary>
    /// 从文本中提取JSON部分
    /// </summary>
    private string ExtractJsonFromText(string text)
    {
        try
        {
            // 尝试查找JSON对象
            var jsonObjectMatch = Regex.Match(text, @"\{[\s\S]*\}");
            if (jsonObjectMatch.Success)
            {
                return jsonObjectMatch.Value;
            }

            // 尝试查找JSON数组
            var jsonArrayMatch = Regex.Match(text, @"\[[\s\S]*\]");
            if (jsonArrayMatch.Success)
            {
                return jsonArrayMatch.Value;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从文本提取JSON时发生错误");
            return text;
        }
    }

    /// <summary>
    /// 从文本中解析选项
    /// </summary>
    private List<string> ParseOptionsFromText(string optionsText)
    {
        var options = new List<string>();
        try
        {
            // 尝试按行拆分
            string[] lines = optionsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 如果有多行，每行可能是一个选项
            if (lines.Length > 1)
            {
                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        // 去掉可能的选项前缀 A. B. C. 等
                        if (trimmedLine.Length >= 2 && char.IsLetter(trimmedLine[0]) && trimmedLine[1] == '.')
                        {
                            trimmedLine = trimmedLine.Substring(2).Trim();
                        }
                        options.Add(trimmedLine);
                    }
                }
            }
            else
            {
                // 尝试按分隔符分割
                string[] parts = optionsText.Split(new[] { ',', '，', ';', '；', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    options.Add(part.Trim());
                }
            }

            _logger.LogDebug("从文本解析出 {Count} 个选项", options.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析选项文本时发生错误: {Text}", optionsText);
        }

        return options;
    }
}