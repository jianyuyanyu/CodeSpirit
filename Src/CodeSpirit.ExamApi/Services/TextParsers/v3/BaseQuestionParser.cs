using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 问题解析器基类
    /// </summary>
    public abstract class BaseQuestionParser : IQuestionParser
    {
        protected readonly ILogger _logger;
        protected static readonly Regex AnswerPattern = new(@"\(([A-Z]+)\)", RegexOptions.Compiled);
        protected static readonly Regex OptionPattern = new(@"([A-Z])、(.*?)(?=(?:[A-Z]、)|$)", RegexOptions.Compiled | RegexOptions.Singleline);
        protected static readonly Regex QuestionStartPattern = new(@"^\d+[、.．]\s*", RegexOptions.Compiled);
        protected static readonly Regex CodePattern = new(@"【代码】([\s\S]*?)【代码结束】", RegexOptions.Compiled);
        protected static readonly Regex AnalysisPattern = new(@"(?:解析|【解析】)[:：]?\s*(.*?)(?=(?:标签|【标签】|难度|$))", RegexOptions.Singleline | RegexOptions.Compiled);
        protected static readonly Regex TagsPattern = new(@"(?:标签|【标签】)[:：]?\s*(.*?)(?=(?:难度|$))", RegexOptions.Singleline | RegexOptions.Compiled);
        protected static readonly Regex DifficultyPattern = new(@"难度[:：](.*?)$", RegexOptions.Singleline | RegexOptions.Compiled);
        protected static readonly Regex CodeSnippetPattern = new(@"```(.*?)```", RegexOptions.Singleline | RegexOptions.Compiled);

        protected BaseQuestionParser(ILogger logger)
        {
            _logger = logger;
        }

        public abstract QuestionType SupportedType { get; }

        public abstract bool CanParse(string text);

        public abstract QuestionParseResult? Parse(string text);

        /// <summary>
        /// 清理内容
        /// </summary>
        protected virtual string CleanContent(string text)
        {
            // 移除题号
            text = QuestionStartPattern.Replace(text, "");
            // 移除代码片段
            text = CodeSnippetPattern.Replace(text, "");
            // 移除选项
            text = OptionPattern.Replace(text, "");
            // 移除解析
            text = AnalysisPattern.Replace(text, "");
            // 移除标签
            text = TagsPattern.Replace(text, "");
            // 移除难度
            text = DifficultyPattern.Replace(text, "");
            // 移除答案标记
            text = Regex.Replace(text, @"（\s*[A-Z]\s*）|\([A-Z]+\)", "");
            // 清理多余的空行和空格
            text = Regex.Replace(text, @"\n\s*\n", "\n");
            return text.Trim();
        }

        /// <summary>
        /// 提取解析
        /// </summary>
        protected virtual string ExtractAnalysis(string text)
        {
            var match = AnalysisPattern.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        /// <summary>
        /// 提取标签
        /// </summary>
        protected virtual string ExtractTags(string text)
        {
            var match = TagsPattern.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        /// <summary>
        /// 提取难度
        /// </summary>
        protected virtual string ExtractDifficulty(string text)
        {
            var match = DifficultyPattern.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        /// <summary>
        /// 提取代码片段
        /// </summary>
        protected virtual string ExtractCodeSnippet(string text)
        {
            var match = CodePattern.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        protected List<string> ExtractOptions(string text)
        {
            var options = new List<string>();
            var optionMatches = OptionPattern.Matches(text);
            foreach (Match match in optionMatches)
            {
                options.Add(match.Groups[2].Value.Trim());
            }
            return options;
        }

        protected string GetOptionByLetter(string letter, List<string> options)
        {
            var index = letter[0] - 'A';
            return index >= 0 && index < options.Count ? options[index] : string.Empty;
        }

        protected string GetAnswerContent(string answerLetter, List<string> options)
        {
            if (string.IsNullOrEmpty(answerLetter))
                return string.Empty;

            if (answerLetter.Length == 1)
            {
                return GetOptionByLetter(answerLetter, options);
            }
            else
            {
                var answers = new List<string>();
                foreach (var letter in answerLetter)
                {
                    var answer = GetOptionByLetter(letter.ToString(), options);
                    if (!string.IsNullOrEmpty(answer))
                    {
                        answers.Add(answer);
                    }
                }
                return string.Join("|", answers);
            }
        }

        protected string ExtractCodeSnippets(string text)
        {
            var match = CodeSnippetPattern.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        protected QuestionDifficulty ParseDifficulty(string difficulty)
        {
            if (string.IsNullOrWhiteSpace(difficulty))
                return QuestionDifficulty.Easy;

            return difficulty.Trim() switch
            {
                "简单" => QuestionDifficulty.Easy,
                "中等" => QuestionDifficulty.Medium,
                "困难" => QuestionDifficulty.Hard,
                _ => QuestionDifficulty.Easy
            };
        }
    }
} 