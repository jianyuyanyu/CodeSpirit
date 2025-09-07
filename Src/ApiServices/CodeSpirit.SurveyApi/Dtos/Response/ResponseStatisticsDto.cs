using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Response;

/// <summary>
/// 问卷回答统计DTO
/// </summary>
[DisplayName("问卷回答统计")]
public class ResponseStatisticsDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    public string? SurveyTitle { get; set; }

    /// <summary>
    /// 总回答数
    /// </summary>
    [DisplayName("总回答数")]
    public int TotalResponses { get; set; }

    /// <summary>
    /// 已完成回答数
    /// </summary>
    [DisplayName("已完成回答数")]
    public int CompletedResponses { get; set; }

    /// <summary>
    /// 进行中回答数
    /// </summary>
    [DisplayName("进行中回答数")]
    public int InProgressResponses { get; set; }

    /// <summary>
    /// 已放弃回答数
    /// </summary>
    [DisplayName("已放弃回答数")]
    public int AbandonedResponses { get; set; }

    /// <summary>
    /// 完成率（百分比）
    /// </summary>
    [DisplayName("完成率")]
    public decimal CompletionRate { get; set; }

    /// <summary>
    /// 平均答题用时（分钟）
    /// </summary>
    [DisplayName("平均答题用时")]
    public decimal? AverageDurationMinutes { get; set; }

    /// <summary>
    /// 最快答题用时（分钟）
    /// </summary>
    [DisplayName("最快答题用时")]
    public int? MinDurationMinutes { get; set; }

    /// <summary>
    /// 最慢答题用时（分钟）
    /// </summary>
    [DisplayName("最慢答题用时")]
    public int? MaxDurationMinutes { get; set; }

    /// <summary>
    /// 最近7天回答数
    /// </summary>
    [DisplayName("最近7天回答数")]
    public int Last7DaysResponses { get; set; }

    /// <summary>
    /// 最近30天回答数
    /// </summary>
    [DisplayName("最近30天回答数")]
    public int Last30DaysResponses { get; set; }

    /// <summary>
    /// 首次回答时间
    /// </summary>
    [DisplayName("首次回答时间")]
    public DateTime? FirstResponseAt { get; set; }

    /// <summary>
    /// 最新回答时间
    /// </summary>
    [DisplayName("最新回答时间")]
    public DateTime? LastResponseAt { get; set; }
}
