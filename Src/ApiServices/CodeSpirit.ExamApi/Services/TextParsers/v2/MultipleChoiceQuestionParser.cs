using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CodeSpirit.ExamApi.Services.TextParsers.v2;

/// <summary>
/// 多选题解析器
/// </summary>
public class MultipleChoiceQuestionParser : BaseQuestionParser, IScopedDependency
{
    private static readonly string[] SectionHeaders = { "多项选择题", "多选题" };
    private static readonly string OptionPattern = @"^([A-Z])[、\.\s]+(.+)$";

    public MultipleChoiceQuestionParser(ILogger<MultipleChoiceQuestionParser> logger) : base(logger)
    {
    }

    public override bool CanParse(string line)
    {
        // 检查是否是题目开始（数字开头）
        if (Regex.IsMatch(line, @"^\d+[、\.\s]"))
        {
            // 检查是否包含多个选项标记
            var answerMatch = Regex.Match(line, @"[\(（]([A-Z][A-Z]+)[\)）]");
            return answerMatch.Success;
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
                Type = QuestionType.MultipleChoice,
                Options = new List<string>()
            };

            // 解析题目内容和答案
            var firstLine = lineList.First();
            var answerMatch = Regex.Match(firstLine, @"[\(（]([A-Z]+)[\)）]");
            
            // 清理题目内容，包括答案标记
            result.Content = firstLine;
            if (answerMatch.Success)
            {
                var answerText = answerMatch.Value;
                // 智能处理答案标记：
                // 1. 如果答案标记在题目末尾，直接移除
                // 2. 如果答案标记在题目中间，用占位符替换
                var answerIndex = result.Content.IndexOf(answerText);
                var afterAnswerText = result.Content.Substring(answerIndex + answerText.Length);
                
                // 如果答案标记后面只有空格、标点符号或为空，认为是在末尾
                if (Regex.IsMatch(afterAnswerText, @"^[\s？?。.]*$"))
                {
                    // 在末尾，直接移除答案标记，但保留标点符号
                    var punctuation = Regex.Match(afterAnswerText, @"[？?。.]").Value;
                    result.Content = result.Content.Substring(0, answerIndex).Trim() + punctuation;
                }
                else
                {
                    // 在中间位置，用占位符替换
                    result.Content = result.Content.Replace(answerText, "____").Trim();
                }
            }

            // 移除序号和分数标记
            result.Content = CleanContent(result.Content);

            // 添加问号（如果需要）
            if (!result.Content.EndsWith("？") && !result.Content.EndsWith("?") && !result.Content.EndsWith("。"))
            {
                result.Content += "？";
            }

            // 解析选项
            var options = new Dictionary<string, string>();
            foreach (var line in lineList.Skip(1))
            {
                var optionMatch = Regex.Match(line, OptionPattern);
                if (optionMatch.Success)
                {
                    var optionMark = optionMatch.Groups[1].Value;
                    var optionContent = optionMatch.Groups[2].Value.Trim();
                    options[optionMark] = optionContent;
                    result.Options.Add(optionContent);
                }
            }

            // 设置正确答案（多个）
            if (answerMatch.Success)
            {
                var correctAnswers = new List<string>();
                var answerMarks = answerMatch.Groups[1].Value.ToCharArray();
                foreach (var mark in answerMarks)
                {
                    var normalizedMark = mark.ToString();
                    if (options.ContainsKey(normalizedMark))
                    {
                        correctAnswers.Add(options[normalizedMark]);
                    }
                }
                result.CorrectAnswer = string.Join(",", correctAnswers);
            }

            // 解析分数
            result.Score = ExtractScore(firstLine);

            // 解析解析和标签
            result.Analysis = ExtractAnalysis(lineList);
            result.Tags = ExtractTags(lineList);
            // 添加难度解析
            result.Difficulty = ExtractDifficulty(lineList);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析多选题失败");
            throw;
        }
    }
} 