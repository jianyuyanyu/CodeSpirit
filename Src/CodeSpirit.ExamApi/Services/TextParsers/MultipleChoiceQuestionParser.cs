using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.TextParsers;

/// <summary>
/// 多选题解析器
/// </summary>
public class MultipleChoiceQuestionParser : BaseQuestionParser
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
                var index = result.Content.LastIndexOf(answerText);
                if (index >= 0)
                {
                    result.Content = result.Content.Remove(index).Trim();
                }
            }

            // 移除序号和分数标记
            result.Content = CleanContent(result.Content);

            // 添加问号（如果没有）
            if (!result.Content.EndsWith("？") && !result.Content.EndsWith("?"))
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
                result.CorrectAnswer = string.Join("|", correctAnswers);
            }

            // 解析分数
            result.Score = ExtractScore(firstLine);

            // 解析解析和标签
            result.Analysis = ExtractAnalysis(lineList);
            result.Tags = ExtractTags(lineList);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析多选题失败");
            throw;
        }
    }
} 