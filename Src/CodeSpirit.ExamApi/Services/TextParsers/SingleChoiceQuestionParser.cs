using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.TextParsers;

/// <summary>
/// 单选题解析器
/// </summary>
public class SingleChoiceQuestionParser : BaseQuestionParser
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
               line.Contains("B、") || line.Contains("B.");
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
            result.Content = CleanContent(firstLine);

            // 提取分数
            var scoreMatch = ScorePattern.Match(firstLine);
            if (scoreMatch.Success)
            {
                result.Score = int.Parse(scoreMatch.Groups[1].Value);
                result.Content = ScorePattern.Replace(result.Content, "").Trim();
            }

            // 提取答案（如果有）
            var answerMatch = AnswerPattern.Match(firstLine);
            if (answerMatch.Success)
            {
                result.CorrectAnswer = answerMatch.Groups[1].Value;
                result.Content = Regex.Replace(result.Content, @"\s*[A-Z]\s*$", "").Trim(); // 移除末尾的答案
            }

            // 解析选项
            var options = new Dictionary<string, string>();
            var currentOption = "";
            var currentOptionMark = "";
            
            foreach (var line in lineList.Skip(1))
            {
                if (line.StartsWith("【解析】") || line.StartsWith("【标签】"))
                    break;

                var optionMatch = OptionPattern.Match(line);
                if (optionMatch.Success)
                {
                    if (!string.IsNullOrWhiteSpace(currentOption))
                    {
                        options[currentOptionMark] = currentOption.Trim();
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
                options[currentOptionMark] = currentOption.Trim();
            }

            // 按选项标记排序
            result.Options = options.OrderBy(kv => kv.Key)
                                 .Select(kv => kv.Value)
                                 .ToList();

            // 如果有答案标记，转换为选项内容
            if (!string.IsNullOrEmpty(result.CorrectAnswer))
            {
                var answerIndex = result.CorrectAnswer[0] - 'A';
                if (answerIndex >= 0 && answerIndex < result.Options.Count)
                {
                    result.CorrectAnswer = result.Options[answerIndex];
                }
            }

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