using CodeSpirit.ExamApi.Data.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 问题文本解析器V3
    /// </summary>
    public class QuestionTextParserV3
    {
        private readonly ILogger<QuestionTextParserV3> _logger;
        private readonly IEnumerable<IQuestionParser> _parsers;
        private static readonly string[] QuestionTypeHeaders = { "单选题", "判断题", "多选题" };
        private static readonly Regex QuestionStartPattern = new(@"^\d+[、.．]\s*", RegexOptions.Compiled);
        private static readonly Regex HeaderScorePattern = new(@"\[每题(\d+)分\]", RegexOptions.Compiled);

        public QuestionTextParserV3(
            ILogger<QuestionTextParserV3> logger,
            IEnumerable<IQuestionParser> parsers)
        {
            _logger = logger;
            _parsers = parsers;
        }

        public List<QuestionParseResult> Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<QuestionParseResult>();

            var questions = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var results = new List<QuestionParseResult>();

            foreach (var question in questions)
            {
                try
                {
                    var parser = _parsers.FirstOrDefault(p => p.CanParse(question));
                    if (parser != null)
                    {
                        var result = parser.Parse(question);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "解析题目时发生错误：{Question}", question);
                }
            }

            return results;
        }
    }
} 