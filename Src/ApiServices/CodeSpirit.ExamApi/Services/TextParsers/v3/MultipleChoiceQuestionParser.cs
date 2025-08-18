using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 多选题解析器
    /// </summary>
    public class MultipleChoiceQuestionParser : BaseQuestionParser, IQuestionParser
    {
        private static readonly Regex MultipleAnswerPattern = new(@"\(([A-Z]+)\)", RegexOptions.Compiled);
        private static readonly new Regex OptionPattern = new(@"([A-Z])、(.*?)(?=(?:[A-Z]、)|$)", RegexOptions.Compiled | RegexOptions.Singleline);

        public override QuestionType SupportedType => QuestionType.MultipleChoice;

        public MultipleChoiceQuestionParser(ILogger logger) : base(logger)
        {
        }

        public override bool CanParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var answerMatch = MultipleAnswerPattern.Match(text);
            if (!answerMatch.Success || answerMatch.Groups[1].Value.Length < 2)
                return false;

            var optionsMatch = OptionPattern.Matches(text);
            return optionsMatch.Count >= 2;
        }

        public override QuestionParseResult Parse(string text)
        {
            var answerMatch = MultipleAnswerPattern.Match(text);
            if (!answerMatch.Success)
                return null;

            var answerLetters = answerMatch.Groups[1].Value.ToCharArray();
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

            var correctAnswers = new List<string>();
            foreach (var letter in answerLetters)
            {
                if (options.ContainsKey(letter.ToString()))
                {
                    correctAnswers.Add(options[letter.ToString()]);
                }
            }

            if (!correctAnswers.Any())
                return null;

            var analysis = ExtractAnalysis(text);
            var tags = ExtractTags(text);
            var difficulty = ExtractDifficulty(text);

            return new QuestionParseResult
            {
                Type = QuestionType.MultipleChoice,
                Content = content,
                CodeSnippets = codeSnippets,
                Options = options.Values.ToList(),
                CorrectAnswer = string.Join(";", correctAnswers),
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