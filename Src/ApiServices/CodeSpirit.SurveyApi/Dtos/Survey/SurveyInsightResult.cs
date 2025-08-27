using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷洞察结果
/// </summary>
[DisplayName("问卷洞察结果")]
public class SurveyInsightResult
{
    /// <summary>
    /// 洞察分析内容
    /// </summary>
    [DisplayName("洞察分析")]
    public string Insights { get; set; } = string.Empty;

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


