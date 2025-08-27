using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷字段建议
/// </summary>
[DisplayName("问卷字段建议")]
public class SurveyFieldSuggestions
{
    /// <summary>
    /// 建议的问卷描述
    /// </summary>
    [DisplayName("问卷描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议的问卷类型
    /// </summary>
    [DisplayName("问卷类型")]
    public string SurveyType { get; set; } = string.Empty;

    /// <summary>
    /// 建议的题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; } = 10;

    /// <summary>
    /// 建议的目标受众
    /// </summary>
    [DisplayName("目标受众")]
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// 建议的调查目标
    /// </summary>
    [DisplayName("调查目标")]
    public string Goals { get; set; } = string.Empty;
}
