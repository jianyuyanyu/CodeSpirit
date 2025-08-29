using System.ComponentModel;
using CodeSpirit.SurveyApi.Dtos.Question;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// AI扩题结果
/// </summary>
[DisplayName("AI扩题结果")]
public class ExpandQuestionsResult
{
    /// <summary>
    /// 生成的题目列表
    /// </summary>
    [DisplayName("生成的题目")]
    public List<QuestionDto> GeneratedQuestions { get; set; } = new();

    /// <summary>
    /// 扩展说明
    /// </summary>
    [DisplayName("扩展说明")]
    public string? ExpandDescription { get; set; }

    /// <summary>
    /// AI分析的问卷风格
    /// </summary>
    [DisplayName("问卷风格分析")]
    public string? StyleAnalysis { get; set; }

    /// <summary>
    /// 使用的提示词
    /// </summary>
    [DisplayName("使用的提示词")]
    public string? UsedPrompt { get; set; }

    /// <summary>
    /// LLM原始输出
    /// </summary>
    [DisplayName("LLM原始输出")]
    public string? LLMRawOutput { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    [DisplayName("生成时间")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 成功生成的题目数量
    /// </summary>
    [DisplayName("成功生成数量")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// 扩展建议
    /// </summary>
    [DisplayName("扩展建议")]
    public List<string>? Suggestions { get; set; }
}
