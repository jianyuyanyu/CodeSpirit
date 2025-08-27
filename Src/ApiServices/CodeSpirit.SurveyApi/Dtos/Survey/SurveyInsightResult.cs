using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷洞察结果
/// </summary>
[DisplayName("问卷洞察结果")]
public class SurveyInsightResult
{
    /// <summary>
    /// 洞察列表
    /// </summary>
    [DisplayName("洞察列表")]
    public List<SurveyInsight> Insights { get; set; } = new();

    /// <summary>
    /// 数据质量评分
    /// </summary>
    [DisplayName("数据质量评分")]
    public int DataQualityScore { get; set; }

    /// <summary>
    /// 关键发现
    /// </summary>
    [DisplayName("关键发现")]
    public List<string> KeyFindings { get; set; } = new();

    /// <summary>
    /// 建议行动
    /// </summary>
    [DisplayName("建议行动")]
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// 问卷洞察
/// </summary>
[DisplayName("问卷洞察")]
public class SurveyInsight
{
    /// <summary>
    /// 洞察类型
    /// </summary>
    [DisplayName("洞察类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 洞察内容
    /// </summary>
    [DisplayName("洞察内容")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    [DisplayName("置信度")]
    public double Confidence { get; set; }

    /// <summary>
    /// 相关题目ID
    /// </summary>
    [DisplayName("相关题目ID")]
    public List<int> RelatedQuestionIds { get; set; } = new();
}
