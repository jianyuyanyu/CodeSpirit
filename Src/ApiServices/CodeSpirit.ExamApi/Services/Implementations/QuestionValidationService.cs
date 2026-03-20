using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 题目验证和自动修复服务实现
/// </summary>
public class QuestionValidationService : IQuestionValidationService
{
    private readonly ILogger<QuestionValidationService> _logger;

    public QuestionValidationService(ILogger<QuestionValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证和修复单个题目
    /// </summary>
    /// <param name="question">待验证的题目</param>
    /// <returns>验证和修复后的题目</returns>
    public async Task<CreateQuestionDto> ValidateAndFixQuestionAsync(CreateQuestionDto question)
    {
        if (question == null)
        {
            throw new ArgumentNullException(nameof(question));
        }

        _logger.LogDebug("开始验证和修复题目: {Content}", 
            question.Content?.Length > 50 ? question.Content.Substring(0, 50) + "..." : question.Content);

        // 验证题目
        var validationResult = ValidateQuestion(question);

        // 如果有错误，尝试修复
        if (validationResult.HasErrors)
        {
            _logger.LogInformation("题目存在 {ErrorCount} 个问题，开始自动修复", validationResult.Errors.Count);
            question = FixQuestion(question, validationResult);
        }

        _logger.LogDebug("题目验证和修复完成");
        return question;
    }

    /// <summary>
    /// 批量验证和修复题目
    /// </summary>
    /// <param name="questions">待验证的题目列表</param>
    /// <returns>验证和修复后的题目列表</returns>
    public async Task<List<CreateQuestionDto>> ValidateAndFixQuestionsAsync(List<CreateQuestionDto> questions)
    {
        if (questions == null || !questions.Any())
        {
            return questions ?? new List<CreateQuestionDto>();
        }

        _logger.LogInformation("开始批量验证和修复 {Count} 个题目", questions.Count);

        var fixedQuestions = new List<CreateQuestionDto>();
        int fixedCount = 0;

        foreach (var question in questions)
        {
            try
            {
                var fixedQuestion = await ValidateAndFixQuestionAsync(question);
                fixedQuestions.Add(fixedQuestion);

                // 检查是否进行了修复
                if (!ReferenceEquals(question, fixedQuestion) || HasQuestionChanged(question, fixedQuestion))
                {
                    fixedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证和修复题目时发生错误，保留原题目");
                fixedQuestions.Add(question);
            }
        }

        _logger.LogInformation("批量验证和修复完成，共修复 {FixedCount} 个题目", fixedCount);
        return fixedQuestions;
    }

    /// <summary>
    /// 验证题目格式
    /// </summary>
    /// <param name="question">待验证的题目</param>
    /// <returns>验证结果</returns>
    public QuestionValidationResult ValidateQuestion(CreateQuestionDto question)
    {
        var result = new QuestionValidationResult();

        if (question == null)
        {
            result.HasErrors = true;
            result.Errors.Add("题目对象为空");
            return result;
        }

        // 验证题目内容
        ValidateQuestionContent(question, result);

        // 验证基本字段
        ValidateBasicFields(question, result);

        // 验证选项
        ValidateOptions(question, result);

        // 验证答案
        ValidateAnswer(question, result);

        // 最终校验：单选题和多选题的答案必须是指定选项之一
        ValidateAnswerInOptions(question, result);

        result.HasErrors = result.Errors.Any();

        if (result.HasErrors)
        {
            _logger.LogDebug("题目验证发现 {ErrorCount} 个问题: {Errors}", 
                result.Errors.Count, string.Join("; ", result.Errors));
        }

        return result;
    }

    /// <summary>
    /// 自动修复题目
    /// </summary>
    /// <param name="question">待修复的题目</param>
    /// <param name="validationResult">验证结果</param>
    /// <returns>修复后的题目</returns>
    public CreateQuestionDto FixQuestion(CreateQuestionDto question, QuestionValidationResult validationResult)
    {
        if (question == null || !validationResult.HasErrors)
        {
            return question;
        }

        // 创建题目副本进行修复
        var fixedQuestion = CloneQuestion(question);

        try
        {
            // 修复选项序号
            if (validationResult.NeedsOptionCleanup)
            {
                FixOptionSequenceNumbers(fixedQuestion);
            }

            // 修复答案格式（ABCD转选项文本）
            if (validationResult.NeedsAnswerFormatFix)
            {
                FixAnswerFormat(fixedQuestion);
            }

            // 验证答案匹配性
            if (validationResult.NeedsAnswerValidation)
            {
                ValidateAndFixAnswerMatch(fixedQuestion);
            }

            _logger.LogInformation("题目修复完成");
            return fixedQuestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修复题目时发生错误，返回原题目");
            return question;
        }
    }

    /// <summary>
    /// 验证题目内容
    /// </summary>
    private void ValidateQuestionContent(CreateQuestionDto question, QuestionValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(question.Content))
        {
            result.Errors.Add("题目内容为空");
            return;
        }

        // 检查是否包含序号
        if (Regex.IsMatch(question.Content, @"^\s*\d+[\.\)、]\s*"))
        {
            result.Errors.Add("题目内容包含序号");
            result.FixSuggestions.Add("移除题目开头的序号");
        }

        // 检查是否包含分值标记
        if (Regex.IsMatch(question.Content, @"\(\s*\d+\s*分\s*\)"))
        {
            result.Errors.Add("题目内容包含分值标记");
            result.FixSuggestions.Add("移除分值标记");
        }
    }

    /// <summary>
    /// 验证基本字段
    /// </summary>
    private void ValidateBasicFields(CreateQuestionDto question, QuestionValidationResult result)
    {
        if (question.DefaultScore < 0 || question.DefaultScore > 100)
        {
            result.Errors.Add("题目分值必须在 0-100 之间");
        }
    }

    /// <summary>
    /// 验证选项
    /// </summary>
    private void ValidateOptions(CreateQuestionDto question, QuestionValidationResult result)
    {
        // 判断题不需要选项，或使用默认 True/False
        if (question.Type == QuestionType.TrueFalse)
        {
            if (question.Options == null || !question.Options.Any())
            {
                question.Options = new List<string> { "True", "False" };
            }
            return;
        }

        if (question.Options == null || !question.Options.Any())
        {
            result.Errors.Add("选项为空");
            return;
        }

        // 单选题和多选题至少需要 2 个有效选项
        var validOptions = question.Options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        if (question.Type == QuestionType.SingleChoice || question.Type == QuestionType.MultipleChoice)
        {
            if (validOptions.Count < 4)
            {
                result.Errors.Add("单选题和多选题至少需要 4 个选项");
            }
        }

        // 检查选项是否包含序号
        // 优化后的序号检测模式，减少误判：
        // 1. 字母序号：A. B) C、等
        // 2. 数字序号：限制为1-2位数字，避免误判小数和数量（如3.14、100.5）
        // 3. 特殊序号：圆圈数字、罗马数字
        bool hasSequenceNumbers = question.Options.Any(option => 
            !string.IsNullOrWhiteSpace(option) && 
            (Regex.IsMatch(option.Trim(), @"^[A-Za-z][\.\)、]\s*") ||
             Regex.IsMatch(option.Trim(), @"^\d{1,2}[\.\)、]\s+(?!\d)") ||
             Regex.IsMatch(option.Trim(), @"^[①②③④⑤⑥⑦⑧⑨⑩ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩ][\.\)、]?\s*")));

        if (hasSequenceNumbers)
        {
            result.Errors.Add("选项包含序号标记");
            result.NeedsOptionCleanup = true;
            result.FixSuggestions.Add("清理选项序号标记");
        }

        // 检查选项重复
        var duplicateOptions = question.Options
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .GroupBy(o => o.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOptions.Any())
        {
            result.Errors.Add($"存在重复选项: {string.Join(", ", duplicateOptions)}");
            result.FixSuggestions.Add("移除重复选项");
        }
    }

    /// <summary>
    /// 验证答案
    /// </summary>
    private void ValidateAnswer(CreateQuestionDto question, QuestionValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            result.Errors.Add("正确答案为空");
            return;
        }

        var answer = question.CorrectAnswer.Trim();

        // 判断题：答案必须是 True 或 False，且必须在选项中
        if (question.Type == QuestionType.TrueFalse)
        {
            if (!string.Equals(answer, "True", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(answer, "False", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("判断题的正确答案必须是 True 或 False");
            }
            else if (question.Options != null && question.Options.Any())
            {
                var cleanOptions = question.Options.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();
                if (!cleanOptions.Any(o => string.Equals(o, answer, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Errors.Add("正确答案必须是指定选项之一");
                }
            }
            return;
        }

        // 检查是否是字母序号格式（A、B、C、D等）
        if (IsLetterSequenceAnswer(answer))
        {
            result.Errors.Add("答案使用字母序号格式");
            result.NeedsAnswerFormatFix = true;
            result.FixSuggestions.Add("将字母序号转换为完整选项文本");
        }

        // 检查是否是数字序号格式（1、2、3、4等）
        if (IsNumberSequenceAnswer(answer))
        {
            result.Errors.Add("答案使用数字序号格式");
            result.NeedsAnswerFormatFix = true;
            result.FixSuggestions.Add("将数字序号转换为完整选项文本");
        }

        // 验证答案是否与选项匹配（单选题、多选题：答案必须是指定选项之一）
        if (question.Options != null && question.Options.Any())
        {
            bool answerMatches = ValidateAnswerMatchesOptions(question);
            if (!answerMatches)
            {
                result.Errors.Add("正确答案必须是指定选项之一");
                result.NeedsAnswerValidation = true;
                result.FixSuggestions.Add("验证并修正答案与选项的匹配关系");
            }
        }
    }

    /// <summary>
    /// 最终校验：单选题和多选题的正确答案必须是指定选项之一
    /// </summary>
    private void ValidateAnswerInOptions(CreateQuestionDto question, QuestionValidationResult result)
    {
        if (question.Type != QuestionType.SingleChoice && question.Type != QuestionType.MultipleChoice)
        {
            return;
        }

        if (question.Options == null || !question.Options.Any() || string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            return;
        }

        if (!ValidateAnswerMatchesOptions(question) && !result.Errors.Contains("正确答案必须是指定选项之一"))
        {
            result.Errors.Add("正确答案必须是指定选项之一");
        }
    }

    /// <summary>
    /// 检查是否是字母序号答案
    /// </summary>
    private bool IsLetterSequenceAnswer(string answer)
    {
        // 单个字母：仅 ASCII A–Z / a–z；中文单字正确答案（如「水」）不应视为字母序号
        if (answer.Length == 1 && char.IsAsciiLetter(answer[0]))
        {
            return true;
        }

        // 多个字母：A,B,C 或 A、B、C
        if (Regex.IsMatch(answer, @"^[A-Za-z](\s*[,，、]\s*[A-Za-z])*$"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否是数字序号答案
    /// </summary>
    private bool IsNumberSequenceAnswer(string answer)
    {
        // 单个数字：1、2、3、4
        if (answer.Length == 1 && char.IsDigit(answer[0]))
        {
            return true;
        }

        // 多个数字：1,2,3 或 1、2、3
        if (Regex.IsMatch(answer, @"^\d(\s*[,，、]\s*\d)*$"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 验证答案是否与选项匹配
    /// </summary>
    private bool ValidateAnswerMatchesOptions(CreateQuestionDto question)
    {
        if (question.Options == null || !question.Options.Any() || string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            return false;
        }

        var answer = question.CorrectAnswer.Trim();
        var cleanOptions = question.Options.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        // 单选题：答案应该完全匹配某个选项
        if (question.Type == QuestionType.SingleChoice)
        {
            return cleanOptions.Any(option => string.Equals(option, answer, StringComparison.OrdinalIgnoreCase));
        }

        // 多选题：答案应该是选项的组合（按逗号/分号分隔；顿号「、」常出现在选项正文内，不得作为分隔符）
        if (question.Type == QuestionType.MultipleChoice)
        {
            var answerParts = answer.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToList();

            return answerParts.All(part => cleanOptions.Any(option => 
                string.Equals(option, part, StringComparison.OrdinalIgnoreCase)));
        }

        return true; // 其他类型的题目暂时认为匹配
    }

    /// <summary>
    /// 修复选项序号
    /// </summary>
    private void FixOptionSequenceNumbers(CreateQuestionDto question)
    {
        if (question.Options == null || !question.Options.Any())
        {
            return;
        }

        _logger.LogDebug("开始清理选项序号");

        for (int i = 0; i < question.Options.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(question.Options[i]))
            {
                continue;
            }

            var originalOption = question.Options[i];
            var cleanedOption = CleanOptionSequenceNumber(originalOption);

            if (cleanedOption != originalOption)
            {
                question.Options[i] = cleanedOption;
                _logger.LogDebug("选项序号清理: '{Original}' -> '{Cleaned}'", originalOption, cleanedOption);
            }
        }
    }

    /// <summary>
    /// 清理单个选项的序号
    /// </summary>
    private string CleanOptionSequenceNumber(string option)
    {
        if (string.IsNullOrWhiteSpace(option))
        {
            return option;
        }

        var cleanedOption = option.Trim();

        // 移除常见的选项序号格式
        var patterns = new[]
        {
            @"^[A-Za-z]\.\s*",      // A. B. C. D.
            @"^[A-Za-z]、\s*",      // A、B、C、D、
            @"^[A-Za-z]\)\s*",      // A) B) C) D)
            @"^[A-Za-z]\s*[\.\)、]\s*", // A. A) A、等的通用模式
            @"^\d+\.\s*",           // 1. 2. 3. 4.
            @"^\d+、\s*",           // 1、2、3、4、
            @"^\d+\)\s*",           // 1) 2) 3) 4)
            @"^\d+\s*[\.\)、]\s*",  // 1. 1) 1、等的通用模式
            @"^[(（]\d+[)）]\s*",   // (1) (2) (3) (4) 或 （1）（2）（3）（4）
            @"^[(（][A-Za-z][)）]\s*", // (A) (B) (C) (D) 或 （A）（B）（C）（D）
            @"^[①②③④⑤⑥⑦⑧⑨⑩]\s*", // 圆圈数字
            @"^[ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩ]\s*[\.\)、]?\s*", // 罗马数字
            @"^[ⅰⅱⅲⅳⅴⅵⅶⅷⅸⅹ]\s*[\.\)、]?\s*"  // 小写罗马数字
        };

        foreach (var pattern in patterns)
        {
            cleanedOption = Regex.Replace(cleanedOption, pattern, "", RegexOptions.IgnoreCase);
        }

        return cleanedOption.TrimStart();
    }

    /// <summary>
    /// 修复答案格式（ABCD转选项文本）
    /// </summary>
    private void FixAnswerFormat(CreateQuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(question.CorrectAnswer) || question.Options == null || !question.Options.Any())
        {
            return;
        }

        var originalAnswer = question.CorrectAnswer.Trim();
        _logger.LogDebug("开始修复答案格式: {Answer}", originalAnswer);

        // 处理字母序号答案
        if (IsLetterSequenceAnswer(originalAnswer))
        {
            var fixedAnswer = ConvertLetterAnswerToText(originalAnswer, question.Options);
            if (fixedAnswer != originalAnswer)
            {
                question.CorrectAnswer = fixedAnswer;
                _logger.LogInformation("字母序号答案修复: '{Original}' -> '{Fixed}'", originalAnswer, fixedAnswer);
            }
        }
        // 处理数字序号答案
        else if (IsNumberSequenceAnswer(originalAnswer))
        {
            var fixedAnswer = ConvertNumberAnswerToText(originalAnswer, question.Options);
            if (fixedAnswer != originalAnswer)
            {
                question.CorrectAnswer = fixedAnswer;
                _logger.LogInformation("数字序号答案修复: '{Original}' -> '{Fixed}'", originalAnswer, fixedAnswer);
            }
        }
    }

    /// <summary>
    /// 将字母序号答案转换为选项文本
    /// </summary>
    private string ConvertLetterAnswerToText(string letterAnswer, List<string> options)
    {
        try
        {
            var cleanOptions = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            if (!cleanOptions.Any())
            {
                return letterAnswer;
            }

            // 单个字母答案
            if (letterAnswer.Length == 1 && char.IsLetter(letterAnswer[0]))
            {
                int index = char.ToUpper(letterAnswer[0]) - 'A';
                if (index >= 0 && index < cleanOptions.Count)
                {
                    return cleanOptions[index].Trim();
                }
            }
            // 多个字母答案
            else
            {
                var letterParts = letterAnswer.Split(new[] { ',', '，', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length == 1 && char.IsLetter(l[0]))
                    .ToList();

                var optionTexts = new List<string>();
                foreach (var letter in letterParts)
                {
                    int index = char.ToUpper(letter[0]) - 'A';
                    if (index >= 0 && index < cleanOptions.Count)
                    {
                        optionTexts.Add(cleanOptions[index].Trim());
                    }
                }

                if (optionTexts.Any())
                {
                    return string.Join("，", optionTexts);
                }
            }

            return letterAnswer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换字母序号答案时发生错误: {Answer}", letterAnswer);
            return letterAnswer;
        }
    }

    /// <summary>
    /// 将数字序号答案转换为选项文本
    /// </summary>
    private string ConvertNumberAnswerToText(string numberAnswer, List<string> options)
    {
        try
        {
            var cleanOptions = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
            if (!cleanOptions.Any())
            {
                return numberAnswer;
            }

            // 单个数字答案
            if (numberAnswer.Length == 1 && char.IsDigit(numberAnswer[0]))
            {
                int index = int.Parse(numberAnswer) - 1; // 数字从1开始，索引从0开始
                if (index >= 0 && index < cleanOptions.Count)
                {
                    return cleanOptions[index].Trim();
                }
            }
            // 多个数字答案
            else
            {
                var numberParts = numberAnswer.Split(new[] { ',', '，', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => n.Length == 1 && char.IsDigit(n[0]))
                    .ToList();

                var optionTexts = new List<string>();
                foreach (var number in numberParts)
                {
                    int index = int.Parse(number) - 1; // 数字从1开始，索引从0开始
                    if (index >= 0 && index < cleanOptions.Count)
                    {
                        optionTexts.Add(cleanOptions[index].Trim());
                    }
                }

                if (optionTexts.Any())
                {
                    return string.Join("，", optionTexts);
                }
            }

            return numberAnswer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换数字序号答案时发生错误: {Answer}", numberAnswer);
            return numberAnswer;
        }
    }

    /// <summary>
    /// 验证并修复答案匹配性
    /// </summary>
    private void ValidateAndFixAnswerMatch(CreateQuestionDto question)
    {
        if (question.Options == null || !question.Options.Any() || string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            return;
        }

        var answer = question.CorrectAnswer.Trim();
        var cleanOptions = question.Options.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        // 尝试模糊匹配
        if (question.Type == QuestionType.SingleChoice)
        {
            var bestMatch = FindBestMatchingOption(answer, cleanOptions);
            if (bestMatch != null && bestMatch != answer)
            {
                _logger.LogInformation("答案模糊匹配修复: '{Original}' -> '{Fixed}'", answer, bestMatch);
                question.CorrectAnswer = bestMatch;
            }
        }
    }

    /// <summary>
    /// 查找最佳匹配的选项
    /// </summary>
    private string? FindBestMatchingOption(string answer, List<string> options)
    {
        if (string.IsNullOrWhiteSpace(answer) || !options.Any())
        {
            return null;
        }

        // 精确匹配
        var exactMatch = options.FirstOrDefault(o => string.Equals(o, answer, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // 包含匹配
        var containsMatch = options.FirstOrDefault(o => o.Contains(answer, StringComparison.OrdinalIgnoreCase) ||
                                                        answer.Contains(o, StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null)
        {
            return containsMatch;
        }

        // 相似度匹配（简单的字符相似度）
        var bestMatch = options
            .Select(o => new { Option = o, Similarity = CalculateStringSimilarity(answer, o) })
            .Where(x => x.Similarity > 0.7) // 相似度阈值
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();

        return bestMatch?.Option;
    }

    /// <summary>
    /// 计算字符串相似度（简单实现）
    /// </summary>
    private double CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        {
            return 0;
        }

        int maxLength = Math.Max(str1.Length, str2.Length);
        if (maxLength == 0)
        {
            return 1;
        }

        int commonChars = 0;
        for (int i = 0; i < Math.Min(str1.Length, str2.Length); i++)
        {
            if (str1[i] == str2[i])
            {
                commonChars++;
            }
        }

        return (double)commonChars / maxLength;
    }

    /// <summary>
    /// 克隆题目对象
    /// </summary>
    private CreateQuestionDto CloneQuestion(CreateQuestionDto question)
    {
        return new CreateQuestionDto
        {
            Content = question.Content,
            Type = question.Type,
            Options = question.Options?.ToList() ?? new List<string>(),
            CorrectAnswer = question.CorrectAnswer,
            Difficulty = question.Difficulty,
            Analysis = question.Analysis,
            KnowledgePoints = question.KnowledgePoints,
            Tags = question.Tags?.ToList() ?? new List<string>(),
            CategoryId = question.CategoryId,
            DefaultScore = question.DefaultScore
        };
    }

    /// <summary>
    /// 检查题目是否发生了变化
    /// </summary>
    private bool HasQuestionChanged(CreateQuestionDto original, CreateQuestionDto modified)
    {
        return original.Content != modified.Content ||
               original.CorrectAnswer != modified.CorrectAnswer ||
               !original.Options.SequenceEqual(modified.Options) ||
               !original.Tags.SequenceEqual(modified.Tags);
    }
}
