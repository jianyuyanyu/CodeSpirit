using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.TextParsers.v2;

/// <summary>
/// 单选题解析器
/// </summary>
public class SingleChoiceQuestionParser : BaseQuestionParser, IScopedDependency
{
    private static readonly Regex OptionPattern = new(@"^[A-Z][、.．]\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex AnswerPattern = new(@"[\(（]\s*([A-Z])\s*[\)）]|(?<=\d[、.．]\s*.*)[A-Z](?=\s*$)", RegexOptions.Compiled);
    private static readonly Regex ScorePattern = new(@"\[(\d+)分\]", RegexOptions.Compiled);

    public SingleChoiceQuestionParser(ILogger<SingleChoiceQuestionParser> logger) : base(logger)
    {
    }

    public override bool CanParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // 检查是否以数字开头
        if (!Regex.IsMatch(line, @"^\d+[、.．]\s*"))
            return false;

        // 检查是否包含选项标记或答案标记
        return AnswerPattern.IsMatch(line) ||
               line.Contains("A、") || line.Contains("A.") ||
               line.Contains("B、") || line.Contains("B.") ||
               line.Contains("C、") || line.Contains("C.") ||
               line.Contains("D、") || line.Contains("D.");
    }

    public override QuestionParseResult Parse(IEnumerable<string> lines)
    {
        var lineList = lines.ToList();
        var result = new QuestionParseResult
        {
            Type = QuestionType.SingleChoice,
            Options = new List<string>()
        };

        try
        {
            // 解析题目内容和分数
            var firstLine = lineList[0];
            var contentLines = new List<string>();
            var currentContent = CleanContent(firstLine);

            // 提取分数
            var scoreMatch = ScorePattern.Match(firstLine);
            if (scoreMatch.Success)
            {
                result.Score = int.Parse(scoreMatch.Groups[1].Value);
                currentContent = ScorePattern.Replace(currentContent, "").Trim();
            }

            // 提取答案（如果有）
            var answerMatch = AnswerPattern.Match(firstLine);
            if (answerMatch.Success)
            {
                result.CorrectAnswer = answerMatch.Groups[1].Value;
                
                // 处理所有答案标记
                var singleLetterPattern = @"[\(（]\s*[A-Z]\s*[\)）]";
                var allAnswerMatches = Regex.Matches(currentContent, singleLetterPattern);
                
                // 从后往前替换，避免索引变化问题
                for (int i = allAnswerMatches.Count - 1; i >= 0; i--)
                {
                    var match = allAnswerMatches[i];
                    var answerText = match.Value;
                    var answerIndex = match.Index;
                    var beforeAnswerText = currentContent.Substring(0, answerIndex);
                    var afterAnswerText = currentContent.Substring(answerIndex + answerText.Length);
                    
                    // 如果答案标记后面只有空格、标点符号或为空，认为是在末尾
                    if (Regex.IsMatch(afterAnswerText, @"^[\s？?。.]*$"))
                    {
                        // 在末尾，保留带空格的括号，标点符号放在括号后面
                        // 检查答案标记前面是否有标点符号
                        var punctuationBeforeMatch = Regex.Match(beforeAnswerText, @"[？?。.]$");
                        if (punctuationBeforeMatch.Success)
                        {
                            // 标点符号在答案标记前面，移到括号后面
                            var beforePunctuation = beforeAnswerText.Substring(0, beforeAnswerText.Length - 1);
                            currentContent = beforePunctuation + "(  )" + punctuationBeforeMatch.Value;
                        }
                        else
                        {
                            // 标点符号在答案标记后面或没有标点符号
                            var punctuation = Regex.Match(afterAnswerText, @"[？?。.]").Value;
                            currentContent = beforeAnswerText + "(  )" + punctuation;
                        }
                    }
                    else
                    {
                        // 在中间位置，保留括号并用占位符替换
                        currentContent = currentContent.Substring(0, answerIndex) + "(  )" + 
                                       currentContent.Substring(answerIndex + answerText.Length);
                    }
                }
            }

            // 收集所有内容行，直到遇到选项标记
            contentLines.Add(currentContent);
            var optionStartIndex = 1;
            for (var i = 1; i < lineList.Count; i++)
            {
                var line = lineList[i];
                if (line.StartsWith("A、") || line.StartsWith("A.") ||
                    line.StartsWith("B、") || line.StartsWith("B.") ||
                    line.StartsWith("C、") || line.StartsWith("C.") ||
                    line.StartsWith("D、") || line.StartsWith("D."))
                {
                    optionStartIndex = i;
                    break;
                }
                contentLines.Add(line);
            }

            // 合并所有内容行
            result.Content = string.Join("\r\n", contentLines);
            
            // 清理内容
            result.Content = CleanContent(result.Content);

            // 解析选项 - 修复重复选项标记的问题
            var options = new List<(string Mark, string Content)>();
            var currentOption = "";
            var currentOptionMark = "";

            for (var i = optionStartIndex; i < lineList.Count; i++)
            {
                var line = lineList[i];
                if (line.StartsWith("【解析】") || line.StartsWith("【标签】") || line.StartsWith("【难度】"))
                    break;

                var optionMatch = OptionPattern.Match(line);
                if (optionMatch.Success)
                {
                    if (!string.IsNullOrWhiteSpace(currentOption))
                    {
                        options.Add((currentOptionMark, currentOption.Trim()));
                    }
                    currentOptionMark = line[0].ToString();
                    currentOption = optionMatch.Groups[1].Value;
                }
                else if (!string.IsNullOrWhiteSpace(currentOption))
                {
                    currentOption += " " + line;
                }

            }

            if (!string.IsNullOrWhiteSpace(currentOption))
            {
                options.Add((currentOptionMark, currentOption.Trim()));
            }

            // 如果有答案标记，转换为选项内容（使用原始内容，不转义）
            if (!string.IsNullOrEmpty(result.CorrectAnswer))
            {
                var answerMark = result.CorrectAnswer[0].ToString();
                var matchingOption = options.FirstOrDefault(opt => opt.Mark == answerMark);
                if (matchingOption != default)
                {
                    result.CorrectAnswer = matchingOption.Content;
                }
                else
                {
                    // 如果找不到匹配的选项标记，回退到索引方式（兼容性处理）
                    var answerIndex = result.CorrectAnswer[0] - 'A';
                    if (answerIndex >= 0 && answerIndex < options.Count)
                    {
                        result.CorrectAnswer = options[answerIndex].Content;
                    }
                }
            }

            // 按出现顺序保存选项内容，确保索引对应关系正确
            result.Options = options.Select(opt => opt.Content).ToList();

            // 解析解析和标签
            result.Analysis = ExtractAnalysis(lineList);
            result.Tags = ExtractTags(lineList);
            // 添加难度解析
            result.Difficulty = ExtractDifficulty(lineList);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析单选题失败");
            throw;
        }
    }
}
