using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeSpirit.ExamApi.Services.TextParsers.v2
{
    /// <summary>
    /// 问题文本解析器V2
    /// 支持解析包含特殊字符（如$符号）的编程题目
    /// 根据AMIS框架数据映射规范，对$字符进行特殊处理以避免模板变量冲突
    /// </summary>
    public class QuestionTextParserV2: IScopedDependency
    {
        private readonly ILogger<QuestionTextParserV2> _logger;
        private readonly SingleChoiceQuestionParser _singleChoiceParser;
        private readonly TrueFalseQuestionParser _trueFalseParser;
        private readonly MultipleChoiceQuestionParser _multipleChoiceParser;
        private static readonly string[] QuestionTypeHeaders = { "单选题", "判断题", "多选题" };
        private static readonly Regex QuestionStartPattern = new(@"^\d+[、.．]\s*", RegexOptions.Compiled);
        private static readonly Regex HeaderScorePattern = new(@"\[每题(\d+)分\]", RegexOptions.Compiled);

        /// <summary>
        /// 初始化问题文本解析器V2
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="singleChoiceParser">单选题解析器</param>
        /// <param name="trueFalseParser">判断题解析器</param>
        /// <param name="multipleChoiceParser">多选题解析器</param>
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

        /// <summary>
        /// 解析题目文本，支持包含特殊字符的编程题目
        /// </summary>
        /// <param name="text">待解析的题目文本</param>
        /// <returns>解析结果列表</returns>
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

        /// <summary>
        /// 尝试解析并添加问题到结果列表
        /// </summary>
        /// <param name="lines">问题文本行</param>
        /// <param name="results">结果列表</param>
        /// <param name="defaultScore">默认分数</param>
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
                    // 处理解析结果中的特殊字符，确保与AMIS框架兼容
                    ProcessSpecialCharacters(result);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析问题失败: {FirstLine}", lines.FirstOrDefault());
            }
        }

        /// <summary>
        /// 处理题目内容中的特殊字符，确保与AMIS框架兼容
        /// 根据AMIS数据映射规范，$字符在模板中有特殊含义，需要进行转义处理
        /// 参考：https://aisuda.bce.baidu.com/amis/zh-CN/docs/concepts/data-mapping
        /// </summary>
        /// <param name="result">解析结果</param>
        private void ProcessSpecialCharacters(QuestionParseResult result)
        {
            // 处理题目内容中的$字符，避免被AMIS解析为模板变量
            // 仅处理选项中的$字符，其他内容可以直接输出原始内容
            if (result.Options != null)
            {
                for (int i = 0; i < result.Options.Count; i++)
                {
                    if (!string.IsNullOrEmpty(result.Options[i]) && result.Options[i].Contains("$"))
                    {
                        result.Options[i] = result.Options[i].Replace("$", "\\$");
                        _logger.LogDebug("选项包含$字符，已进行转义处理: {Option}", result.Options[i]);
                    }
                }
            }

            // 处理正确答案中的$字符，确保与选项保持一致
            if (!string.IsNullOrEmpty(result.CorrectAnswer) && result.CorrectAnswer.Contains("$"))
            {
                result.CorrectAnswer = result.CorrectAnswer.Replace("$", "\\$");
                _logger.LogDebug("正确答案包含$字符，已进行转义处理: {CorrectAnswer}", result.CorrectAnswer);
            }
        }
    }
} 