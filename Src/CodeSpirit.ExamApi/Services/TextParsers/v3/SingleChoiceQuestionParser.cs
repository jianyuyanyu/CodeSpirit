using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 单选题解析器
    /// </summary>
    public class SingleChoiceQuestionParser : BaseQuestionParser
    {
        private static readonly Regex SingleAnswerPattern = new(@"（\s*([A-Z])\s*）|\(([A-Z])\)", RegexOptions.Compiled);
        private static readonly new Regex OptionPattern = new(@"([A-Z])[.、](.*?)(?=(?:[A-Z][.、])|$)", RegexOptions.Compiled | RegexOptions.Singleline);

        public SingleChoiceQuestionParser(ILogger<SingleChoiceQuestionParser> logger) : base(logger)
        {
        }

        public override QuestionType SupportedType => QuestionType.SingleChoice;

        public override bool CanParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var answerMatch = SingleAnswerPattern.Match(text);
            var optionsMatch = OptionPattern.Matches(text);

            return answerMatch.Success && optionsMatch.Count >= 2;
        }

        public override QuestionParseResult Parse(string text)
        {
            var answerMatch = SingleAnswerPattern.Match(text);
            if (!answerMatch.Success)
                return null;

            var answerLetter = answerMatch.Groups[1].Success ? answerMatch.Groups[1].Value : answerMatch.Groups[2].Value;
            var content = text;
            var codeSnippets = ExtractCodeSnippets(content);
            content = CleanContent(content);

            var options = new Dictionary<string, string>();
            var optionsMatch = OptionPattern.Matches(text);
            foreach (Match match in optionsMatch)
            {
                var letter = match.Groups[1].Value;
                var optionContent = match.Groups[2].Value.Trim();
                options[letter] = optionContent;
            }

            var correctAnswer = options.ContainsKey(answerLetter) ? options[answerLetter] : null;
            if (correctAnswer == null)
                return null;

            var analysis = ExtractAnalysis(text);
            var tags = ExtractTags(text);
            var difficulty = ExtractDifficulty(text);

            return new QuestionParseResult
            {
                Type = QuestionType.SingleChoice,
                Content = content,
                CodeSnippets = codeSnippets,
                Options = options.Values.ToList(),
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