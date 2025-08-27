using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷优化结果
/// </summary>
[DisplayName("问卷优化结果")]
public class SurveyOptimizationResult
{
    /// <summary>
    /// 优化建议列表
    /// </summary>
    [DisplayName("优化建议列表")]
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 整体评分（1-10）
    /// </summary>
    [DisplayName("整体评分")]
    public int OverallScore { get; set; }

    /// <summary>
    /// 优化后预期提升
    /// </summary>
    [DisplayName("优化后预期提升")]
    public string? ExpectedImprovement { get; set; }
}

/// <summary>
/// 优化建议
/// </summary>
[DisplayName("优化建议")]
public class OptimizationSuggestion
{
    /// <summary>
    /// 建议类型
    /// </summary>
    [DisplayName("建议类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 建议内容
    /// </summary>
    [DisplayName("建议内容")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    [DisplayName("优先级")]
    public int Priority { get; set; }

    /// <summary>
    /// 影响的题目ID（可选）
    /// </summary>
    [DisplayName("影响的题目ID")]
    public int? QuestionId { get; set; }
}
