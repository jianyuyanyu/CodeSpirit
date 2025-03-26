using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 判断题解析器
    /// </summary>
    public class TrueFalseQuestionParser : BaseQuestionParser
    {
        private static readonly Regex TrueFalseAnswerPattern = new(@"（([对错TF])）", RegexOptions.Compiled);
        private static readonly Regex TrueFalseOptionPattern = new(@"（[对错TF]）", RegexOptions.Compiled);

        public override QuestionType SupportedType => QuestionType.TrueFalse;

        public TrueFalseQuestionParser(ILogger logger) : base(logger)
        {
        }

        public override bool CanParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var match = TrueFalseAnswerPattern.Match(text);
            return match.Success;
        }

        public override QuestionParseResult Parse(string text)
        {
            var match = TrueFalseAnswerPattern.Match(text);
            if (!match.Success)
                return null;

            var answerLetter = match.Groups[1].Value;
            var correctAnswer = answerLetter switch
            {
                "对" or "T" => "True",
                "错" or "F" => "False",
                _ => throw new ArgumentException($"无效的判断题答案: {answerLetter}")
            };

            var content = text;
            var codeSnippets = ExtractCodeSnippets(content);
            content = CleanContent(content);
            content = TrueFalseOptionPattern.Replace(content, "");
            var analysis = ExtractAnalysis(text);
            var tags = ExtractTags(text);
            var difficulty = ExtractDifficulty(text);

            return new QuestionParseResult
            {
                Type = QuestionType.TrueFalse,
                Content = content,
                CodeSnippets = codeSnippets,
                CorrectAnswer = correctAnswer,
                Analysis = analysis,
                Tags = tags?.Split(new[] { '，', '、', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                Difficulty = ParseDifficulty(difficulty)
            };
        }

        private QuestionDifficulty ParseDifficulty(string difficulty)
        {
            return difficulty switch
            {
                "简单" => QuestionDifficulty.Easy,
                "中等" => QuestionDifficulty.Medium,
                "困难" => QuestionDifficulty.Hard,
                _ => QuestionDifficulty.Easy
            };
        }
    }
} 