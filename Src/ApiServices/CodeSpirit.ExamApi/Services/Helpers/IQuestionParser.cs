using CodeSpirit.ExamApi.Dtos.Question;

namespace CodeSpirit.ExamApi.Services.Helpers;

/// <summary>
/// 题目解析器接口
/// </summary>
public interface IQuestionParser
{
    /// <summary>
    /// 解析生成的题目
    /// </summary>
    /// <param name="content">生成的内容</param>
    /// <param name="request">生成请求</param>
    /// <returns>解析后的题目列表</returns>
    List<CreateQuestionDto> ParseQuestions(string content, AIGenerateQuestionDto request);
} 