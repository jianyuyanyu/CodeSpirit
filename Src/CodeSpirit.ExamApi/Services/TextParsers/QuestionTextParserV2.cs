using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Services.TextParsers
{
    /// <summary>
    /// 问题文本解析器V2
    /// </summary>
    public class QuestionTextParserV2
    {
        private readonly ILogger<QuestionTextParserV2> _logger;
        private readonly SingleChoiceQuestionParser _singleChoiceParser;
        private readonly TrueFalseQuestionParser _trueFalseParser;
        private readonly MultipleChoiceQuestionParser _multipleChoiceParser;
        private static readonly string[] QuestionTypeHeaders = { "单选题", "判断题", "多选题" };
        private static readonly Regex QuestionStartPattern = new(@"^\d+[、.．]\s*", RegexOptions.Compiled);
        private static readonly Regex HeaderScorePattern = new(@"\[每题(\d+)分\]", RegexOptions.Compiled);

        public QuestionTextParserV2(
            ILogger<QuestionTextParserV2> logger,
            SingleChoiceQuestionParser singleChoiceParser,
            TrueFalseQuestionParser trueFalseParser,
            MultipleChoiceQuestionParser multipleChoiceParser)
        {
            _logger = logger;
            _singleChoiceParser = singleChoiceParser;
            _trueFalseParser = trueFalseParser;
            _multipleChoiceParser = multipleChoiceParser;
        }

        public List<QuestionParseResult> Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<QuestionParseResult>();

            var results = new List<QuestionParseResult>();
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                          .Select(l => l.Trim())
                          .ToList();

            var currentQuestionLines = new List<string>();
            var defaultScore = 0;
            var isInQuestion = false;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                
                // 跳过空行，但如果正在处理问题，则可能表示问题结束
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (isInQuestion && currentQuestionLines.Any())
                    {
                        TryParseAndAddQuestion(currentQuestionLines, results, defaultScore);
                        currentQuestionLines.Clear();
                        isInQuestion = false;
                    }
                    continue;
                }

                // 检查题型标题
                if (QuestionTypeHeaders.Any(h => line.Contains(h)))
                {
                    if (isInQuestion && currentQuestionLines.Any())
                    {
                        TryParseAndAddQuestion(currentQuestionLines, results, defaultScore);
                        currentQuestionLines.Clear();
                    }

                    // 提取默认分数
                    var scoreMatch = HeaderScorePattern.Match(line);
                    if (scoreMatch.Success)
                    {
                        defaultScore = int.Parse(scoreMatch.Groups[1].Value);
                    }

                    isInQuestion = false;
                    continue;
                }

                // 检查新问题开始
                if (QuestionStartPattern.IsMatch(line))
                {
                    if (isInQuestion && currentQuestionLines.Any())
                    {
                        TryParseAndAddQuestion(currentQuestionLines, results, defaultScore);
                        currentQuestionLines.Clear();
                    }
                    isInQuestion = true;
                }

                // 如果是选项或者在问题中，添加行
                if (isInQuestion || Regex.IsMatch(line, @"^[A-Z][、.．]"))
                {
                    currentQuestionLines.Add(line);
                }
            }

            // 处理最后一个问题
            if (currentQuestionLines.Any())
            {
                TryParseAndAddQuestion(currentQuestionLines, results, defaultScore);
            }

            return results;
        }

        private void TryParseAndAddQuestion(List<string> lines, List<QuestionParseResult> results, int defaultScore)
        {
            try
            {
                if (!lines.Any())
                    return;

                var firstLine = lines[0];
                QuestionParseResult result = null;

                // 检查所有行是否包含选项标记
                bool hasOptions = lines.Any(line => 
                    line.Contains("A、") || line.Contains("A.") ||
                    line.Contains("B、") || line.Contains("B.") ||
                    line.Contains("C、") || line.Contains("C.") ||
                    line.Contains("D、") || line.Contains("D."));

                // 尝试使用多选题解析器
                if (_multipleChoiceParser.CanParse(firstLine))
                {
                    result = _multipleChoiceParser.Parse(lines);
                    if (result.Score == 0)
                    {
                        result.Score = defaultScore;
                    }
                }
                // 尝试使用单选题解析器
                else if (_singleChoiceParser.CanParse(firstLine) || hasOptions)
                {
                    result = _singleChoiceParser.Parse(lines);
                    if (result.Score == 0)
                    {
                        result.Score = defaultScore;
                    }
                }
                // 尝试使用判断题解析器
                else if (_trueFalseParser.CanParse(firstLine) && !hasOptions)
                {
                    result = _trueFalseParser.Parse(lines);
                    if (result.Score == 0)
                    {
                        result.Score = defaultScore;
                    }
                }

                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析问题失败: {FirstLine}", lines.FirstOrDefault());
            }
        }
    }
} 