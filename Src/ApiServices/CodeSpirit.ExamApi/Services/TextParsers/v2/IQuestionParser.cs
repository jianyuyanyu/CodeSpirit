using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Services.TextParsers.v2;

/// <summary>
/// 题目解析器接口
/// </summary>
public interface IQuestionParser
{
    /// <summary>
    /// 判断是否可以解析该行
    /// </summary>
    /// <param name="line">待解析的行</param>
    bool CanParse(string line);

    /// <summary>
    /// 解析题目
    /// </summary>
    /// <param name="lines">题目相关的所有行</param>
    /// <returns>解析结果</returns>
    QuestionParseResult Parse(IEnumerable<string> lines);
} 