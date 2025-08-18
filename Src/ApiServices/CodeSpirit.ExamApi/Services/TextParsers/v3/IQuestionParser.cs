using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Services.TextParsers.v3
{
    /// <summary>
    /// 问题解析器接口
    /// </summary>
    public interface IQuestionParser
    {
        /// <summary>
        /// 判断是否可以解析该问题
        /// </summary>
        /// <param name="text">问题文本</param>
        /// <returns>是否可以解析</returns>
        bool CanParse(string text);

        /// <summary>
        /// 解析问题
        /// </summary>
        /// <param name="text">问题文本</param>
        /// <returns>解析结果</returns>
        QuestionParseResult Parse(string text);

        /// <summary>
        /// 获取支持的问题类型
        /// </summary>
        QuestionType SupportedType { get; }
    }
} 