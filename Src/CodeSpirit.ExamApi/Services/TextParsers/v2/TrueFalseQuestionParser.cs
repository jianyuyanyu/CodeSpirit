using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.TextParsers.v2;

/// <summary>
/// 判断题解析器
/// </summary>
public class TrueFalseQuestionParser : BaseQuestionParser
{
    private static readonly string[] TrueMarks = { "对", "√", "✓", "true", "t", "是", "y", "yes" };
    private static readonly string[] FalseMarks = { "错", "×", "✗", "false", "f", "否", "n", "no" };
    private static readonly Regex ScorePattern = new(@"\[(\d+)分\]", RegexOptions.Compiled);

    public TrueFalseQuestionParser(ILogger<TrueFalseQuestionParser> logger) : base(logger)
    {
    }

    public override bool CanParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // 检查是否以数字开头
        if (!Regex.IsMatch(line, @"^\d+[、\.\s]"))
            return false;

        // 检查是否包含选项标记（如果包含选项标记，则不是判断题）
        if (line.Contains("A、") || line.Contains("A.") ||
            line.Contains("B、") || line.Contains("B.") ||
            line.Contains("C、") || line.Contains("C.") ||
            line.Contains("D、") || line.Contains("D."))
        {
            return false;
        }

        // 构建答案匹配模式
        var answerPattern = string.Join("|", TrueMarks.Concat(FalseMarks).Select(Regex.Escape));
        // 检查是否包含判断题答案标记，支持带括号和不带括号的形式
        var hasTrueFalseAnswer = Regex.IsMatch(line, $@"[\(（]?\s*({answerPattern})\s*[\)）]?", RegexOptions.IgnoreCase) ||
                                line.Contains("对") || line.Contains("错") ||
                                line.Contains("√") || line.Contains("×") ||
                                line.Contains("✓") || line.Contains("✗");

        // 如果包含选项标记，则不是判断题
        if (hasTrueFalseAnswer && !line.Contains("A、") && !line.Contains("A.") &&
            !line.Contains("B、") && !line.Contains("B.") &&
            !line.Contains("C、") && !line.Contains("C.") &&
            !line.Contains("D、") && !line.Contains("D."))
        {
            return true;
        }

        return false;
    }

    public override QuestionParseResult Parse(IEnumerable<string> lines)
    {
        try
        {
            var lineList = lines.ToList();
            var result = new QuestionParseResult
            {
                Type = QuestionType.TrueFalse,
                Options = new List<string> { "True", "False" }
            };

            // 解析题目内容和答案
            var firstLine = lineList.First();
            result.Content = firstLine;

            // 提取分数
            var scoreMatch = ScorePattern.Match(firstLine);
            if (scoreMatch.Success)
            {
                result.Score = int.Parse(scoreMatch.Groups[1].Value);
                result.Content = ScorePattern.Replace(result.Content, "").Trim();
            }

            // 构建答案匹配模式
            var answerPattern = string.Join("|", TrueMarks.Concat(FalseMarks).Select(Regex.Escape));
            var answerMatch = Regex.Match(result.Content, $@"[\(（]\s*({answerPattern})\s*[\)）]", RegexOptions.IgnoreCase);
            
            // 移除序号和分数标记
            result.Content = Regex.Replace(result.Content, @"^\d+[、\.\s]+", "");
            result.Content = Regex.Replace(result.Content, @"\[\d+分\]", "").Trim();

            // 移除答案标记
            if (answerMatch.Success)
            {
                var answerText = answerMatch.Value;
                // 只移除包含正确答案的括号，保留题目内容中的其他括号
                var index = result.Content.LastIndexOf(answerText);
                if (index >= 0)
                {
                    result.Content = result.Content.Remove(index, answerText.Length).Trim();
                }
            }

            // 设置正确答案
            if (answerMatch.Success)
            {
                var answer = answerMatch.Groups[1].Value.Trim().ToLower();
                if (FalseMarks.Contains(answer, StringComparer.OrdinalIgnoreCase))
                {
                    result.CorrectAnswer = "False";
                }
                else if (TrueMarks.Contains(answer, StringComparer.OrdinalIgnoreCase))
                {
                    result.CorrectAnswer = "True";
                }
                else
                {
                    _logger.LogWarning("无法识别的判断题答案: {Answer}", answer);
                    result.CorrectAnswer = "False"; // 默认值
                }
            }
            else
            {
                // 尝试从题目末尾提取答案
                var endMatch = Regex.Match(result.Content, $@"[\(（]\s*({answerPattern})\s*[\)）]", RegexOptions.IgnoreCase);
                if (endMatch.Success)
                {
                    var answer = endMatch.Groups[1].Value.Trim().ToLower();
                    if (FalseMarks.Contains(answer, StringComparer.OrdinalIgnoreCase))
                    {
                        result.CorrectAnswer = "False";
                    }
                    else if (TrueMarks.Contains(answer, StringComparer.OrdinalIgnoreCase))
                    {
                        result.CorrectAnswer = "True";
                    }
                    else
                    {
                        _logger.LogWarning("无法识别的判断题答案: {Answer}", answer);
                        result.CorrectAnswer = "False"; // 默认值
                    }
                    // 只移除包含正确答案的括号
                    result.Content = result.Content.Remove(endMatch.Index, endMatch.Length).Trim();
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
            _logger.LogError(ex, "解析判断题失败");
            throw;
        }
    }
} 